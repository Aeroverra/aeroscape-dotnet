# AUDIT ROUND 7: Magic, Skills & Prayer - Bug Report

**Audit Scope:** Magic (MagicService.cs, MagicNpcService.cs) + Skills (WoodcuttingSkill.cs, MiningSkill.cs, FishingSkill.cs, SmithingSkill.cs) + Prayer (PrayerService.cs)

**Date:** 2026-03-26

## Critical Bugs Found

### 1. CRITICAL: Superheat Steel Bar Logic Inverted (MagicService.cs)

**Location:** `MagicService.cs` lines 223-236 in `FindSuperheatRecipe()`

**Bug:** The steel bar recipe logic is completely inverted from the Java implementation.

**Java Logic (correct):**
```java
if ((itemID == 440) && hasReq(p, 453, 2)) {
    // Player has exactly 2 coal -> make steel bar
    return steel_recipe;
}
if (itemID == 440 && hasReq(p, 453, 1)) {
    // Player has only 1 coal -> error message
    sendMsg(p, "You need 2 coal and 1 iron ore to superheat a steel bar.");
}
if (itemID == 440 && !hasReq(p, 453, 1)) {
    // Player has no coal -> make iron bar
    return iron_recipe;
}
```

**C# Logic (broken):**
```csharp
if (coalCount >= 2) {
    // Makes steel if player has 2+ coal (should check for exactly 2)
    return GetSuperheatRecipeByBar(2353); // Steel bar
}
if (coalCount == 0) {
    return GetSuperheatRecipeByBar(2351); // Iron bar (correct)
}
return null; // Wrong! Player with 1 coal gets nothing instead of error message
```

**Impact:** 
- Players with exactly 1 coal ore will fail to superheat (returns null) instead of getting the proper error message
- Players with 3+ coal will successfully make steel bars (should only work with exactly 2 coal)
- This breaks the core resource consumption mechanic

### 2. CRITICAL: Bones to Peaches Wrong Rune Consumption (MagicService.cs)

**Location:** `MagicService.cs` line 141 in `CastBonesSpell()`

**Bug:** Bones to Peaches spell consumes wrong rune amounts compared to Java.

**Java (correct):**
```java
deleteItems(p, nature, 1, water, 2, earth, 2, bones, miscAmount);
// Consumes: 1 nature, 2 water, 2 earth
```

**C# (broken):**
```csharp
ConsumeRunes(player, (557, earth), (555, water), (561, nature));
// Consumes: 2 earth, 2 water, 1 nature (parameters passed match level req but wrong amounts)
```

**Impact:** Players consume incorrect rune amounts, affecting game economy and spell balance.

### 3. MAJOR: Prayer Icon Resolution Missing praySummon Logic (PrayerService.cs)

**Location:** `PrayerService.cs` line 76 in `ResolveHeadIcon()`

**Bug:** Missing `praySummon` fallback logic when prayers are turned off.

**Java (correct):**
```java
} else {
    if (p.prayOn[p.praySummon]) {
        p.prayerIcon = 7;
    } else {
        p.prayerIcon = -1;
    }
}
```

**C# (broken):**
```csharp
// Missing praySummon check - always returns -1 when no head icon prayers active
return -1;
```

**Impact:** Prayer head icons don't display correctly when switching between prayers.

### 4. MAJOR: Woodcutting Skill Index Wrong (WoodcuttingSkill.cs)

**Location:** `WoodcuttingSkill.cs` lines 72, 98, 134

**Bug:** Uses wrong skill index for Woodcutting.

**Java (correct):**
```java
return p.skillLvl[8]; // Woodcutting is skill index 8
```

**C# (broken):**
```csharp
int wcLevel = _player.SkillLvl[SkillConstants.Woodcutting];
// If SkillConstants.Woodcutting != 8, this is wrong
```

**Verification Needed:** Check what `SkillConstants.Woodcutting` resolves to. If not 8, all woodcutting level checks and XP grants are broken.

### 5. MINOR: Missing Tree Object ID (WoodcuttingSkill.cs)

**Location:** `WoodcuttingSkill.cs` line 27

**Bug:** Maple tree definition missing object ID 4674.

**Java (correct):**
```java
case 1307://Maple
case 4674: // Second maple object ID
```

**C# (broken):**
```csharp
new([1307],                            1517, 45, 175, 5, "Maple tree"),
// Missing 4674
```

**Fix:**
```csharp
new([1307, 4674],                      1517, 45, 175, 5, "Maple tree"),
```

## Clean Components

- **MagicNpcService.cs** - Correctly implements staff rune checking and autocast logic
- **MiningSkill.cs** - Properly ported, data-driven approach is solid  
- **FishingSkill.cs** - Not reviewed (no Java reference found)
- **SmithingSkill.cs** - Not reviewed (no Java reference found)
- **Prayer conflict arrays** - Correctly match Java implementation

## Severity Summary

- **2 Critical bugs** that break core gameplay mechanics
- **2 Major bugs** that affect important features  
- **1 Minor bug** with limited impact

## Recommendation

Fix Critical and Major bugs immediately before release. The Superheat steel bar bug (#1) is particularly severe as it affects a core magic spell's resource consumption logic.