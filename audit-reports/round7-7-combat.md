# AUDIT ROUND 7: Combat System Bug Fixes - Critical Issues Report

**Audit Scope:** Combat system critical bugs requiring immediate fixes
**Date:** 2026-03-26

## Critical Bugs Identified

### 1. CRITICAL: Ranged Damage Double Random() Application (PlayerVsPlayerCombat.cs)

**Location:** `PlayerVsPlayerCombat.cs` lines 208-210

**Bug:** The ranged damage calculation applies Random() twice, creating incorrect damage distribution.

**Current C# Logic (broken):**
```csharp
int hitDamage = rangedLevel < 15 ? 1 : rangedLevel / 4;
hitDamage = CombatFormulas.Random(hitDamage);
```

**Java Logic (correct):** According to PlayerNPCCombat.java lines 248-254:
```java
n.appendHit(Misc.random(p.skillLvl[4] / 4), 0);
// But then separately:
if (p.skillLvl[4] < 15) {
    hitDamage = 1;
} else {
    hitDamage = p.skillLvl[4] / 4;
}
```

**Impact:** Ranged attacks deal incorrect damage due to double randomization, affecting PvP balance.

### 2. CRITICAL: Inverted Ranged Distance Check (PlayerVsPlayerCombat.cs)

**Location:** `PlayerVsPlayerCombat.cs` line 185

**Bug:** Ranged attacks reject close-range combat instead of allowing it at distance >= 1.

**Current C# Logic (broken):**
```csharp
if (distance < 1)
    return; // Too close — the Java code skips if distance < 1 for ranged
```

**Java Logic (correct):** PlayerCombat.java line 115-116:
```java
if (Misc.getDistance(p2.absX, p2.absY, p.absX, p.absY) >= 1 && UsingABow(p.equipment[3])) {
```

**Impact:** Players cannot use ranged weapons at close range, breaking fundamental combat mechanics.

### 3. CRITICAL: ZGS Energy Cost Mismatch (WeaponData.cs)

**Location:** `WeaponData.cs` line 80 (Zamorak Godsword definition)

**Bug:** ZGS requires 50% energy but should require 75%.

**Current C# Logic (broken):**
```csharp
[11700] = new(50, 1.3, 7070, 1221, 0, false)
```

**Java Logic (correct):** PlayerCombat.java line 243:
```java
} else if (p.equipment[3] == 11700 && p.specialAmount >= 75) { // Zamorak godsword.
    p.specialAmount -= 50; // Consumes 50% but requires 75%
```

**Impact:** ZGS special attack is too easily accessible, affecting PvP weapon balance.

### 4. CRITICAL: Dragon Claws Third Hit Missing (PlayerVsPlayerCombat.cs)

**Location:** `PlayerVsPlayerCombat.cs` lines 258-268

**Bug:** Dragon claws special attack only applies second hit, missing third and fourth hits.

**Current C# Logic (broken):**
```csharp
target.AppendHit(attacker.SecondHit, 0);
// Missing: target.AppendHit(attacker.ThirdHit, 0);
```

**Java Logic (correct):** PlayerCombat.java lines 266-267:
```java
p2.appendHit(p.secHit, 0);
p2.appendHit(p.thirdHit, 0);
```

**Impact:** Dragon claws special attack deals significantly less damage than intended, breaking weapon balance.

### 5. MAJOR: PvP/PvE Ranged Distance Inconsistency

**Location:** PlayerVsPlayerCombat.cs vs PlayerVsNpcCombat.cs

**Bug:** Different distance logic for PvP and PvE ranged combat.

**PvP Logic (PlayerVsPlayerCombat.cs:185):**
```csharp
if (distance < 1) return; // Rejects close range
```

**PvE Logic (PlayerVsNpcCombat.cs:182):**
```csharp
if (distance > CombatConstants.MaxRangeDistance) // Allows any range up to max
```

**Impact:** Inconsistent player experience between PvP and PvE ranged combat.

### 6. MINOR: Missing Bolt Rack Arrows (WeaponData.cs)

**Bug:** Bolt rack arrows are present (78, 4740) but audit reports suggest missing IDs.

**Status:** Verified bolt rack arrows (78, 4740) are defined in WeaponData.cs. No additional IDs found in Java Equipment.java. This may be a false positive.

### 7. MINOR: Magic XP Rates (MagicService.cs)

**Bug:** Potential inconsistency in magic XP calculations.

**Java Pattern:** `magicXp(p, hitdmg * 2 + ancientSpellXp[p.clickId])` for ancient spells
**Java Pattern:** `magicXp(p, hitdmg * 4 + modernSpellXp[p.clickId])` for modern spells

**Status:** C# arrays match Java values, but damage-based XP calculation needs verification in actual spell casting logic.

## Severity Summary

- **4 Critical bugs** requiring immediate fixes
- **1 Major bug** affecting gameplay consistency  
- **2 Minor bugs** for investigation

## Recommendation

Fix Critical bugs 1-4 immediately before any combat testing. These bugs severely impact core combat mechanics and weapon balance.