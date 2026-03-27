using System;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Engine;
using Microsoft.Extensions.Logging;

namespace AeroScape.Server.Core.Combat;

/// <summary>
/// Handles Player vs Player combat processing.
/// Called once per tick for each player with AttackingPlayer == true.
/// Ported from PlayerCombat.attackPlayer(Player p) in Java.
/// </summary>
public class PlayerVsPlayerCombat
{
    private readonly GameEngine _engine;
    private readonly ILogger<PlayerVsPlayerCombat> _logger;

    public PlayerVsPlayerCombat(GameEngine engine, ILogger<PlayerVsPlayerCombat> logger)
    {
        _engine = engine;
        _logger = logger;
    }

    /// <summary>
    /// Process one tick of PvP combat for the given attacker.
    /// </summary>
    public void ProcessAttack(Player attacker)
    {
        if (attacker.AttackPlayer <= 0)
            return;

        if (attacker.AttackPlayer >= GameEngine.MaxPlayers)
        {
            ResetAttack(attacker);
            return;
        }

        // Thread-safe access to prevent race conditions
        Player target;
        try
        {
            target = _engine.Players[attacker.AttackPlayer];
        }
        catch (IndexOutOfRangeException)
        {
            ResetAttack(attacker);
            return;
        }

        if (target == null || attacker.IsDead || target.IsDead || target.Disconnected[1])
        {
            ResetAttack(attacker);
            return;
        }

        // ── Area restriction checks ────────────────────────────────────────
        if (!CanAttackInArea(attacker, target))
        {
            ResetAttack(attacker);
            return;
        }

        // ── Combat delay gate ──────────────────────────────────────────────
        if (attacker.CombatDelay > 0)
            return;

        // ── Wilderness range check ─────────────────────────────────────────
        bool inWild = Player.IsWildernessArea(attacker.AbsX, attacker.AbsY);
        bool targetInWild = Player.IsWildernessArea(target.AbsX, target.AbsY);

        if (inWild && targetInWild && !IsInArena(attacker))
        {
            if (!CombatFormulas.IsInWildRange(attacker.CombatLevel, attacker.AbsY,
                    target.CombatLevel, target.AbsY))
            {
                // Out of wild range — need to move deeper
                ResetAttack(attacker);
                return;
            }
        }

        // ── Follow target ──────────────────────────────────────────────────
        attacker.FollowPlayerIndex = target.PlayerId;
        attacker.FollowingPlayer = true;

        // ── Determine combat type and execute ──────────────────────────────
        // Check if equipment array has enough slots for weapon slot access
        if (attacker.Equipment.Length <= CombatConstants.SlotWeapon)
        {
            ResetAttack(attacker);
            return;
        }
        
        int weaponId = attacker.Equipment[CombatConstants.SlotWeapon];
        int distance = CombatFormulas.GetDistance(attacker.AbsX, attacker.AbsY,
            target.AbsX, target.AbsY);

        if (WeaponData.IsBow(weaponId))
        {
            ProcessRangedAttack(attacker, target, distance);
        }
        else if (distance <= 1)
        {
            ProcessMeleeAttack(attacker, target);
        }
        // else: not in range yet, keep following
    }

    /// <summary>
    /// Execute a melee attack against another player.
    /// </summary>
    private void ProcessMeleeAttack(Player attacker, Player target)
    {
        // Calculate prayer bonus for strength
        double prayerMultiplier = GetStrengthPrayerMultiplier(attacker);
        
        int maxHit = CombatFormulas.MaxMeleeHit(
            attacker.SkillLvl[CombatConstants.SkillStrength],
            attacker.EquipmentBonus[CombatConstants.BonusStrength],
            prayerMultiplier);
        int hitDamage = CombatFormulas.Random(maxHit);

        int weaponId = attacker.Equipment[CombatConstants.SlotWeapon];

        // ── Special attack handling ────────────────────────────────────────
        if (attacker.UsingSpecial && WeaponData.PlayerVsPlayerSpecialAttacks.TryGetValue(weaponId, out var spec))
        {
            if (attacker.SpecialAmount >= spec.EnergyCost)
            {
                attacker.SpecialAmount -= spec.EnergyCost;
                attacker.UsingSpecial = false;
                attacker.SpecialAmountUpdateReq = true;

                attacker.RequestAnim(spec.AnimId, 0);
                if (spec.AttackerGfx > 0)
                    attacker.RequestGfx(spec.AttackerGfx, 0);
                if (spec.TargetGfx > 0)
                    target.RequestGfx(spec.TargetGfx, 100);

                if (spec.IsMultiHit)
                {
                    ProcessDragonClawsSpec(attacker, target, hitDamage);
                }
                else
                {
                    hitDamage = spec.DamageMultiplier > 0
                        ? CombatFormulas.SpecialMaxHit(maxHit, spec.DamageMultiplier)
                        : CombatFormulas.Random(69) + 60; // Anger weapons

                    // Dragon dagger extra hit
                    if (weaponId == 5698)
                    {
                        int extraHit = CombatFormulas.Random(42);
                        target.AppendHit(extraHit, 0);
                        // Track special extra damage
                        if (extraHit > 0 && attacker.PlayerId >= 0 && attacker.PlayerId < target.KilledBy.Length)
                            target.KilledBy[attacker.PlayerId] += extraHit;
                    }

                    // Dragon halberd extra hit
                    if (weaponId == 3204)
                    {
                        int halberdHit = CombatFormulas.SpecialMaxHit(maxHit, spec.DamageMultiplier);
                        target.AppendHit(halberdHit, 0);
                        // Track special extra damage
                        if (halberdHit > 0 && attacker.PlayerId >= 0 && attacker.PlayerId < target.KilledBy.Length)
                            target.KilledBy[attacker.PlayerId] += halberdHit;
                    }
                }
            }
            else
            {
                attacker.UsingSpecial = false;
            }
        }
        else
        {
            // Normal melee attack
            attacker.RequestAnim(attacker.AttackEmote, 0);
        }

        // ── Vengeance recoil ───────────────────────────────────────────────
        ProcessVengeance(attacker, target, hitDamage);

        // ── Protection prayer check ────────────────────────────────────────
        hitDamage = ApplyPrayerProtection(target, hitDamage, CombatType.Melee);

        // ── Apply hit and set delays ───────────────────────────────────────
        target.AppendHit(hitDamage, 0);
        
        // Track damage for killer attribution
        if (hitDamage > 0 && attacker.PlayerId >= 0 && attacker.PlayerId < target.KilledBy.Length)
            target.KilledBy[attacker.PlayerId] += hitDamage;
        attacker.CombatDelay = attacker.AttackDelay;
        attacker.RequestFaceTo(target.PlayerId + 32768);
        target.RequestAnim(1659, 0); // defend anim

        // ── Award combat XP ────────────────────────────────────────────────
        AwardMeleeCombatXp(attacker, hitDamage);
        attacker.SpecialAmountUpdateReq = true;

        // ── Auto-retaliate ─────────────────────────────────────────────────
        TriggerAutoRetaliate(target, attacker);

        // ── Skull attacker if initiating ───────────────────────────────────
        if (attacker.SkulledDelay <= 0 && Player.IsWildernessArea(attacker.AbsX, attacker.AbsY))
            attacker.SkulledDelay = CombatConstants.SkullDuration;
    }

    /// <summary>
    /// Execute a ranged attack against another player.
    /// </summary>
    private void ProcessRangedAttack(Player attacker, Player target, int distance)
    {
        if (distance > CombatConstants.MaxRangeDistance)
        {
            ResetAttack(attacker);
            return;
        }

        // Check bounds for both Equipment and EquipmentN arrays
        if (attacker.Equipment.Length <= CombatConstants.SlotAmmo || 
            attacker.EquipmentN.Length <= CombatConstants.SlotAmmo)
        {
            ResetAttack(attacker);
            return;
        }

        int ammoId = attacker.Equipment[CombatConstants.SlotAmmo];
        int ammoCount = attacker.EquipmentN[CombatConstants.SlotAmmo];
        int weaponId = attacker.Equipment[CombatConstants.SlotWeapon];

        // Crystal bow (4214) — no ammo needed
        if (weaponId == 4214)
        {
            ProcessCrystalBowAttack(attacker, target);
            return;
        }

        if (!WeaponData.IsValidArrow(ammoId) || ammoCount <= 0)
        {
            // No arrows
            ResetAttack(attacker);
            return;
        }

        // ── Fire ranged attack ─────────────────────────────────────────────
        // Use ranged level for ranged damage, not strength (Java: p.skillLvl[4] / 4)
        int rangedLevel = attacker.SkillLvl[CombatConstants.SkillRanged];
        int maxHit = rangedLevel < 15 ? 1 : rangedLevel / 4;
        int hitDamage = CombatFormulas.Random(maxHit);

        attacker.RequestAnim(attacker.AttackEmote, 0);
        attacker.RequestGfx(WeaponData.GetArrowDrawGfx(ammoId), 100);

        // Consume ammo
        attacker.EquipmentN[CombatConstants.SlotAmmo]--;
        if (attacker.EquipmentN[CombatConstants.SlotAmmo] <= 0)
            attacker.Equipment[CombatConstants.SlotAmmo] = -1;

        // Prayer protection (range)
        hitDamage = ApplyPrayerProtection(target, hitDamage, CombatType.Ranged);

        target.AppendHit(hitDamage, 0);
        target.RequestAnim(424, 0);
        
        // Track damage for killer attribution
        if (hitDamage > 0 && attacker.PlayerId >= 0 && attacker.PlayerId < target.KilledBy.Length)
            target.KilledBy[attacker.PlayerId] += hitDamage;
        attacker.CombatDelay = attacker.AttackDelay;
        attacker.RequestFaceTo(target.PlayerId + 32768);

        // Award ranged XP
        attacker.AddSkillXP(4.0 * hitDamage * CombatConstants.CombatXpRate, CombatConstants.SkillRanged);
        attacker.AddSkillXP(2.0 * hitDamage * CombatConstants.CombatXpRate, CombatConstants.SkillHitpoints);
        attacker.SpecialAmountUpdateReq = true;

        TriggerAutoRetaliate(target, attacker);
    }

    /// <summary>
    /// Crystal bow (4214) special — no ammo, freeze target.
    /// </summary>
    private void ProcessCrystalBowAttack(Player attacker, Player target)
    {
        attacker.RequestAnim(attacker.AttackEmote, 0);
        attacker.RequestGfx(250, 100);
        attacker.CombatDelay = attacker.AttackDelay;
        attacker.RequestFaceTo(target.PlayerId + 32768);

        // Crystal bow uses Misc.random(30) in Java
        int maxHit = 30;
        int iceBowDamage = CombatFormulas.Random(maxHit);
        target.AppendHit(iceBowDamage, 0);
        target.RequestAnim(424, 0);
        
        // Track damage for killer attribution
        if (iceBowDamage > 0 && attacker.PlayerId >= 0 && attacker.PlayerId < target.KilledBy.Length)
            target.KilledBy[attacker.PlayerId] += iceBowDamage;
        target.FreezeDelay = 10;
        target.RequestGfx(8, 100);
    }

    /// <summary>
    /// Dragon claws multi-hit special attack.
    /// Ported from PlayerCombat — 4 hits with decreasing damage.
    /// </summary>
    private static void ProcessDragonClawsSpec(Player attacker, Player target, int hitDamage)
    {
        attacker.SecondHit = hitDamage / 2;
        attacker.ThirdHit = attacker.SecondHit / 2;
        attacker.FourthHit = attacker.ThirdHit > 0 ? attacker.ThirdHit - 1 : 0;
        attacker.secHit = attacker.SecondHit;
        attacker.fourHit = attacker.FourthHit;
        target.AppendHit(attacker.SecondHit, 0);
        target.AppendHit(attacker.ThirdHit, 0);
        
        // Track damage for killer attribution (claw special hits)
        if (attacker.PlayerId >= 0 && attacker.PlayerId < target.KilledBy.Length)
        {
            target.KilledBy[attacker.PlayerId] += attacker.SecondHit + attacker.ThirdHit;
        }
        attacker.ClawTimer = 1;
        attacker.UseClaws = true;
    }

    /// <summary>
    /// Process vengeance recoil if the target has it active.
    /// </summary>
    private static void ProcessVengeance(Player attacker, Player target, int hitDamage)
    {
        if (!target.VengOn || hitDamage <= 0)
            return;

        int vengDamage = (hitDamage / 4) * 3;
        attacker.AppendHit(vengDamage, 0);
        target.RequestForceChat("Taste Vengeance!");
        
        // Track vengeance damage for killer attribution (victim becomes attacker)
        if (vengDamage > 0 && target.PlayerId >= 0 && target.PlayerId < attacker.KilledBy.Length)
            attacker.KilledBy[target.PlayerId] += vengDamage;
        target.VengOn = false;
    }

    /// <summary>
    /// Apply protection prayer damage reduction.
    /// Ported from the Hitter counter logic in Java.
    /// </summary>
    private static int ApplyPrayerProtection(Player target, int hitDamage, CombatType combatType)
    {
        int requiredIcon = combatType switch
        {
            CombatType.Melee => 0,  // Protect from Melee icon
            CombatType.Ranged => 1, // Protect from Ranged icon
            CombatType.Magic => 2,  // Protect from Magic icon
            _ => -1,
        };

        if (target.PrayerIcon == requiredIcon)
        {
            if (target.Hitter > 0)
            {
                target.Hitter--;
                return 0;
            }

            target.Hitter = 2 + CombatFormulas.Random(4);
        }

        return hitDamage;
    }

    /// <summary>
    /// Award melee combat XP based on attack style.
    /// </summary>
    private static void AwardMeleeCombatXp(Player attacker, int hitDamage)
    {
        double xpBase = 4.0 * hitDamage * CombatConstants.CombatXpRate;
        double hpXp = 3.0 * hitDamage * CombatConstants.CombatXpRate;

        switch ((CombatStyle)attacker.AttackStyle)
        {
            case CombatStyle.Accurate:
                attacker.AddSkillXP(xpBase, CombatConstants.SkillAttack);
                break;
            case CombatStyle.Aggressive:
                attacker.AddSkillXP(xpBase, CombatConstants.SkillStrength);
                break;
            case CombatStyle.Defensive:
                attacker.AddSkillXP(xpBase, CombatConstants.SkillDefence);
                break;
            case CombatStyle.Controlled:
                double third = xpBase / 3.0;
                attacker.AddSkillXP(third, CombatConstants.SkillAttack);
                attacker.AddSkillXP(third, CombatConstants.SkillDefence);
                attacker.AddSkillXP(third, CombatConstants.SkillStrength);
                break;
        }
        attacker.AddSkillXP(hpXp, CombatConstants.SkillHitpoints);
    }

    /// <summary>
    /// Trigger auto-retaliate on the target if enabled.
    /// </summary>
    private static void TriggerAutoRetaliate(Player target, Player attacker)
    {
        if (target.AutoRetaliate == 0 && !target.AttackingPlayer)
        {
            target.RequestFaceTo(attacker.PlayerId + 32768);
            target.AttackPlayer = attacker.PlayerId;
            target.AttackingPlayer = true;
        }
    }

    /// <summary>
    /// Check area-specific PvP restrictions (duel partner, castle wars team, etc.).
    /// </summary>
    private bool CanAttackInArea(Player attacker, Player target)
    {
        // Bounty Hunter — must be assigned opponent
        if (IsBountyArea(attacker) && attacker.BountyOpponent != target.PlayerId)
            return false;

        // Duel Arena — must be assigned partner
        if (IsAtDuel(attacker) && attacker.DuelPartner != target.PlayerId)
            return false;

        // Castle Wars — can't attack teammates
        if (IsAtCastleWars(attacker) && attacker.CWTeam == target.CWTeam)
            return false;

        // Fight Pits — game must have started
        if (IsAtPits(attacker) && !attacker.GameStarted)
            return false;

        // Duel not yet started
        if (IsAtDuel(attacker) && !attacker.DuelCan)
            return false;

        return true;
    }

    // ── Area detection helpers (match Java Player methods) ──────────────────

    private static bool IsBountyArea(Player p)
        => p.AbsX >= 3085 && p.AbsX <= 3185 && p.AbsY >= 3662 && p.AbsY <= 3765;

    private static bool IsAtDuel(Player p)
        => p.AbsX >= 3362 && p.AbsX <= 3391 && p.AbsY >= 3228 && p.AbsY <= 3241;

    private static bool IsAtCastleWars(Player p)
        => p.AbsX >= 2368 && p.AbsX <= 2428 && p.AbsY >= 3072 && p.AbsY <= 3132;

    private static bool IsAtPits(Player p)
        => p.AbsX >= 2370 && p.AbsX <= 2426 && p.AbsY >= 5128 && p.AbsY <= 5167;

    private static bool IsInArena(Player p)
        => IsAtDuel(p) || IsAtPits(p) || IsAtCastleWars(p);

    /// <summary>
    /// Get the strength prayer multiplier based on active prayers.
    /// </summary>
    private static double GetStrengthPrayerMultiplier(Player player)
    {
        // Prayer indices from Java:
        // 1 = Burst of Strength (5%)
        // 6 = Superhuman Strength (10%) 
        // 14 = Ultimate Strength (15%)
        // 25 = Chivalry (18%)
        // 26 = Piety (23%)
        
        if (player.PrayOn[26]) return 1.23; // Piety
        if (player.PrayOn[25]) return 1.18; // Chivalry
        if (player.PrayOn[14]) return 1.15; // Ultimate Strength
        if (player.PrayOn[6]) return 1.10; // Superhuman Strength
        if (player.PrayOn[1]) return 1.05; // Burst of Strength
        
        return 1.0; // No strength prayer
    }

    /// <summary>
    /// Reset the player's PvP combat state.
    /// </summary>
    public static void ResetAttack(Player p)
    {
        if (p == null) return;
        p.AttackingPlayer = false;
        if (p.FaceToReq != 65535)
            p.RequestFaceTo(65535);
    }
}
