using System.Buffers;
using System.Text;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;
using AeroScape.Server.Core.Util;

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
    public sbyte ReadSignedByteA() => (sbyte)(ReadByte() - 128);

    public int ReadUnsignedWord()
    {
        byte hi = ReadByte();
        byte lo = ReadByte();
        return (hi << 8) | lo;
    }

    public int ReadUnsignedWordBigEndian()
    {
        byte lo = ReadByte();
        byte hi = ReadByte();
        return (hi << 8) | lo;
    }

    public int ReadUnsignedWordA()
    {
        byte hi = ReadByte();
        byte lo = ReadByte();
        return (hi << 8) | ((lo - 128) & 0xFF);
    }

    public int ReadUnsignedWordBigEndianA()
    {
        byte lo = ReadByte();
        byte hi = ReadByte();
        return (hi << 8) | ((lo - 128) & 0xFF);
    }

    public int ReadSignedWordA()
    {
        int val = ReadUnsignedWordA();
        return val > 32767 ? val - 65536 : val;
    }

    public int ReadSignedWordBigEndian()
    {
        int val = ReadUnsignedWordBigEndian();
        return val > 32767 ? val - 65536 : val;
    }

    public int ReadSignedWordBigEndianA()
    {
        int val = ReadUnsignedWordBigEndianA();
        return val > 32767 ? val - 65536 : val;
    }

    public int ReadDWord()
    {
        byte a = ReadByte(), b = ReadByte(), c = ReadByte(), d = ReadByte();
        return (a << 24) | (b << 16) | (c << 8) | d;
    }

    public int ReadDWordV2()
    {
        byte b1 = ReadByte(), b0 = ReadByte(), b3 = ReadByte(), b2 = ReadByte();
        return (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
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
        while ((b = ReadByte()) != 0)
            sb.Append((char)b);
        return sb.ToString();
    }

    public void Skip(int count)
    {
        for (int i = 0; i < count; i++) ReadByte();
    }
}

internal static class RsChatCodec
{
    private static readonly int[] DecodeTree =
    [
        215, 203, 83, 158, 104, 101, 93, 84, 107, 103, 109, 95, 94, 98, 89, 86,
        70, 41, 32, 27, 24, 23, -1, -2, 26, -3, -4, 31, 30, -5, -6, -7, 37, 38,
        36, -8, -9, -10, 40, -11, -12, 55, 48, 46, 47, -13, -14, -15, 52, 51,
        -16, -17, 54, -18, -19, 63, 60, 59, -20, -21, 62, -22, -23, 67, 66, -24,
        -25, 69, -26, -27, 199, 132, 80, 77, 76, -28, -29, 79, -30, -31, 87, 85,
        -32, -33, -34, -35, -36, 197, -37, 91, -38, 134, -39, -40, -41, 97, -42,
        -43, 133, 106, -44, 117, -45, -46, 139, -47, -48, 110, -49, -50, 114,
        113, -51, -52, 116, -53, -54, 135, 138, 136, 129, 125, 124, -55, -56,
        130, 128, -57, -58, -59, 183, -60, -61, -62, -63, -64, 148, -65, -66,
        153, 149, 145, 144, -67, -68, 147, -69, -70, -71, 152, 154, -72, -73,
        -74, 157, 171, -75, -76, 207, 184, 174, 167, 166, 165, -77, -78, -79,
        172, 170, -80, -81, -82, 178, -83, 177, 182, -84, -85, 187, 181, -86,
        -87, -88, -89, 206, 221, -90, 189, -91, 198, 254, 262, 195, 196, -92,
        -93, -94, -95, -96, 252, 255, 250, -97, 211, 209, -98, -99, 212, -100,
        213, -101, -102, -103, 224, -104, 232, 227, 220, 226, -105, -106, 246,
        236, -107, 243, -108, -109, 231, 237, 235, -110, -111, 239, 238, -112,
        -113, -114, -115, -116, 241, -117, 244, -118, -119, 248, -120, 249, -121,
        -122, -123, 253, -124, -125, -126, -127, 259, 258, -128, -129, 261, -130,
        -131, 390, 327, 296, 281, 274, 271, 270, -132, -133, 273, -134, -135,
        278, 277, -136, -137, 280, -138, -139, 289, 286, 285, -140, -141, 288,
        -142, -143, 293, 292, -144, -145, 295, -146, -147, 312, 305, 302, 301,
        -148, -149, 304, -150, -151, 309, 308, -152, -153, 311, -154, -155, 320,
        317, 316, -156, -157, 319, -158, -159, 324, 323, -160, -161, 326, -162,
        -163, 359, 344, 337, 334, 333, -164, -165, 336, -166, -167, 341, 340,
        -168, -169, 343, -170, -171, 352, 349, 348, -172, -173, 351, -174, -175,
        356, 355, -176, -177, 358, -178, -179, 375, 368, 365, 364, -180, -181,
        367, -182, -183, 372, 371, -184, -185, 374, -186, -187, 383, 380, 379,
        -188, -189, 382, -190, -191, 387, 386, -192, -193, 389, -194, -195, 454,
        423, 408, 401, 398, 397, -196, -197, 400, -198, -199, 405, 404, -200,
        -201, 407, -202, -203, 416, 413, 412, -204, -205, 415, -206, -207, 420,
        419, -208, -209, 422, -210, -211, 439, 432, 429, 428, -212, -213, 431,
        -214, -215, 436, 435, -216, -217, 438, -218, -219, 447, 444, 443, -220,
        -221, 446, -222, -223, 451, 450, -224, -225, 453, -226, -227, 486, 471,
        464, 461, 460, -228, -229, 463, -230, -231, 468, 467, -232, -233, 470,
        -234, -235, 479, 476, 475, -236, -237, 478, -238, -239, 483, 482, -240,
        -241, 485, -242, -243, 499, 495, 492, 491, -244, -245, 494, -246, -247,
        497, -248, 502, -249, 506, 503, -250, -251, 505, -252, -253, 508, -254,
        510, -255, -256, 0
    ];

    public static string Decompress(ReadOnlySequence<byte> payload, int offset, int numChars)
    {
        if (numChars <= 0)
        {
            return string.Empty;
        }

        var charsDecoded = 0;
        var node = 0;
        var sb = new StringBuilder(numChars);
        var reader = new SequenceReader<byte>(payload);
        reader.Advance(offset);

        while (charsDecoded < numChars && reader.TryRead(out byte raw))
        {
            int value = unchecked((sbyte)raw);

            node = value >= 0 ? node + 1 : DecodeTree[node];
            int decoded = DecodeTree[node];
            if (decoded < 0)
            {
                sb.Append((char)(byte)~decoded);
                charsDecoded++;
                node = 0;
                if (charsDecoded >= numChars) break;
            }

            for (int mask = 0x40; mask >= 0x1 && charsDecoded < numChars; mask >>= 1)
            {
                node = (value & mask) == 0 ? node + 1 : DecodeTree[node];
                decoded = DecodeTree[node];
                if (decoded < 0)
                {
                    sb.Append((char)(byte)~decoded);
                    charsDecoded++;
                    node = 0;
                }
            }
        }

        return sb.ToString();
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
        var player = session.Entity;
        int regionBaseX = player is null ? 0 : (player.MapRegionX - 6) * 8;
        int regionBaseY = player is null ? 0 : (player.MapRegionY - 6) * 8;
        int firstX = r.ReadUnsignedWordBigEndianA() - regionBaseX;
        int firstY = r.ReadUnsignedWordA() - regionBaseY;
        bool running = r.ReadSignedByteC() == 1;
        int[] pathX = new int[numPath];
        int[] pathY = new int[numPath];
        for (int i = 0; i < numPath; i++)
        {
            pathX[i] = r.ReadSignedByte();
            pathY[i] = r.ReadSignedByteS();
        }

        return new WalkMessage(opcode, firstX, firstY, running, pathX, pathY);
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
        string text = RsChatCodec.Decompress(payload, 3, numChars);
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

        // Java reads two separate words: readUnsignedWord() for interfaceId, then readUnsignedWord() for buttonId
        // Java ref: ActionButtons.java:44-47
        interfaceId = r.ReadUnsignedWord();
        buttonId = r.ReadUnsignedWord();
        
        if (payload.Length == 6)
        {
            slotId = r.ReadUnsignedWord();
            if (slotId == 65535)
            {
                slotId = 0;
            }
        }

        return new ActionButtonsMessage(opcode, interfaceId, buttonId, itemId, slotId);
    }
}

/// <summary>Equip item: opcode 3.</summary>
public sealed class EquipItemDecoder : IPacketDecoder
{
    public Type MessageType => typeof(EquipItemMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        // Java reads readDWord_v2() as junk and ignores it, then reads readUnsignedWordBigEndian() for wearId
        // Java ref: Equipment.java:76-77
        int junk = r.ReadDWordV2(); // Java ignores this completely
        int wearId = r.ReadUnsignedWordBigEndian();
        int slot = r.ReadByte();
        r.ReadByte();
        return new EquipItemMessage(wearId, slot, 0); // Java doesn't extract interfaceId, use 0 instead of -1
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
        int itemId = r.ReadUnsignedWordA();
        int slot = r.ReadUnsignedWordBigEndianA();
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
        int slot = r.ReadUnsignedWordBigEndianA();
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
        return new PlayerOption1Message(r.ReadUnsignedWordBigEndian());
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
        return new NPCOption1Message(r.ReadUnsignedWordA());
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
        return new NPCOption3Message(r.ReadUnsignedWordBigEndian());
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
    public Type MessageType => typeof(PlayerOption2Message);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        // Java ObjectOption2.java handles player interactions, reading playerId then using player coordinates
        // Fixed: This should be a PlayerOption2Message, not ObjectOption2Message to match Java behavior
        var r = new RsReader(payload);
        int playerId = r.ReadUnsignedWord();
        // Return correct message type that matches Java behavior
        return new PlayerOption2Message(playerId);
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
        int usedWith = r.ReadSignedWordBigEndian();
        int itemUsed = r.ReadSignedWordA();
        // Remaining bytes: usedWithSlot, itemUsedSlot, interface hashes (ignored by legacy)
        int usedWithSlot = r.ReadUnsignedWord();
        int itemUsedSlot = r.ReadUnsignedWord();
        int usedWithInterface = r.ReadDWord();
        int itemUsedInterface = r.ReadDWord();
        return new ItemOnItemMessage(usedWith, itemUsed, usedWithSlot, itemUsedSlot, usedWithInterface, itemUsedInterface);
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
        int interfaceId = r.ReadUnsignedWord();
        r.ReadByte(); // junk
        int itemId = r.ReadUnsignedWordBigEndian();
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
        int slot = r.ReadUnsignedWordBigEndianA();
        int interfaceId = r.ReadUnsignedWord();
        r.ReadUnsignedWord();
        int itemId = r.ReadUnsignedWord();
        r.ReadUnsignedWordBigEndian();
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
        int npcIndex = r.ReadSignedWordA();
        int buttonId = r.ReadSignedWordA();
        int interfaceId = r.ReadUnsignedWord();
        r.ReadSignedWordA();
        r.ReadSignedWordA();
        r.ReadUnsignedWord();
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
        int targetIndex = r.ReadSignedWordA();
        int playerId = r.ReadSignedWordBigEndian();
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
        // Read all 14 bytes: objectId, itemId, objectX, objectY, slot, interface hash
        var r = new RsReader(payload);
        int objectId = r.ReadUnsignedWord();
        int itemId = r.ReadSignedWordA();
        int objectX = r.ReadUnsignedWordBigEndian();
        int objectY = r.ReadUnsignedWordBigEndianA();
        // Skip remaining bytes (slot, interface hash) as they're not used in legacy
        r.ReadSignedWord(); // slot
        r.ReadUnsignedWord(); // interface hash
        return new ItemOnObjectMessage(objectId, itemId, objectX, objectY);
    }
}

/// <summary>Item on NPC: Uses same opcode region. Separate from magic.</summary>
public sealed class ItemOnNPCDecoder : IPacketDecoder
{
    public Type MessageType => typeof(ItemOnNPCMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int itemId = r.ReadUnsignedWordA();
        r.ReadUnsignedWordA();
        int npcIndex = r.ReadUnsignedWordA();
        int interfaceId = r.ReadUnsignedWordA();
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
        int slot = r.ReadUnsignedWordBigEndianA();
        int interfaceId = r.ReadUnsignedWord();
        r.ReadUnsignedWord();
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
        string text = RsChatCodec.Decompress(payload, 9, numChars);
        return new PrivateMessageMessage(targetName, text);
    }
}

/// <summary>Packets that must be consumed but do not currently map to a gameplay message.</summary>
public sealed class NoOpDecoder : IPacketDecoder
{
    public Type MessageType => typeof(IdleMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload) => null;
}

public sealed class ClanJoinDecoder : IPacketDecoder
{
    public Type MessageType => typeof(ClanJoinMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        return new ClanJoinMessage(NameUtil.LongToString(r.ReadQWord()).Replace('_', ' '));
    }
}

public sealed class StringInputDecoder : IPacketDecoder
{
    public Type MessageType => typeof(StringInputMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        return new StringInputMessage(r.ReadString());
    }
}

public sealed class LongInputDecoder : IPacketDecoder
{
    public Type MessageType => typeof(LongInputMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        return new LongInputMessage(r.ReadQWord());
    }
}

public sealed class ClanKickDecoder : IPacketDecoder
{
    public Type MessageType => typeof(ClanKickMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        return new ClanKickMessage(NameUtil.LongToString(r.ReadQWord()).Replace('_', ' '));
    }
}

public sealed class ConstructionDecoder : IPacketDecoder
{
    public Type MessageType => typeof(ConstructionMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int y = r.ReadUnsignedWordBigEndian();
        int x = r.ReadUnsignedWordBigEndianA();
        int objectId = r.ReadUnsignedWordBigEndianA();
        return new ConstructionMessage(x, y, objectId);
    }
}

public sealed class PrayerDecoder : IPacketDecoder
{
    public Type MessageType => typeof(PrayerMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int buttonId = payload.Length >= 2 ? r.ReadUnsignedWord() : r.ReadByte();
        return new PrayerMessage(buttonId);
    }
}

public sealed class BountyHunterDecoder : IPacketDecoder
{
    public Type MessageType => typeof(BountyHunterMessage);

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int targetId = payload.Length >= 2 ? r.ReadUnsignedWord() : r.ReadByte();
        return new BountyHunterMessage(targetId);
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
    
    // Protocol constants for trade partner ID calculation
    private const int TRADE_ID_BASE = 33024;
    private const int TRADE_ID_DIVISOR = 256;

    public object? Decode(PlayerSession session, int opcode, ReadOnlySequence<byte> payload)
    {
        var r = new RsReader(payload);
        int raw = r.ReadUnsignedWord();
        
        // Validate raw value to prevent invalid calculations
        if (raw < TRADE_ID_BASE)
        {
            session.Logger?.LogWarning($"TradeAccept: Invalid raw value {raw} (less than base {TRADE_ID_BASE})");
            return null;
        }
        
        int partnerId = (raw - TRADE_ID_BASE) / TRADE_ID_DIVISOR + 1;
        
        // Validate calculated partner ID is within reasonable bounds
        if (partnerId < 1 || partnerId > 2047) // Max player index in RS
        {
            session.Logger?.LogWarning($"TradeAccept: Invalid partner ID {partnerId} calculated from raw value {raw}");
            return null;
        }
        
        return new TradeAcceptMessage(partnerId);
    }
}
