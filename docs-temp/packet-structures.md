# Packet Structures

Detailed byte-level structures for RS 508 packets.

## Data Types

### Primitive Types
- **Byte**: 8-bit signed integer (-128 to 127)
- **UByte**: 8-bit unsigned integer (0 to 255)
- **Short/Word**: 16-bit signed integer
- **UShort/UWord**: 16-bit unsigned integer  
- **Int/DWord**: 32-bit signed integer
- **Long/QWord**: 64-bit signed integer
- **String (C-String)**: Null-terminated (0x00) ASCII string
- **JString**: String terminated with byte value 10 (0x0A)

### Byte-Order and Transformation Modifiers
These are commonly used in the 508 client to obfuscate the protocol:

| Modifier | Description |
|----------|-------------|
| **ByteA** | `value - 128` on write; `(value - 128) & 0xFF` on read |
| **ByteC** | `-value` (negation) on write; `(-value) & 0xFF` on read |
| **ByteS** | `128 - value` on write; `(128 - value) & 0xFF` on read |
| **LEShort** | Little-endian short (low byte first, then high byte) |
| **ShortA** | Big-endian short with modifier A on the low byte |
| **LEShortA** | Little-endian short with modifier A on the low byte |
| **LEInt** | Little-endian 32-bit integer |
| **Int1 / ME Int** | Middle-endian int (bytes in order: 2,1,4,3) |
| **Int2 / IME Int** | Inverse middle-endian int (bytes in order: 3,4,1,2) |

### Bit Access
The protocol also uses bit-level access for efficient data packing, especially in player/NPC updating. Bits are written/read sequentially within a byte buffer using position tracking.

## Buffer Read/Write Methods Reference

### Common Server-Side Write Methods
| Method | Bytes Written | Description |
|--------|---------------|-------------|
| `createFrame(opcode)` | 1 | Write a fixed-size packet opcode |
| `createFrameVarSize(opcode)` | 1 + 1 (size placeholder) | Start a var-byte packet |
| `createFrameVarSizeWord(opcode)` | 1 + 2 (size placeholder) | Start a var-short packet |
| `endFrameVarSize()` | 0 (fills placeholder) | Finalize var-byte packet size |
| `endFrameVarSizeWord()` | 0 (fills placeholder) | Finalize var-short packet size |
| `writeByte(val)` | 1 | Standard byte |
| `writeByteA(val)` | 1 | `val + 128` |
| `writeByteC(val)` | 1 | `-val` |
| `writeByteS(val)` | 1 | `128 - val` |
| `writeWord(val)` | 2 | Big-endian short |
| `writeWordA(val)` | 2 | Big-endian short, low byte + 128 |
| `writeWordBigEndian(val)` | 2 | Big-endian short (same as writeWord) |
| `writeWordBigEndianA(val)` | 2 | Big-endian short with A modifier |
| `writeLEShort(val)` | 2 | Little-endian short |
| `writeDWord(val)` | 4 | Big-endian int |
| `writeLEInt(val)` | 4 | Little-endian int |
| `writeInt1(val)` / `writeInt2(val)` | 4 | Middle-endian / inverse middle-endian int |
| `writeQWord(val)` | 8 | 64-bit long |
| `writeString(str)` | Variable | NUL-terminated string |

### Common Client-Side Read Methods
| Method | Bytes Read | Description |
|--------|------------|-------------|
| `readUnsignedByte()` | 1 | Read unsigned byte |
| `readByte()` | 1 | Read signed byte |
| `readByteA()` | 1 | Read byte with A modifier |
| `readByteC()` | 1 | Read negated byte |
| `readByteS()` | 1 | Read byte with S modifier |
| `readUnsignedWord()` | 2 | Read big-endian unsigned short |
| `readShortA()` | 2 | Read short with A modifier |
| `readLEShort()` | 2 | Read little-endian short |
| `readLEShortA()` | 2 | Read LE short with A modifier |
| `readInt()` | 4 | Read big-endian int |
| `readLEInt()` | 4 | Read little-endian int |
| `readLong()` | 8 | Read 64-bit long |
| `readString()` | Variable | Read NUL-terminated string |

### Legacy Method Mappings (from our codebase)
- `readUnsignedByte()` - Standard unsigned byte
- `readSignedByte()` - Standard signed byte
- `readUnsignedWord()` - 16-bit big endian
- `readUnsignedWordBigEndian()` - 16-bit big endian  
- `readUnsignedWordBigEndianA()` - 16-bit big endian + 128
- `readUnsignedWordA()` - 16-bit little endian + 128
- `readDWord()` - 32-bit big endian
- `readDWord_v1()` - 32-bit middle endian v1
- `readDWord_v2()` - 32-bit middle endian v2
- `readQWord()` - 64-bit big endian
- `readString()` - Null-terminated string
- `readSignedByteC()` - Signed byte - 128
- `readSignedByteS()` - 128 - signed byte

## Encryption

### ISAAC (Indirection, Shift, Accumulate, Add, and Count)
- Used to encrypt/decrypt packet opcodes during the game session
- Both client and server maintain their own ISAAC cipher instances
- Seeded from session keys established during login
- **Encoding (client → server)**: `opcode + isaac.nextInt()` (written as byte)
- **Decoding (server → client)**: `(opcode - isaac.nextInt()) & 0xFF`

### RSA (Rivest–Shamir–Adleman)
- Used **only** during the login block to protect session keys, username, and password
- The login block is RSA encrypted before being sent
- In many RSPS implementations, RSA is disabled for simplicity (keys replaced with trivial values)

## Client→Server Packet Details

### Walking Packets (49, 119, 138)

#### Packet 49: Main Walking
```
Size: Variable
Structure:
  [0-1] First X coordinate - regionX * 8 (readUnsignedWordBigEndianA)
  [2-3] First Y coordinate - regionY * 8 (readUnsignedWordA)  
  [4] Running flag (readSignedByteC)
  [5..n] Path steps:
    - X offset (readSignedByte)
    - Y offset (readSignedByteS)
```

#### Packet 119: Minimap Walking
```
Size: Variable (actual size - 14)
Structure: Same as packet 49
```

### Chat Packets

#### Packet 222: Public Chat
```
Size: Variable
Structure:
  [0-1] Text effects (readUnsignedWord)
    - Bits 0-7: Text color
    - Bits 8-15: Text effect
  [2] Number of characters (readUnsignedByte)
  [3..n] Encrypted chat text (variable)
```

#### Packet 178: Private Message
```
Size: Variable  
Structure:
  [0-7] Recipient name (readQWord)
  [8] Text length (readUnsignedByte)
  [9..n] Encrypted message text
```

### Interface Packets

#### Packet 21, 169, 232, 233: Button Click
```
Size: 6 bytes
Structure:
  [0-1] Interface ID (readUnsignedWord)
  [2-3] Button ID (readUnsignedWord)
  [4-5] Button ID 2 (readUnsignedWord) - only for packets 21,169,232,233
```

#### Packet 108: Close Interface
```
Size: 0 bytes
Structure: No payload
```

### Item Packets

#### Packet 3: Equip Item
```
Size: 8 bytes
Structure:
  [0-3] Unknown/Junk (readDWord_v2)
  [4-5] Item ID (readUnsignedWordBigEndian)
  [6] Item slot (readUnsignedByte)
  [7] Unknown/Junk (readUnsignedByte)
```

#### Packet 211: Drop Item
```
Size: 8 bytes
Structure:
  [0-1] Item ID (readUnsignedWordBigEndian)
  [2-5] Interface ID (readDWord_v1) 
  [6-7] Item slot (readUnsignedWordBigEndianA)
```

#### Packet 201: Pickup Item
```
Size: 6 bytes
Structure:
  [0-1] Item Y coordinate (readUnsignedWordBigEndian)
  [2-3] Item ID (readUnsignedWordA)
  [4-5] Item X coordinate (readUnsignedWordBigEndianA)
```

#### Packet 167: Switch Items
```
Size: 9 bytes
Structure:
  [0-3] Interface ID (readDWord_v2)
  [4-5] To slot (readUnsignedWordA)
  [6] Unknown (readSignedByteS) 
  [7-8] From slot (readUnsignedWordBigEndianA)
```

### Combat Packets

#### Packet 123: NPC Attack
```
Size: 2 bytes
Structure:
  [0-1] NPC index (readUnsignedWordA)
```

#### Packet 160: Player Option 1 (Attack)
```
Size: 2 bytes  
Structure:
  [0-1] Player index - 33024 (readUnsignedWordBigEndian)
```

#### Packet 70: Magic on Player
```
Size: 8 bytes
Structure:
  [0-1] Player ID (readSignedWordA)
  [2-3] Unknown (readUnsignedWord)
  [4-5] Unknown (readSignedWord) 
  [6-7] Spell button ID (readSignedWordBigEndianA)
```

### NPC Interaction

#### Packet 7: NPC Option 1
```
Size: 2 bytes
Structure:
  [0-1] NPC ID (readUnsignedWordA)
```

#### Packet 52: NPC Option 2  
```
Size: 2 bytes
Structure:
  [0-1] NPC ID (readUnsignedWordBigEndianA)
```

### Object Interaction

#### Packet 158: Object Option 1
```
Size: 6 bytes
Structure:
  [0-1] Object Y (readSignedWordBigEndianA)
  [2-3] Object ID (readUnsignedWord)
  [4-5] Object X (readSignedWordBigEndian)
```

#### Packet 228: Object Option 2
```
Size: 6 bytes
Structure:
  [0-1] Object ID (readUnsignedWordBigEndianA)
  [2-3] Object Y (readSignedWordBigEndian)
  [4-5] Object X (readSignedWordBigEndianA)
```

### Misc Packets

#### Packet 107: Command
```
Size: Variable
Structure:
  [0..n] Command string without :: prefix
```

#### Packet 47: Idle
```
Size: 0 bytes
Structure: No payload
Note: Sent periodically to keep connection alive
```

## Packet Transformations

The 508 protocol uses various byte transformations:

- **A transformation**: Add 128 to the value
- **C transformation**: Subtract 128 from the value  
- **S transformation**: 128 minus the value

## Coordinate System

- Absolute coordinates = Region base + Local offset
- Region X/Y = Floor(Absolute / 64)
- Local X/Y = Absolute % 64  
- Network format often uses (coordinate - regionBase * 8)