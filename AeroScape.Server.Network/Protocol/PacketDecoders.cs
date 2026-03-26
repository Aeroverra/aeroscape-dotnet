using System.Buffers;
using System.Text;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Network.Protocol;

// ═══════════════════════════════════════════════════════════════════════════════
//  Helper: lightweight SequenceReader wrapper for RS stream-style reads
// ═══════════════════════════════════════════════════════════════════════════════

internal ref struct RsReader
{
    private SequenceReader<byte> _r;

    public RsReader(ReadOnlySequence<byte> seq) => _r = new SequenceReader<byte>(seq);

    public byte ReadByte() { _r.TryRead(out byte b); return b; }
    public sbyte ReadSignedByte() => (sbyte)ReadByte();
    public sbyte ReadSignedByteC() => (sbyte)(-ReadByte());
    public sbyte ReadSignedByteS() => (sbyte)(128 - ReadByte());

    public int ReadUnsignedWord()
    {
        byte hi = ReadByte(), lo = ReadByte();
        return (hi << 8) | lo;
    }

    public int ReadUnsignedWordBigEndian() => ReadUnsignedWord(); // same in RS terminology: big-endian = MSB first
    public int ReadUnsignedWordA()
    {
        byte hi = ReadByte(), lo = ReadByte();
        return ((hi << 8) | lo) - 128;
    }
    public int ReadUnsignedWordBigEndianA() => ReadUnsignedWordA();

    public int ReadSignedWordA()
    {
        int val = ReadUnsignedWordA();
        return val > 32767 ? val - 65536 : val;
    }
    public int ReadSignedWordBigEndian()
    {
        int val = ReadUnsignedWord();
        return val > 32767 ? val - 65536 : val;
    }

    public int ReadDWord()
    {
        byte a = ReadByte(), b = ReadByte(), c = ReadByte(), d = ReadByte();
        return (a << 24) | (b << 16) | (c << 8) | d;
    }

    public long ReadQWord()
    {
        long hi = (uint)ReadDWord();
        long lo = (uint)ReadDWord();
        return (hi << 32) | lo;
    }

    public string ReadString()
    {
        var sb = new StringBuilder();
        byte b;
        while ((b = ReadByte()) != 10 && b != 0)
            sb.Append((char)b);
        return sb.ToString();
    }

    public void Skip(int count)
    {
        for (int i = 0; i < count; i++) ReadByte();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  Decoders
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Walk packets: opcodes 49 (main), 119 (minimap), 138 (other).</summary>
public sealed class WalkDecoder : IPacketDecoder
{
    public Type MessageType => typeof(WalkMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int size = (int)payload.Length;

        if (opcode == 119) size -= 14; // minimap has 14 extra anticheat bytes

        int numPath = (size - 5) / 2;
        int firstX = r.ReadUnsignedWordBigEndianA();
        int firstY = r.ReadUnsignedWordA();
        bool running = r.ReadSignedByteC() == 1;

        // We just pass the first step; the full path reconstruction will happen in the handler
        return new WalkMessage(firstX, firstY, running);
    }
}

/// <summary>Public chat: opcode 222.</summary>
public sealed class PublicChatDecoder : IPacketDecoder
{
    public Type MessageType => typeof(PublicChatMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int effects = r.ReadUnsignedWord();
        int numChars = r.ReadByte();

        // Read remaining bytes as compressed chat text
        // Actual RS chat decompression would go here; for now pass raw
        var remaining = new byte[(int)payload.Length - 3];
        int idx = 0;
        var reader = new SequenceReader<byte>(payload);
        reader.Advance(3);
        while (reader.TryRead(out byte b) && idx < remaining.Length)
            remaining[idx++] = b;

        string text = Encoding.ASCII.GetString(remaining, 0, idx);
        return new PublicChatMessage(text, effects, 0);
    }
}

/// <summary>Command: opcode 107.</summary>
public sealed class CommandDecoder : IPacketDecoder
{
    public Type MessageType => typeof(CommandMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        return new CommandMessage(r.ReadString());
    }
}

/// <summary>Action buttons: opcodes 21, 113, 169, 173, 232, 233.</summary>
public sealed class ActionButtonsDecoder : IPacketDecoder
{
    public Type MessageType => typeof(ActionButtonsMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int interfaceId, buttonId, itemId = -1, slotId = -1;

        if (payload.Length == 6)
        {
            int packed = r.ReadDWord();
            interfaceId = packed >> 16;
            buttonId = packed & 0xFFFF;
            slotId = r.ReadUnsignedWord();
        }
        else // 4-byte variant
        {
            int packed = r.ReadDWord();
            interfaceId = packed >> 16;
            buttonId = packed & 0xFFFF;
        }

        return new ActionButtonsMessage(interfaceId, buttonId, itemId, slotId);
    }
}

/// <summary>Equip item: opcode 3.</summary>
public sealed class EquipItemDecoder : IPacketDecoder
{
    public Type MessageType => typeof(EquipItemMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int packed = r.ReadDWord();
        int interfaceId = packed >> 16;
        int slot = r.ReadUnsignedWord();
        int itemId = r.ReadUnsignedWord();
        return new EquipItemMessage(itemId, slot, interfaceId);
    }
}

/// <summary>Item operate: opcode 186.</summary>
public sealed class ItemOperateDecoder : IPacketDecoder
{
    public Type MessageType => typeof(ItemOperateMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int packed = r.ReadDWord();
        int interfaceId = packed >> 16;
        int slot = r.ReadUnsignedWord();
        int itemId = r.ReadUnsignedWord();
        return new ItemOperateMessage(itemId, slot, interfaceId);
    }
}

/// <summary>Drop item: opcode 211.</summary>
public sealed class DropItemDecoder : IPacketDecoder
{
    public Type MessageType => typeof(DropItemMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int packed = r.ReadDWord();
        int interfaceId = packed >> 16;
        int slot = r.ReadUnsignedWord();
        int itemId = r.ReadUnsignedWord();
        return new DropItemMessage(itemId, slot, interfaceId);
    }
}

/// <summary>Pickup item: opcode 201.</summary>
public sealed class PickupItemDecoder : IPacketDecoder
{
    public Type MessageType => typeof(PickupItemMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int y = r.ReadUnsignedWordA();
        int x = r.ReadUnsignedWord();
        int itemId = r.ReadUnsignedWordBigEndianA();
        return new PickupItemMessage(itemId, x, y);
    }
}

/// <summary>Player option 1: opcode 160.</summary>
public sealed class PlayerOption1Decoder : IPacketDecoder
{
    public Type MessageType => typeof(PlayerOption1Message);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        return new PlayerOption1Message(r.ReadUnsignedWord());
    }
}

/// <summary>Player option 2: opcode 37.</summary>
public sealed class PlayerOption2Decoder : IPacketDecoder
{
    public Type MessageType => typeof(PlayerOption2Message);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        return new PlayerOption2Message(r.ReadUnsignedWord());
    }
}

/// <summary>Player option 3: opcode 227.</summary>
public sealed class PlayerOption3Decoder : IPacketDecoder
{
    public Type MessageType => typeof(PlayerOption3Message);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        return new PlayerOption3Message(r.ReadUnsignedWordBigEndianA());
    }
}

/// <summary>NPC attack: opcode 123.</summary>
public sealed class NPCAttackDecoder : IPacketDecoder
{
    public Type MessageType => typeof(NPCAttackMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        return new NPCAttackMessage(r.ReadUnsignedWord());
    }
}

/// <summary>NPC option 1: opcode 7.</summary>
public sealed class NPCOption1Decoder : IPacketDecoder
{
    public Type MessageType => typeof(NPCOption1Message);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        return new NPCOption1Message(r.ReadUnsignedWord());
    }
}

/// <summary>NPC option 2: opcode 52.</summary>
public sealed class NPCOption2Decoder : IPacketDecoder
{
    public Type MessageType => typeof(NPCOption2Message);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        return new NPCOption2Message(r.ReadUnsignedWordBigEndianA());
    }
}

/// <summary>NPC option 3: opcode 199.</summary>
public sealed class NPCOption3Decoder : IPacketDecoder
{
    public Type MessageType => typeof(NPCOption3Message);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        return new NPCOption3Message(r.ReadUnsignedWord());
    }
}

/// <summary>Object option 1: opcode 158.</summary>
public sealed class ObjectOption1Decoder : IPacketDecoder
{
    public Type MessageType => typeof(ObjectOption1Message);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int x = r.ReadUnsignedWordBigEndian();
        int objId = r.ReadUnsignedWord();
        int y = r.ReadUnsignedWordBigEndianA();
        return new ObjectOption1Message(objId, x, y);
    }
}

/// <summary>Object option 2: opcode 228.</summary>
public sealed class ObjectOption2Decoder : IPacketDecoder
{
    public Type MessageType => typeof(ObjectOption2Message);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int x = r.ReadUnsignedWordBigEndian();
        int objId = r.ReadUnsignedWord();
        int y = r.ReadUnsignedWordBigEndianA();
        return new ObjectOption2Message(objId, x, y);
    }
}

/// <summary>Switch items: opcode 167.</summary>
public sealed class SwitchItemsDecoder : IPacketDecoder
{
    public Type MessageType => typeof(SwitchItemsMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int toSlot = r.ReadUnsignedWordBigEndianA();
        r.ReadByte(); // junk
        int fromSlot = r.ReadUnsignedWordBigEndianA();
        r.ReadUnsignedWord(); // junk
        int interfaceId = r.ReadByte();
        r.ReadByte(); // junk
        return new SwitchItemsMessage(toSlot, fromSlot, interfaceId);
    }
}

/// <summary>Switch items 2: opcode 179.</summary>
public sealed class SwitchItems2Decoder : IPacketDecoder
{
    public Type MessageType => typeof(SwitchItems2Message);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int toInterface = r.ReadDWord();
        int fromInterface = r.ReadDWord();
        int fromSlot = r.ReadUnsignedWord();
        int toSlot = r.ReadUnsignedWordBigEndian();
        int interfaceId = toInterface >> 16;
        int tabId = toInterface - 49938432;
        return new SwitchItems2Message(toInterface, fromInterface, fromSlot, toSlot, interfaceId, tabId);
    }
}

/// <summary>Item on item: opcode 40. 16 bytes total.</summary>
public sealed class ItemOnItemDecoder : IPacketDecoder
{
    public Type MessageType => typeof(ItemOnItemMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        // RS 508 ItemOnItem: the Java handler only reads the first 4 bytes:
        //   readSignedWordBigEndian() → usedWith (the item used on)
        //   readSignedWordA() → itemUsed (the item being used)
        // The remaining 12 bytes are interface/slot data that the legacy server ignores.
        var r = new RsReader(payload);
        int usedWithId = r.ReadSignedWordBigEndian();
        int itemUsedId = r.ReadSignedWordA();
        // Remaining bytes: usedWithSlot, itemUsedSlot, interface hashes (ignored by legacy)
        int usedWithSlot = r.ReadUnsignedWord();
        int itemUsedSlot = r.ReadUnsignedWord();
        int usedWithInterface = r.ReadDWord();
        int itemUsedInterface = r.ReadDWord();
        return new ItemOnItemMessage(usedWithId, itemUsedId, usedWithSlot, itemUsedSlot, usedWithInterface, itemUsedInterface);
    }
}

/// <summary>Item select: opcodes 220, 134.</summary>
public sealed class ItemSelectDecoder : IPacketDecoder
{
    public Type MessageType => typeof(ItemSelectMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        r.ReadByte(); // junk
        int packed = r.ReadDWord();
        int interfaceId = packed >> 16;
        r.ReadByte(); // junk
        int itemId = r.ReadUnsignedWord();
        int slot = r.ReadUnsignedWordA();
        return new ItemSelectMessage(interfaceId, itemId, slot);
    }
}

/// <summary>Item option 1: opcodes 203, 152.</summary>
public sealed class ItemOption1Decoder : IPacketDecoder
{
    public Type MessageType => typeof(ItemOption1Message);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int packed = r.ReadDWord();
        int interfaceId = packed >> 16;
        int slot = r.ReadUnsignedWord();
        int itemId = r.ReadUnsignedWord();
        return new ItemOption1Message(slot, interfaceId, itemId);
    }
}

/// <summary>Item give: opcode 131.</summary>
public sealed class ItemGiveDecoder : IPacketDecoder
{
    public Type MessageType => typeof(ItemGiveMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int targetIndex = r.ReadSignedWordA();
        r.Skip(1); // junk
        int itemId = r.ReadSignedWordBigEndian();
        return new ItemGiveMessage(targetIndex, itemId);
    }
}

/// <summary>Item on NPC: opcode 24 (partial — magic on NPC also uses this).</summary>
public sealed class MagicOnNPCDecoder : IPacketDecoder
{
    public Type MessageType => typeof(MagicOnNPCMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        // First 2 bytes consumed by PacketManager inline (junk), then:
        int npcIndex = r.ReadSignedWordA();
        int buttonId = r.ReadSignedWordA();
        int interfaceId = r.ReadUnsignedWord();
        return new MagicOnNPCMessage(npcIndex, buttonId, interfaceId);
    }
}

/// <summary>Magic on player: opcode 70.</summary>
public sealed class MagicOnPlayerDecoder : IPacketDecoder
{
    public Type MessageType => typeof(MagicOnPlayerMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int targetIndex = r.ReadUnsignedWord();
        int playerId = r.ReadUnsignedWord();
        int interfaceId = r.ReadUnsignedWord();
        int buttonId = r.ReadUnsignedWord();
        return new MagicOnPlayerMessage(targetIndex, playerId, interfaceId, buttonId);
    }
}

/// <summary>Item on object: opcode 224. 14 bytes in RS 508.</summary>
public sealed class ItemOnObjectDecoder : IPacketDecoder
{
    public Type MessageType => typeof(ItemOnObjectMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        // Java handler reads: readUnsignedWord() → objectId (usedWith), readSignedWordA() → itemId
        // Remaining bytes are object x/y, slot, interface hash (not read by legacy).
        var r = new RsReader(payload);
        int objectId = r.ReadUnsignedWord();
        int itemId = r.ReadSignedWordA();
        return new ItemOnObjectMessage(objectId, itemId);
    }
}

/// <summary>Item on NPC: Uses same opcode region. Separate from magic.</summary>
public sealed class ItemOnNPCDecoder : IPacketDecoder
{
    public Type MessageType => typeof(ItemOnNPCMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int itemId = r.ReadUnsignedWord();
        int npcIndex = r.ReadUnsignedWord();
        int interfaceId = r.ReadUnsignedWord();
        return new ItemOnNPCMessage(itemId, npcIndex, interfaceId);
    }
}

/// <summary>Item option 2: opcode not directly in packet table but used via ActionButtons.</summary>
public sealed class ItemOption2Decoder : IPacketDecoder
{
    public Type MessageType => typeof(ItemOption2Message);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int packed = r.ReadDWord();
        int interfaceId = packed >> 16;
        int slot = r.ReadUnsignedWord();
        int itemId = r.ReadUnsignedWord();
        return new ItemOption2Message(slot, interfaceId, itemId);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  Inline-handled packet decoders (formerly processed directly in PacketManager)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Add friend: opcode 30 (8 bytes — QWord encoded name).</summary>
public sealed class AddFriendDecoder : IPacketDecoder
{
    public Type MessageType => typeof(AddFriendMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        return new AddFriendMessage(r.ReadQWord());
    }
}

/// <summary>Remove friend: opcode 132 (8 bytes — QWord encoded name).</summary>
public sealed class RemoveFriendDecoder : IPacketDecoder
{
    public Type MessageType => typeof(RemoveFriendMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        return new RemoveFriendMessage(r.ReadQWord());
    }
}

/// <summary>Add ignore: opcode 61 (8 bytes — QWord encoded name).</summary>
public sealed class AddIgnoreDecoder : IPacketDecoder
{
    public Type MessageType => typeof(AddIgnoreMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        return new AddIgnoreMessage(r.ReadQWord());
    }
}

/// <summary>Remove ignore: opcode 2 (8 bytes — QWord encoded name).</summary>
public sealed class RemoveIgnoreDecoder : IPacketDecoder
{
    public Type MessageType => typeof(RemoveIgnoreMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        return new RemoveIgnoreMessage(r.ReadQWord());
    }
}

/// <summary>Private message: opcode 178 (variable length).</summary>
public sealed class PrivateMessageDecoder : IPacketDecoder
{
    public Type MessageType => typeof(PrivateMessageMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        long targetName = r.ReadQWord();
        int numChars = r.ReadByte();

        // Read remaining as chat text (compressed RS chat)
        var remaining = new byte[(int)payload.Length - 9];
        var reader = new SequenceReader<byte>(payload);
        reader.Advance(9);
        int idx = 0;
        while (reader.TryRead(out byte b) && idx < remaining.Length)
            remaining[idx++] = b;

        string text = Encoding.ASCII.GetString(remaining, 0, idx);
        return new PrivateMessageMessage(targetName, text);
    }
}

/// <summary>Idle: opcode 47 (0 bytes).</summary>
public sealed class IdleDecoder : IPacketDecoder
{
    public Type MessageType => typeof(IdleMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
        => new IdleMessage();
}

/// <summary>Dialogue continue: opcode 63 (6 bytes — interface hash + slot, consumed but not used by legacy).</summary>
public sealed class DialogueContinueDecoder : IPacketDecoder
{
    public Type MessageType => typeof(DialogueContinueMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
        => new DialogueContinueMessage();
}

/// <summary>Close interface: opcode 108 (0 bytes).</summary>
public sealed class CloseInterfaceDecoder : IPacketDecoder
{
    public Type MessageType => typeof(CloseInterfaceMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
        => new CloseInterfaceMessage();
}

/// <summary>Item examine: opcode 38 (2 bytes).</summary>
public sealed class ItemExamineDecoder : IPacketDecoder
{
    public Type MessageType => typeof(ItemExamineMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        return new ItemExamineMessage(r.ReadUnsignedWordBigEndianA());
    }
}

/// <summary>NPC examine: opcode 88 (2 bytes).</summary>
public sealed class NpcExamineDecoder : IPacketDecoder
{
    public Type MessageType => typeof(NpcExamineMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        return new NpcExamineMessage(r.ReadUnsignedWord());
    }
}

/// <summary>Object examine: opcode 84 (2 bytes).</summary>
public sealed class ObjectExamineDecoder : IPacketDecoder
{
    public Type MessageType => typeof(ObjectExamineMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        return new ObjectExamineMessage(r.ReadUnsignedWordA());
    }
}

/// <summary>Trade accept: opcode 253.</summary>
public sealed class TradeAcceptDecoder : IPacketDecoder
{
    public Type MessageType => typeof(TradeAcceptMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int raw = r.ReadUnsignedWord();
        int partnerId = (raw - 33024) / 256 + 1;
        return new TradeAcceptMessage(partnerId);
    }
}
