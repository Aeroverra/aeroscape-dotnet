# Round 16-1 Bug Fixes

## Fixed Issues

### 1. CRITICAL: Silent buffer underrun in RsReader.ReadByte()
**Fixed in**: AeroScape.Server.Network/Protocol/PacketDecoders.cs (line 19)
**Issue**: ReadByte() was using TryRead() without checking the return value, silently returning 0 on buffer underrun
**Fix**: Added proper error checking that throws InvalidOperationException when attempting to read beyond buffer
**Impact**: Prevents silent data corruption and makes buffer underrun errors visible

### 2. ObjectOption2Decoder type mismatch  
**Fixed in**: AeroScape.Server.Network/Protocol/PacketDecoders.cs (lines 453-467)
**Issue**: ObjectOption2Decoder was returning PlayerOption2Message instead of ObjectOption2Message
**Fix**: 
- Changed MessageType to return ObjectOption2Message
- Properly reads 6-byte packet structure: objectId (2), objectX (2), objectY (2)
- Added clarifying comment about Java code being misnamed/repurposed
**Impact**: Object interactions now have proper coordinate data instead of invalid player data

## Commit Details
- Commit: 73a4370
- Pushed to: master branch
- Total changes: 2 critical bug fixes in PacketDecoders.cs