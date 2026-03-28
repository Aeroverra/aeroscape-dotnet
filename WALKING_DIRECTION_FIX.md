# Walking Direction Fix

## Issue Found
The character was walking in the wrong direction due to **double subtraction of region base coordinates**.

## Root Cause
In `WalkDecoder.cs`, the decoder subtracts the region base from firstX/firstY:
```csharp
int firstX = r.ReadUnsignedWordBigEndianA() - regionBaseX;
int firstY = r.ReadUnsignedWordA() - regionBaseY;
```

Then in `WalkQueue.cs` HandleWalk(), it was subtracting the region base AGAIN:
```csharp
int firstX = message.FirstX - (player.MapRegionX - 6) * 8;
int firstY = message.FirstY - (player.MapRegionY - 6) * 8;
```

This caused the coordinates to be offset by twice the region base, making the character walk in the wrong direction.

## The Fix
Remove the double subtraction in `WalkQueue.HandleWalk()`:
```csharp
// FirstX/FirstY already have region base subtracted in WalkDecoder
AddToWalkingQueue(player, message.FirstX, message.FirstY);
```

## Verification
- The Java code in `Walking.java` only subtracts region base once when reading the packet
- The direction arrays (DirectionDeltaX/Y) match exactly between Java and C#
- The direction encoding (0=NW through 7=SE) is correct

## Testing Required
1. Click on different spots on the map
2. Character should walk directly to the clicked location
3. Character should stop at the destination without queue overflow
4. Both single-click (5-byte packets) and path-following should work correctly