# Critical Fixes TODO - Based on Java Port Audit

## ✅ COMPLETED FIXES

### 1. **ObjectOption2 Decoder Bug** (Audit 1) ✅ FIXED
- **Issue**: ObjectOption2Decoder returns PlayerOption2Message instead of ObjectOption2Message
- **Fix**: Updated decoder to return ObjectOption2Message with proper coordinate/ID parsing
- **File**: AeroScape.Server.Network/Protocol/PacketDecoders.cs

### 2. **Walking State Management** (Audit 1) ✅ FIXED  
- **Issue**: Walking decoder only handles coordinates, missing state reset
- **Fix**: Added state resets in WalkMessageHandler (interaction flags, autocast, face-to)
- **File**: AeroScape.Server.Core/Handlers/WalkMessageHandler.cs

### 3. **DropItem Pet System** (Audit 1) ✅ PARTIAL FIX
- **Issue**: Basic 6-line implementation missing 95% of functionality
- **Fix**: Added pet summoning logic for 5 pet types, untradable item handling, LoadedBackup timer
- **File**: AeroScape.Server.Core/Handlers/DropItemMessageHandler.cs
- **TODO**: Need to implement NPC spawning method and destroy item interface

### 4. **Missing Assault Packet** (Audit 1) ✅ NOT A BUG
- **Issue**: Audit mentioned missing Assault packet
- **Finding**: "Assault" refers to Barbarian Assault minigame, not PVP. PVP uses PlayerOption1 (already implemented)

### 5. **Command System HouseHeight Bug** (Audit 5) ✅ NOT A BUG
- **Issue**: Commands reference undefined HouseHeight
- **Finding**: HouseHeight property exists on Player class and is properly used

## ⚠️ HIGH PRIORITY (Major Features Missing)

### 6. **Grand Exchange Service** (Audit 4) ✅ PLACEHOLDER CREATED
- **Issue**: Database models exist but NO service implementation
- **Fix**: Created placeholder GrandExchangeService.cs with stub methods
- **File**: AeroScape.Server.Core/Services/GrandExchangeService.cs  
- **TODO**: Need Player GE properties, full order matching, persistence

### 7. **House System** (Audit 2 & 5)
- **Issue**: Entire house system missing
- **Impact**: Player housing non-functional
- **Files**: Need house coordinate arrays and management

### 8. **Drop Party System** (Audit 2)
- **Issue**: Drop party timer and logic completely missing
- **Impact**: Drop party events non-functional

### 9. **Missing Packet Decoders** (Audit 1)
- Construction.java
- Prayer.java
- BountyHunter.java

### 10. **Prayer Integration** (Audit 4) ✅ PARTIAL FIX
- **Issue**: Service exists but not integrated into combat/handlers
- **Fix**: Added strength prayer multipliers to PvP melee combat
- **TODO**: Add prayer bonuses to NPC combat, ranged/magic prayers

## 📊 MEDIUM PRIORITY

### 11. **Equipment Slot Constants** (Audit 1) ✅ FIXED
- **Issue**: Magic numbers used instead of named constants for equipment slots
- **Fix**: Updated PlayerUpdateWriter.cs to use SlotHead, SlotChest, SlotShield, etc. constants

### 12. **NPC Retribution Prayer** (Audit 2)
- Missing explosion damage logic

### 13. **Magic Combat Integration** (Audit 4)
- Service exists but combat damage missing

### 14. **Clan Chat Advanced Features** (Audit 5)
- Rank management, loot share calculation

## Order of Attack:
1. Fix ObjectOption2 decoder bug first (breaks everything)
2. Fix Walking state management
3. Fix Command HouseHeight crash
4. Implement missing Assault packet
5. Fix DropItem functionality
6. Create Grand Exchange service
7. Continue with remaining issues