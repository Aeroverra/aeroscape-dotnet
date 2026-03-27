# Round 16 Combat Audit Report

**Scope:** ALL combat files (PlayerVsPlayerCombat, PlayerVsNpcCombat, NpcVsPlayerCombat, CombatFormulas, WeaponData)
**Date:** 2026-03-27
**Status:** After 15 rounds of fixes

## Bugs Found

### 1. **Array Bounds Safety Missing in NpcVsPlayerCombat**
**File:** `NpcVsPlayerCombat.cs`
**Location:** Line 43-44
**Issue:** Direct array access without bounds checking
```csharp
var target = _engine.Players[npc.AttackPlayer];
```
**Risk:** IndexOutOfRangeException if `npc.AttackPlayer` exceeds array bounds
**Fix Needed:** Add try-catch or bounds check like PlayerVsPlayerCombat does

### 2. **Equipment Array Bounds Check Missing in PlayerVsNpcCombat**
**File:** `PlayerVsNpcCombat.cs` 
**Location:** Line 56
**Issue:** Direct access without length validation
```csharp
int weaponId = attacker.Equipment[CombatConstants.SlotWeapon];
```
**Risk:** IndexOutOfRangeException if Equipment array too small
**Fix Needed:** Add bounds check like PlayerVsPlayerCombat does at lines 67-72

### 3. **Equipment/EquipmentN Array Safety Missing in PlayerVsNpcCombat Ranged**
**File:** `PlayerVsNpcCombat.cs`
**Location:** Lines 226-227  
**Issue:** Missing bounds checks on both arrays
```csharp
int ammoId = attacker.Equipment[CombatConstants.SlotAmmo];
int ammoCount = attacker.EquipmentN[CombatConstants.SlotAmmo];
```
**Risk:** IndexOutOfRangeException if arrays too small
**Fix Needed:** Add bounds checks like PlayerVsPlayerCombat does at lines 177-183

### 4. **Potential Division by Zero in CombatFormulas**
**File:** `CombatFormulas.cs`
**Location:** Line 35
**Issue:** No validation that `range > 0` in edge cases
```csharp
return _rng.Value.Next(range + 1);
```
**Risk:** ArgumentOutOfRangeException if range is negative or causes overflow
**Fix Needed:** Additional validation beyond the current `<= 0` check

## Clean Files

- **WeaponData.cs** - No bugs found
- **CombatFormulas.cs** - Minor edge case issue only

## Summary

**4 bugs found** requiring array bounds safety fixes in NpcVsPlayerCombat and PlayerVsNpcCombat. These are critical runtime safety issues that could cause crashes during combat.