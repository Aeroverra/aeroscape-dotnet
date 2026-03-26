using System.Buffers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Network.Protocol;

/// <summary>
/// Reads raw bytes from a client pipeline, frames them into packets using
/// <see cref="ProtocolDictionary"/>, decodes them into protocol-agnostic message
/// records, and dispatches to scoped <c>IMessageHandler&lt;T&gt;</c> instances
/// resolved via DI.
/// </summary>
public sealed class PacketRouter
{
    private readonly ILogger<PacketRouter> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>Opcode → decoder mapping.</summary>
    private readonly Dictionary<int, IPacketDecoder> _decoders = new();

    /// <summary>Opcode → handler type mapping (the closed generic IMessageHandler&lt;T&gt;).</summary>
    private readonly Dictionary<int, Type> _handlerTypes = new();

    public PacketRouter(ILogger<PacketRouter> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;

        RegisterDecoders();
    }

    private void RegisterDecoders()
    {
        // Helper to register a decoder for one or more opcodes
        void Reg(IPacketDecoder decoder, params int[] opcodes)
        {
            var handlerType = typeof(Core.Handlers.IMessageHandler<>).MakeGenericType(decoder.MessageType);
            foreach (var op in opcodes)
            {
                _decoders[op] = decoder;
                _handlerTypes[op] = handlerType;
            }
        }

        Reg(new WalkDecoder(),           49, 119, 138);
        Reg(new PublicChatDecoder(),      222);
        Reg(new CommandDecoder(),         107);
        Reg(new ActionButtonsDecoder(),   21, 113, 169, 173, 232, 233);
        Reg(new EquipItemDecoder(),       3);
        Reg(new ItemOperateDecoder(),     186);
        Reg(new DropItemDecoder(),        211);
        Reg(new PickupItemDecoder(),      201);
        Reg(new PlayerOption1Decoder(),   160);
        Reg(new PlayerOption2Decoder(),   37);
        Reg(new PlayerOption3Decoder(),   227);
        Reg(new NPCAttackDecoder(),       123);
        Reg(new NPCOption1Decoder(),      7);
        Reg(new NPCOption2Decoder(),      52);
        Reg(new NPCOption3Decoder(),      199);
        Reg(new ObjectOption1Decoder(),   158);
        Reg(new ObjectOption2Decoder(),   228);
        Reg(new SwitchItemsDecoder(),     167);
        Reg(new SwitchItems2Decoder(),    179);
        Reg(new ItemOnItemDecoder(),      40);
        Reg(new ItemSelectDecoder(),      220, 134);
        Reg(new ItemOption1Decoder(),     203, 152);
        Reg(new ItemGiveDecoder(),        131);
        Reg(new MagicOnNPCDecoder(),      24);
        Reg(new MagicOnPlayerDecoder(),   70);
        Reg(new ItemOnObjectDecoder(),    224);

        // Inline-handled packets (formerly in PacketManager.parsePacket directly)
        Reg(new AddFriendDecoder(),       30);
        Reg(new RemoveFriendDecoder(),    132);
        Reg(new AddIgnoreDecoder(),       61);
        Reg(new RemoveIgnoreDecoder(),    2);
        Reg(new PrivateMessageDecoder(),  178);
        Reg(new IdleDecoder(),            47);
        Reg(new DialogueContinueDecoder(),63);
        Reg(new CloseInterfaceDecoder(),  108);
        Reg(new ItemExamineDecoder(),     38);
        Reg(new NpcExamineDecoder(),      88);
        Reg(new ObjectExamineDecoder(),   84);
        Reg(new TradeAcceptDecoder(),     253);

        // Legacy PacketManager paths that are currently consume-and-ignore in the C# port.
        Reg(new NoOpDecoder(),            42, 43, 62, 99, 117, 127, 189, 190, 200, 247, 248);
    }

    /// <summary>
    /// Attempt to consume as many complete packets as possible from <paramref name="buffer"/>.
    /// Returns the number of bytes consumed so the caller can advance the pipe reader.
    /// </summary>
    /// <remarks>
    /// Parsing is synchronous (required by the ref-struct <see cref="SequenceReader{T}"/>),
    /// but handler dispatch is fully async — no sync-over-async blocking.
    /// </remarks>
    public async Task<long> ProcessBufferAsync(PlayerSession session, ReadOnlySequence<byte> buffer)
    {
        // Phase 1: Parse packets synchronously (SequenceReader is a ref struct).
        var (consumed, packets) = ParsePackets(session, buffer);

        // Phase 2: Dispatch decoded packets asynchronously.
        foreach (var (opcode, payload) in packets)
        {
            await DispatchPacketAsync(session, opcode, payload);
        }

        return consumed;
    }

    /// <summary>
    /// Synchronously parses up to 10 framed packets from <paramref name="buffer"/>,
    /// decrypting opcodes via ISAAC when the session cipher is initialised.
    /// </summary>
    private (long Consumed, List<(int Opcode, ReadOnlySequence<byte> Payload)> Packets) ParsePackets(
        PlayerSession session, ReadOnlySequence<byte> buffer)
    {
        var reader = new SequenceReader<byte>(buffer);
        var packets = new List<(int, ReadOnlySequence<byte>)>();
        int packetsRead = 0;

        while (packetsRead < 10) // cap per cycle, same as legacy Java
        {
            if (!reader.TryRead(out byte rawOpcode))
                break;

            // Decrypt the opcode using the session's ISAAC cipher (RS 508 protocol).
            // The client encrypts opcodes with its ISAAC instance; the server must
            // subtract the next ISAAC value to recover the real opcode.
            int opcode = session.InCipher is not null
                ? (rawOpcode - session.InCipher.NextInt()) & 0xFF
                : rawOpcode & 0xFF;

            if ((uint)opcode >= ProtocolDictionary.Incoming.Length)
            {
                _logger.LogWarning("Invalid opcode {Opcode} for session {SessionId}", opcode, session.SessionId);
                break;
            }

            var def = ProtocolDictionary.Incoming[opcode];
            int size = def.Size;

            // Variable-length: next byte is the real size.
            if (size == PacketDefinition.VariableSize)
            {
                if (!reader.TryRead(out byte sizeByte))
                {
                    reader.Rewind(1);
                    break;
                }
                size = sizeByte & 0xFF;
            }
            else if (size == PacketDefinition.UnknownSize)
            {
                _logger.LogDebug("Stopping on unknown-size opcode {Opcode} for session {SessionId}", opcode, session.SessionId);
                reader.Rewind(1);
                break;
            }

            if (size < 0 || size >= 500)
            {
                _logger.LogWarning(
                    "Rejecting packet opcode {Opcode} with invalid size {Size} for session {SessionId}",
                    opcode, size, session.SessionId);
                break;
            }

            if (reader.Remaining < size)
            {
                reader.Rewind(def.IsVariable ? 2 : 1);
                break;
            }

            // Slice the payload.
            ReadOnlySequence<byte> payload = buffer.Slice(reader.Position, size);
            reader.Advance(size);

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace(
                    "[{Session}] Packet {Name} (opcode={Opcode}, size={Size})",
                    session.SessionId, def.Name, opcode, size);
            }

            packets.Add((opcode, payload));
            packetsRead++;
        }

        return (reader.Consumed, packets);
    }

    private async Task DispatchPacketAsync(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        if (!_decoders.TryGetValue(opcode, out var decoder))
        {
            // No decoder for this opcode — silently skip (idle, ping, junk, etc.)
            return;
        }

        object? message;
        try
        {
            message = decoder.Decode(session, opcode, payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decode packet opcode {Opcode}", opcode);
            return;
        }

        if (message is null)
            return;

        // Resolve the handler from a new DI scope
        var handlerType = _handlerTypes[opcode];

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetService(handlerType);

        if (handler is null)
        {
            _logger.LogDebug("No handler registered for {MessageType} (opcode {Opcode})",
                decoder.MessageType.Name, opcode);
            return;
        }

        try
        {
            // Invoke HandleAsync(session, message, CancellationToken.None) via reflection
            // since we don't know T at compile time.
            var method = handlerType.GetMethod("HandleAsync")!;
            var task = (Task)method.Invoke(handler, new[] { session, message, CancellationToken.None })!;
            await task;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling {MessageType} (opcode {Opcode})",
                decoder.MessageType.Name, opcode);
        }
    }
}
