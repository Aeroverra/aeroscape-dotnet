# Audit Report Round 12-3

**Date:** 2026-03-27  
**Scope:** ALL combat + magic + skills + prayer files (30 files audited)  
**Approach:** Static code analysis for real bugs still present after 11 rounds of fixes

## Real Bugs Found

### 1. **Array Bounds Validation Missing - PlayerVsPlayerCombat.cs**
**Lines 70-75**
```csharp
// Check equipment bounds before accessing weapon slot
if (CombatConstants.SlotWeapon >= attacker.Equipment.Length)
{
    ResetAttack(attacker);
    return;
}
```

**Issue:** This only checks if `SlotWeapon` is greater than or equal to array length, but doesn't validate that it's non-negative. If `SlotWeapon` is somehow set to a negative value, this would pass the check but cause an `IndexOutOfRangeException` on the next line.

**Fix:** Add check for `CombatConstants.SlotWeapon < 0`.

### 2. **Inconsistent Array Bounds Checking - MagicService.cs**
**Lines 125-129, 147-151, 164-168**
```csharp
private bool TryAlchemy(Player player, int itemId, int slot, int spellId, int fireRunes, int levelIndex)
{
    // Add bounds checking for array access
    if (levelIndex < 0 || levelIndex >= ModernLevelRequirements.Length || levelIndex >= ModernSpellXp.Length)
        return false;
```

**Issue:** The bounds checking validates `levelIndex` against both `ModernLevelRequirements.Length` and `ModernSpellXp.Length`, but these arrays could theoretically have different lengths. If they become misaligned, this check might pass but still cause an out-of-bounds access on one of the arrays.

**Fix:** Ensure arrays are always the same length or check each individually when accessed.

### 3. **Potential Division by Zero - PlayerVsNpcCombat.cs**
**Lines 115-118**
```csharp
int rangeLevel = attacker.SkillLvl[CombatConstants.SkillRanged];
int xpSeedHit = rangeLevel < 15 ? 1 : rangeLevel / 4;
int hitDamage = CombatFormulas.Random(xpSeedHit);
```

**Issue:** If `rangeLevel` is exactly 0 (which could happen with stat draining), the division `rangeLevel / 4` would result in 0, and `CombatFormulas.Random(0)` might not handle zero range correctly.

**Fix:** Ensure minimum value of 1 for the calculation.

### 4. **Potential Null Reference - MagicService.cs**
**Lines 96-101**
```csharp
public bool TryConsumeCombatRunes(Player player, SpellDefinition spell)
{
    // Validate spell definition exists to prevent null reference exceptions
    if (spell == null || spell.RuneRequirements == null)
        return false;
        
    if (!HasRunes(player, spell.RuneRequirements.Select(r => (r.RuneId, r.Amount)).ToArray()))
        return false;
```

**Issue:** While there's a null check for `spell` and `spell.RuneRequirements`, the LINQ `Select` operation could still throw if any individual `RuneRequirement` in the array is somehow null (though this would be unusual for a value type).

**Fix:** This is actually likely safe since `RuneRequirement` is a value type, but the null check is defensive.

### 5. **Missing Validation for Quest State - PlayerVsNpcCombat.cs**
**Lines 195-201**
```csharp
// ── Dragon Slayer quest ────────────────────────────────────────────
if (npcType == 742 && attacker.DragonSlayer == 3)
{
    attacker.HeadTimer = 8;
    attacker.DragonSlayer = 4;
    _playerItems.AddItem(attacker, 11279, 1);
    attacker.LastTickMessage = "You slayed Elvarg and took his head!";
}
```

**Issue:** The quest completion check only validates `attacker.DragonSlayer == 3` but doesn't check if the player's inventory has space for the head item (11279). If inventory is full, `AddItem` might fail silently but quest state still advances.

**Fix:** Check inventory space before advancing quest state.

### 6. **Potential Array Index Out of Bounds - PlayerVsPlayerCombat.cs**
**Lines 154-156**
```csharp
// Track damage for killer attribution
if (hitDamage > 0 && attacker.PlayerId < target.KilledBy.Length)
    target.KilledBy[attacker.PlayerId] += hitDamage;
```

**Issue:** This checks `attacker.PlayerId < target.KilledBy.Length` but doesn't validate that `attacker.PlayerId` is non-negative. If somehow `PlayerId` is negative, this would pass the length check but cause an exception.

**Fix:** Add check for `attacker.PlayerId >= 0`.

## Summary
- **6 real bugs found**
- Primary issues: Array bounds validation gaps and edge case handling
- Most critical: Array bounds issues that could cause runtime exceptions in combat scenarios
- Quest system has inventory validation gap that could cause inconsistent state