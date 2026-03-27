# Audit Round 7-3: Frame Writing Comparison

## Scope
- **C# Files**: LoginFrames.cs + GameFrames.cs + LoginHandler.cs
- **Java Reference**: Frames.java + Login.java
- **Focus**: Frame writes, byte order, interface IDs, tab assignments

## Critical Bugs Found

### 1. **Critical Shift Operator Bug in LoginFrames.cs:214**
**File**: `LoginFrames.cs`  
**Method**: `WriteMapRegion`  
**Line**: 214

```csharp
// C# - INCORRECT
int region = (xCalc << 8) + yCalc;
```

```java
// Java reference - Frames.java:1113
int region = yCalc + (xCalc << 1786653352);
```

**Issue**: The C# code uses `xCalc << 8` while Java uses `xCalc << 1786653352`. In Java, shift operators are masked to 5 bits for int (`shift & 31`), so `1786653352 % 32 = 8`, making it equivalent to `<< 8`. However, the C# implementation incorrectly places the addition operands in reverse order.

**Correct C# should be**: `int region = yCalc + (xCalc << 8);`

**Impact**: This will generate wrong region IDs, causing map data lookup failures and potential client disconnections.

### 2. **Tab Interface Assignment Bug in LoginFrames.cs**
**File**: `LoginFrames.cs`  
**Method**: `WriteSetInterfaces`  
**Lines**: 103-145

**Issue**: Multiple tab assignments use hardcoded slot numbers instead of the CombatConstants.SlotWeapon pattern seen elsewhere.

```csharp
// C# - INCONSISTENT
p.IsAncients = p.Equipment[3] == 4675 ? 1 : 0; // Line 102

// But GameFrames.cs uses:
p.IsAncients = p.Equipment[CombatConstants.SlotWeapon] == 4675 ? 1 : 0;
```

**Java reference** (Frames.java:1495):
```java
if (p.equipment[3] != 4675) {
    setInterface(p, 1, 548, 79, 192); //Magic tab
} else if (p.equipment[3] == 4675) {
    setInterface(p, 1, 548, 79, 193); //Magic tab  
    p.isAncients = 1;
}
```

**Impact**: Equipment slot 3 is hardcoded throughout LoginFrames.cs but uses a constant in GameFrames.cs, creating maintenance issues and potential bugs if slot constants change.

### 3. **Weapon Tab Assignment Logic Discrepancy**
**File**: `LoginFrames.cs`  
**Method**: `WriteWeaponTab`  
**Lines**: 178-185

```csharp
// C# LoginFrames.cs
string weapon = items.GetItemName(p.Equipment[3]); // Hardcoded slot 3
int attackTabId = usingHD ? 87 : 73;

// Later uses:
WriteSetInterface(w, 1, usingHD ? 746 : 548, attackTabId, childId);
```

```csharp  
// C# GameFrames.cs - DIFFERENT PATTERN
int weaponId = p.Equipment[CombatConstants.SlotWeapon];
string weapon = _items.GetItemName(weaponId);
int attackTabId = p.UsingHd ? 87 : 73;

SetTab(w, p, attackTabId, childId);
```

**Issue**: LoginFrames.cs uses direct slot indexing and WriteSetInterface, while GameFrames.cs uses constants and SetTab() wrapper method.

## Minor Issues

### 4. **Parameter Naming Convention Inconsistency**
**Files**: All frame files  
**Issue**: Java uses camelCase parameters (`casterX`, `casterY`) while C# uses PascalCase in some places but camelCase in others.

### 5. **Interface ID Constants**
Some interface IDs are hardcoded in LoginFrames.cs that should potentially use constants, but this matches the Java pattern so is not considered a bug.

## Summary
- **Critical Bugs**: 2 critical bugs found
- **Minor Issues**: 2 minor issues found  
- **Total Issues**: 4

The region calculation bug is the most severe and would break map loading functionality.