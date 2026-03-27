# AUDIT ROUND 8 - PacketDecoders.cs vs Java io/packets/

**Audit Scope**: ALL packet decoders in PacketDecoders.cs vs Java io/packets/  
**Date**: 2026-03-26  
**Previous Rounds**: 7 fixes already applied  

## Bug #1: EquipItemDecoder ignores interface ID extraction

**File**: PacketDecoders.cs  
**Method**: EquipItemDecoder.Decode()  
**Severity**: Medium  

**Issue**: The C# decoder completely ignores interface ID extraction while the Java version reads it:

**Java** (Equipment.java:76-77):
```java
int junk1 = p.stream.readDWord_v2();
int wearId = p.stream.readUnsignedWordBigEndian();
int index = p.stream.readUnsignedByte();
int junk2 = p.stream.readUnsignedByte();
```

**C#** (Lines 423-428):
```csharp
int junk = r.ReadDWordV2(); // Java ignores this completely
int itemId = r.ReadUnsignedWordBigEndian();
int slot = r.ReadByte();
r.ReadByte();
return new EquipItemMessage(itemId, slot, -1); // No interfaceId extracted in Java
```

**Problem**: While the C# comment correctly states "Java ignores this completely", the C# decoder constructs the message with `-1` as interfaceId, which might cause downstream issues if any handlers expect a valid interface ID.

## Bug #2: ObjectOption2Decoder incorrect packet interpretation

**File**: PacketDecoders.cs  
**Method**: ObjectOption2Decoder.Decode()  
**Severity**: High  

**Issue**: Complete mismatch between packet structure understanding.

**Java** (ObjectOption2.java): This handles **player interactions**, not object interactions. The Java class reads player-related data.

**C#** (Lines 481-489):
```csharp
// Opcode 228 is 6 bytes: word (playerId) + word (objectX) + word (objectY)
// NOTE: Java ObjectOption2.java handles player interactions, not object interactions
var r = new RsReader(payload);
int firstWord = r.ReadUnsignedWord();
int objectX = r.ReadUnsignedWord(); 
int objectY = r.ReadUnsignedWord();
return new ObjectOption2Message(firstWord, objectX, objectY);
```

**Problem**: The C# decoder creates an `ObjectOption2Message` but the NOTE correctly identifies this should be handling player interactions. The packet structure and message type are mismatched.

## Bug #3: ItemOnItemDecoder packet order inconsistency  

**File**: PacketDecoders.cs  
**Method**: ItemOnItemDecoder.Decode()  
**Severity**: Medium  

**Issue**: Different variable naming suggests potential confusion about packet order.

**Java** (ItemOnItem.java:26-27):
```java
int usedWith = player.stream.readSignedWordBigEndian();
int itemUsed = player.stream.readSignedWordA();
```

**C#** (Lines 647-648):
```csharp
int usedWithId = r.ReadSignedWordBigEndian();
int itemUsedId = r.ReadSignedWordA();
```

**Problem**: While the read order is technically correct, the C# variable names (`usedWithId`, `itemUsedId`) don't match Java semantics (`usedWith`, `itemUsed`). The constructor call order also differs:

**C#**: `new ItemOnItemMessage(usedWithId, itemUsedId, ...)`  
**Java semantics**: First param should be `usedWith`, second should be `itemUsed`

This could cause confusion about which item is being used on which.

## Summary

**3 bugs found** requiring fixes:

1. **EquipItemDecoder**: Remove hardcoded `-1` for interfaceId or properly extract it
2. **ObjectOption2Decoder**: Fix fundamental mismatch - either handle as player interaction or correct packet interpretation  
3. **ItemOnItemDecoder**: Align variable semantics with Java implementation for clarity

All other decoders appear to correctly match their Java counterparts.