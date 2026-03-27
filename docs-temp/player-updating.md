# Player Updating

Player updating is a complex packet (opcode 216, VAR_SHORT) that uses **bit-level access** to efficiently describe player movements and appearance changes.

## Packet Structure

The player update packet (opcode 216) is a VAR_SHORT packet with the following high-level structure:

```
[Opcode: 216] [Size: 2 bytes] [BitBuffer: variable] [UpdateBlocks: variable]
```

1. **Local Player Movement** - Movement info for the current player
2. **Other Players List Update** - Add/remove players from local list  
3. **Other Players Movement** - Movement for all players in local list
4. **Update Blocks** - Appearance/state changes for flagged players

## Movement Types (Bit-Level Format)

Movement information is packed into bits within the update mask. The exact bit counts are critical:

| Type | Bit Structure | Description |
|------|---------------|-------------|
| **0** | `1 bit` | **No movement (idle)**<br/>• 1 bit for update-required flag only |
| **1** | `3 + 1 bits` | **Walk**<br/>• 3 bits for direction (0–7)<br/>• 1 bit for update required |
| **2** | `3 + 3 + 1 bits` | **Run**<br/>• 3 bits for first direction<br/>• 3 bits for second direction<br/>• 1 bit for update required |
| **3** | `2 + 1 + 7 + 7 + 1 bits` | **Teleport**<br/>• 2 bits for plane/height (0–3)<br/>• 1 bit for clear waypoint queue<br/>• 7 bits for local X coordinate<br/>• 7 bits for local Y coordinate<br/>• 1 bit for update required |

## Direction Encoding (3-bit)

Directions are encoded as 3-bit values representing 8 possible movement directions:

| Value | Direction | Angle | Delta X,Y |
|-------|-----------|-------|-----------|
| 0 | North-West | 315° | -1, +1 |
| 1 | North | 0° | 0, +1 |
| 2 | North-East | 45° | +1, +1 |
| 3 | West | 270° | -1, 0 |
| 4 | East | 90° | +1, 0 |
| 5 | South-West | 225° | -1, -1 |
| 6 | South | 180° | 0, -1 |
| 7 | South-East | 135° | +1, -1 |

**Note**: The Y-axis in RuneScape increases towards the north (opposite of typical screen coordinates).

## Player Update Process

The player update packet follows a specific sequence:

### Phase 1: Local Player Movement
The packet starts with the local player's movement information:
```java
// Movement type determination
if (noMovement) {
    putBits(1, 0);  // No movement flag
    if (updateRequired) putBits(1, 1);
    else putBits(1, 0);
} else if (walking) {
    putBits(1, 1);  // Movement flag
    putBits(2, 1);  // Walk type
    putBits(3, direction);
    putBits(1, updateRequired ? 1 : 0);
} else if (running) {
    putBits(1, 1);  // Movement flag  
    putBits(2, 2);  // Run type
    putBits(3, direction1);
    putBits(3, direction2);
    putBits(1, updateRequired ? 1 : 0);
} else if (teleporting) {
    putBits(1, 1);  // Movement flag
    putBits(2, 3);  // Teleport type
    putBits(2, plane);
    putBits(1, clearWaypoints ? 1 : 0);
    putBits(7, localX);
    putBits(7, localY);  
    putBits(1, updateRequired ? 1 : 0);
}
```

### Phase 2: Other Players List Management
```java
putBits(8, localPlayersCount);  // Number of local players

for (Player player : localPlayers) {
    if (stillVisible(player)) {
        putBits(1, 1);  // Keep in list
        // Add movement bits (same format as local player)
    } else {
        putBits(1, 0);  // Remove from list
    }
}
```

### Phase 3: New Players Addition  
```java
for (Player player : newPlayersInRange) {
    putBits(11, player.getIndex());      // Player index (11 bits)
    putBits(1, 1);                       // Update required flag
    putBits(5, player.getLocalX());      // Local X (5 bits)
    putBits(3, player.getFaceDirection()); // Face direction (3 bits)  
    putBits(5, player.getLocalY());      // Local Y (5 bits)
}
```

### Phase 4: Update Blocks (Byte-level)
After the bit-packed movement section, the packet switches to byte-level writing for update blocks.

## Bit Packing Example

For a player walking north (direction 1) with no other updates:
```
Movement type: 1 (walk) = 2 bits
Direction: 1 (north) = 3 bits  
Update required: 0 (no) = 1 bit
Total: 6 bits
```

This efficient packing allows hundreds of player positions to be updated in a single packet.

## Local vs Global Player Lists

The client maintains two player lists:
1. **Local players** - Players currently visible on screen
2. **Global players** - All players the client knows about

The update process:
1. Update local player list (add/remove based on distance)
2. Update movement for all local players
3. Process appearance updates for flagged players

## Update Masks

When a player has the "update required" flag set, a mask byte follows indicating what changed:

| Bit | Update Type |
|-----|-------------|
| 0x1 | Graphics |
| 0x2 | Animation |
| 0x4 | Forced chat |
| 0x8 | Chat |
| 0x10 | Face entity |
| 0x20 | Appearance |
| 0x40 | Face coordinate |
| 0x80 | Primary hit |

Additional masks may use a second byte for extended updates.

## Appearance Block

The appearance block contains:
- Gender
- Head icons (prayer, skull)
- Equipment appearance IDs
- Clothing colors
- Animation IDs (stand, walk, run, etc.)
- Username
- Combat level
- Total skill level
- Hidden status