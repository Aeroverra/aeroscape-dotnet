# Combat System Audit Report - Round 17

After comprehensive examination of all combat files, I have identified several critical bugs and issues:

## 1. CRITICAL: Integer Overflow in CombatFormulas.Random()

**File:** `CombatFormulas.cs`, lines 20-27

**Issue:** The Random() method can cause integer overflow when `range == int.MaxValue - 1`:

```csharp
// Additional safety: ensure range + 1 doesn't overflow
if (range > int.MaxValue - 1) return _rng.Value.Next(range);
return _rng.Value.Next(range + 1);
```

**Bug:** When `range == int.MaxValue - 1`, the condition `range > int.MaxValue - 1` is FALSE, so it proceeds to `_rng.Value.Next(range + 1)`, which causes integer overflow since `(int.MaxValue - 1) + 1 = int.MaxValue` is still not a valid upper bound for Next() which requires exclusive upper bound.

**Fix:** The condition should be `>=` not `>`:
```csharp
if (range >= int.MaxValue - 1) return _rng.Value.Next(range);
```

## 2. CRITICAL: Missing Autocast State Reset in PlayerVsNpcCombat

**File:** `PlayerVsNpcCombat.cs`, lines 65-71

**Issue:** When resetting autocast due to no staff, the code only resets local state but doesn't clear the equipped spell:

```csharp
if (attacker.AutoCasting && !MagicNpcService.HasAutocastStaff(attacker))
{
    attacker.AutoCasting = false;
    attacker.AutoCastSpellId = -1;
}
```

**Bug:** This doesn't match the Java behavior which would also need to update the client interface. The player's spell selection UI would still show the spell as selected even though autocasting is disabled.

## 3. BUG: Crystal Bow Damage Inconsistency

**File:** `PlayerVsPlayerCombat.cs`, lines 242-243

**Issue:** Crystal bow uses hardcoded maxHit of 30:

```csharp
// Crystal bow uses Misc.random(30) in Java
int maxHit = 30;
```

**File:** `PlayerVsNpcCombat.cs`, line 158

**Issue:** Same hardcoded value but called "crystalBowDamage" here for clarity.

**Bug:** This creates an artificial cap that doesn't scale with player stats, unlike all other combat styles. The comment says it matches Java, but this seems like a legacy bug being preserved rather than intended behavior.

## 4. BUG: Missing Bounds Check in ProcessNpcKill

**File:** `PlayerVsNpcCombat.cs`, lines 201-226

**Issue:** The barrows array bounds check only validates the index is non-negative and less than array length, but doesn't validate that the Barrows array itself exists or has the expected size:

```csharp
if (barrowsIndex >= 0 && barrowsIndex < attacker.Barrows.Length)
    attacker.Barrows[barrowsIndex] = true;
```

**Bug:** If `attacker.Barrows` is null or has fewer than 6 elements, this could still throw an exception or silently fail.

## 5. RACE CONDITION: Thread-Unsafe Follow Target Assignment

**File:** `PlayerVsPlayerCombat.cs`, lines 83-85

**Issue:** These assignments are not atomic:

```csharp
attacker.FollowPlayerIndex = target.PlayerId;
attacker.FollowingPlayer = true;
```

**Bug:** Another thread could read FollowingPlayer=true but see an old FollowPlayerIndex value, causing the player to follow the wrong target.

## 6. BUG: Special Attack Energy Display Not Updated

**File:** `PlayerVsNpcCombat.cs`, line 114

**Issue:** When special attack fails due to insufficient energy:

```csharp
else
{
    attacker.UsingSpecial = false;
}
```

**Bug:** Missing `attacker.SpecialAmountUpdateReq = true;` which means the client won't update the special attack bar to show the current energy level.

## 7. BUG: Inconsistent XP Calculation for Controlled Style

**File:** `PlayerVsPlayerCombat.cs`, lines 377-379
**File:** `PlayerVsNpcCombat.cs`, lines 263-267

**Issue:** HP XP for controlled style is different between PvP and PvE:
- PvP: `3.0 * hitDamage * CombatConstants.CombatXpRate`
- PvE: `3.0 * hitDamage * CombatConstants.CombatXpRate` (inside the switch)

**Bug:** In PvP, controlled style gives less total XP because HP XP is given at 3.0x rate while other styles get 4.0x for their main skill + 3.0x HP. In PvE, the controlled style correctly gives 3.0x HP XP.

## 8. BUG: NPC Attack Animation Selection Incomplete

**File:** `NpcVsPlayerCombat.cs`, lines 96-105

**Issue:** The GetNpcAttackAnim method only handles 5 specific NPC types:

```csharp
return npc.NpcType switch
{
    9 or 21 or 20 => 451,   // Guard / hero
    2 or 1 => 422,          // Man / woman
    _ => npc.AttackEmote > 0 ? npc.AttackEmote : 422,
};
```

**Bug:** Uses hardcoded fallback animation 422 (man punch) for all NPCs without a defined AttackEmote, which would look wrong for many creature types.

## 9. BUG: Vengeance Damage Attribution Edge Case

**File:** `PlayerVsPlayerCombat.cs`, lines 280-291

**Issue:** Vengeance damage tracking has bounds check but no null check:

```csharp
if (vengDamage > 0 && target.PlayerId >= 0 && target.PlayerId < attacker.KilledBy.Length)
    attacker.KilledBy[target.PlayerId] += vengDamage;
```

**Bug:** If `attacker.KilledBy` is null, this will throw NullReferenceException despite the bounds check.

## 10. PERFORMANCE BUG: Excessive Distance Calculations

**File:** `PlayerVsPlayerCombat.cs`, line 94
**File:** `PlayerVsNpcCombat.cs`, line 62

**Issue:** Distance is calculated even when not needed:

```csharp
int distance = CombatFormulas.GetDistance(attacker.AbsX, attacker.AbsY, target.AbsX, target.AbsY);
if (WeaponData.IsBow(weaponId))
{
    ProcessRangedAttack(attacker, target, distance);
}
else if (distance <= 1)
{
    ProcessMeleeAttack(attacker, target);
}
```

**Bug:** For melee weapons, we calculate expensive square root distance when a simple Manhattan distance check `Math.Abs(dx) <= 1 && Math.Abs(dy) <= 1` would suffice.

## Summary

Found 10 bugs ranging from critical integer overflow and race conditions to performance issues and missing client updates. The combat system needs careful synchronization for thread safety and more defensive null/bounds checking throughout.