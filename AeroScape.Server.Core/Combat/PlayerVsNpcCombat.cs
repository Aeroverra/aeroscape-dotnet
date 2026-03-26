using System;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Engine;
using AeroScape.Server.Core.Items;
using Microsoft.Extensions.Logging;

namespace AeroScape.Server.Core.Combat;

/// <summary>
/// Handles Player vs NPC combat processing (PvE).
/// Called once per tick for each player with AttackingNPC == true.
/// Ported from PlayerNPCCombat.attackNPC(Player p) in Java.
/// </summary>
public class PlayerVsNpcCombat
{
    private readonly GameEngine _engine;
    private readonly ILogger<PlayerVsNpcCombat> _logger;
    private readonly PlayerItemsService _playerItems;

    public PlayerVsNpcCombat(GameEngine engine, ILogger<PlayerVsNpcCombat> logger, PlayerItemsService playerItems)
    {
        _engine = engine;
        _logger = logger;
        _playerItems = playerItems;
    }

    /// <summary>
    /// Process one tick of PvE combat for the given attacker.
    /// </summary>
    public void ProcessAttack(Player attacker)
    {
        if (attacker.AttackNPC <= 0 || attacker.AttackNPC >= GameEngine.MaxNpcs)
        {
            ResetAttack(attacker);
            return;
        }

        var npc = _engine.Npcs[attacker.AttackNPC];

        if (npc == null || attacker.IsDead)
        {
            ResetAttack(attacker);
            return;
        }

        // ── Click delay gate (matches Java) ────────────────────────────────
        if (attacker.ClickDelay > 0)
            return;

        // ── NPC just died — handle kill tracking ───────────────────────────
        if (npc.IsDead)
        {
            ProcessNpcKill(attacker, npc);
            attacker.CombatDelay = 3;
            attacker.ClickDelay = 5;
            return;
        }

        // ── Combat delay gate ──────────────────────────────────────────────
        if (attacker.CombatDelay > 0)
            return;

        // ── Determine combat type and execute ──────────────────────────────
        int weaponId = attacker.Equipment[CombatConstants.SlotWeapon];
        int distance = CombatFormulas.GetDistance(npc.AbsX, npc.AbsY, attacker.AbsX, attacker.AbsY);

        // Autocast magic takes priority
        if (attacker.AutoCasting && !MagicNpcService.HasAutocastStaff(attacker))
        {
            attacker.AutoCasting = false;
            attacker.AutoCastSpellId = -1;
        }

        if (attacker.AutoCastSpellId > 0)
        {
            ProcessMagicAttack(attacker, npc, attacker.AutoCastSpellId);
        }
        else if (WeaponData.IsBow(weaponId))
        {
            ProcessRangedAttack(attacker, npc, distance);
        }
        else if (distance <= 1)
        {
            ProcessMeleeAttack(attacker, npc);
        }
        // else: not in range, keep following
    }

    /// <summary>
    /// Execute a melee attack against an NPC.
    /// </summary>
    private void ProcessMeleeAttack(Player attacker, NPC npc)
    {
        int maxHit = CombatFormulas.MaxMeleeHit(
            attacker.SkillLvl[CombatConstants.SkillStrength],
            attacker.EquipmentBonus[CombatConstants.BonusStrength]);
        int hitDamage = CombatFormulas.Random(maxHit);

        int weaponId = attacker.Equipment[CombatConstants.SlotWeapon];

        // ── Special attack handling ────────────────────────────────────────
        if (attacker.UsingSpecial && WeaponData.PlayerVsNpcSpecialAttacks.TryGetValue(weaponId, out var spec))
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
                    npc.RequestGfx(spec.TargetGfx, 100);

                if (spec.IsMultiHit)
                {
                    hitDamage = CombatFormulas.SpecialMaxHit(maxHit, spec.DamageMultiplier);
                    attacker.secHit2 = hitDamage / 2;
                    attacker.thirdHit2 = attacker.secHit2 / 2;
                    attacker.fourHit2 = attacker.thirdHit2 > 0 ? attacker.thirdHit2 - 1 : 0;
                    npc.AppendHit(attacker.secHit2, 0);
                    attacker.clawTimer2 = 1;
                    attacker.UseClaws2 = true;
                }
                else
                {
                    hitDamage = spec.DamageMultiplier > 0
                        ? CombatFormulas.SpecialMaxHit(maxHit, spec.DamageMultiplier)
                        : CombatFormulas.Random(69) + 60;

                    // Dragon dagger extra hit
                    if (weaponId == 5698)
                        npc.AppendHit(CombatFormulas.SpecialMaxHit(maxHit, 1.0), 0);

                    // Dragon halberd extra hit
                    if (weaponId == 3204)
                        npc.AppendHit(CombatFormulas.SpecialMaxHit(maxHit, spec.DamageMultiplier), 0);
                }
            }
            else
            {
                attacker.UsingSpecial = false;
            }
        }
        else if (attacker.UsingSpecial)
        {
            attacker.UsingSpecial = false;
            attacker.RequestAnim(attacker.AttackEmote, 0);
        }
        else
        {
            // Normal melee attack
            attacker.RequestAnim(attacker.AttackEmote, 0);
        }

        // ── Apply hit and set delays ───────────────────────────────────────
        npc.AppendHit(hitDamage, 0);
        attacker.CombatDelay = attacker.AttackDelay;
        attacker.RequestFaceTo(npc.NpcId);
        npc.RequestAnim(npc.DefendEmote, 0);

        // ── Award melee XP ─────────────────────────────────────────────────
        AwardMeleeCombatXp(attacker, hitDamage);
        attacker.SpecialAmountUpdateReq = true;

        // ── NPC retaliates ─────────────────────────────────────────────────
        if (!npc.AttackingPlayer)
        {
            npc.AttackingPlayer = true;
            npc.AttackPlayer = attacker.PlayerId;
        }
    }

    /// <summary>
    /// Execute a ranged attack against an NPC.
    /// </summary>
    private void ProcessRangedAttack(Player attacker, NPC npc, int distance)
    {
        if (distance > CombatConstants.MaxRangeDistance)
        {
            ResetAttack(attacker);
            return;
        }

        int ammoId = attacker.Equipment[CombatConstants.SlotAmmo];
        int ammoCount = attacker.EquipmentN[CombatConstants.SlotAmmo];
        int weaponId = attacker.Equipment[CombatConstants.SlotWeapon];

        int meleeMaxHit = CombatFormulas.MaxMeleeHit(
            attacker.SkillLvl[CombatConstants.SkillStrength],
            attacker.EquipmentBonus[CombatConstants.BonusStrength]);
        int meleeSeedHit = CombatFormulas.Random(meleeMaxHit);

        // Crystal bow — no ammo
        if (weaponId == 4214)
        {
            attacker.RequestAnim(attacker.AttackEmote, 0);
            attacker.RequestGfx(250, 100);
            attacker.CombatDelay = attacker.AttackDelay;
            attacker.RequestFaceTo(npc.NpcId);

            npc.AppendHit(meleeSeedHit, 0);
            npc.RequestAnim(npc.DefendEmote, 0);
            RetaliateNpc(npc, attacker);
            return;
        }

        if (!WeaponData.IsValidArrow(ammoId) || ammoCount <= 0)
        {
            ResetAttack(attacker);
            return;
        }

        // Fire ranged attack
        attacker.RequestAnim(attacker.AttackEmote, 0);
        attacker.RequestGfx(WeaponData.GetArrowDrawGfx(ammoId), 100);

        // Consume ammo
        attacker.EquipmentN[CombatConstants.SlotAmmo]--;
        if (attacker.EquipmentN[CombatConstants.SlotAmmo] <= 0)
            attacker.Equipment[CombatConstants.SlotAmmo] = -1;

        attacker.CombatDelay = attacker.AttackDelay;
        attacker.RequestFaceTo(npc.NpcId);

        int rangeLevel = attacker.SkillLvl[CombatConstants.SkillRanged];
        int xpSeedHit = rangeLevel < 15 ? 1 : rangeLevel / 4;
        int hitDamage = CombatFormulas.Random(xpSeedHit);
        npc.AppendHit(hitDamage, 0);
        npc.RequestAnim(424, 0);

        // Award ranged XP
        attacker.AddSkillXP(4.0 * xpSeedHit * CombatConstants.CombatXpRate, CombatConstants.SkillRanged);
        attacker.AddSkillXP(2.0 * xpSeedHit * CombatConstants.CombatXpRate, CombatConstants.SkillHitpoints);

        RetaliateNpc(npc, attacker);
    }

    /// <summary>
    /// Execute a magic attack against an NPC using a spell ID.
    /// </summary>
    private void ProcessMagicAttack(Player attacker, NPC npc, int spellId)
    {
        if (spellId < 1 || spellId > 16)
        {
            attacker.AutoCasting = false;
            return;
        }

        var spell = MagicSpellData.Spells[spellId];

        // Level check
        if (attacker.GetLevelForXP(CombatConstants.SkillMagic) < spell.LevelRequired)
        {
            attacker.AutoCasting = false;
            return;
        }

        if (!MagicNpcService.TryConsumeRunes(attacker, spell))
        {
            attacker.AutoCasting = false;
            attacker.AutoCastSpellId = -1;
            return;
        }

        // Apply damage
        int damage = CombatFormulas.MagicDamage(spellId,
            attacker.SkillLvl[CombatConstants.SkillMagic],
            attacker.EquipmentBonus[CombatConstants.BonusMagicAttack]);

        attacker.RequestAnim(711, 0);
        attacker.RequestGfx(spell.CasterGfx, 100);
        npc.RequestGfx(spell.VictimGfx, 177);

        npc.AppendHit(damage, 0);
        attacker.AttackingNPC = true;
        attacker.AttackNPC = npc.NpcId;
        attacker.CombatDelay = CombatConstants.MagicCastDelay;
        attacker.MagicDelay = CombatConstants.MagicCastDelay;
        attacker.MagicCanCast = false;
        attacker.RequestFaceTo(npc.NpcId);

        // Award magic XP
        double xp = spell.GetXpForHit(damage);
        attacker.AddSkillXP(xp, CombatConstants.SkillMagic);

        if (!attacker.AutoCasting)
            attacker.AutoCastSpellId = -1;

        RetaliateNpc(npc, attacker);
    }

    /// <summary>
    /// Handle NPC kill tracking — barrows brothers, GWD kill counts, slayer.
    /// </summary>
    private static void ProcessNpcKill(Player attacker, NPC npc)
    {
        int npcType = npc.NpcType;

        // ── Barrows brothers ───────────────────────────────────────────────
        int barrowsIndex = npcType switch
        {
            2025 => 0, 2026 => 1, 2027 => 2,
            2028 => 3, 2029 => 4, 2030 => 5,
            _ => -1,
        };
        if (barrowsIndex >= 0 && barrowsIndex < attacker.Barrows.Length)
            attacker.Barrows[barrowsIndex] = true;

        // ── GWD kill counts ────────────────────────────────────────────────
        // Zamorak
        if (npcType is 1619 or 49 or 6219)
            attacker.ZilyanakillCount++;
        // Saradomin
        if (npcType is 6255 or 6254)
            attacker.SaradominKillCount++;
        // Bandos
        if (npcType is 6275 or 6277 or 6270)
            attacker.BandosKillCount++;
        // Armadyl
        if (npcType is 6232 or 6229)
            attacker.ArmadylKillCount++;

        // ── Slayer task tracking ───────────────────────────────────────────
        if (attacker.SlayerAmount > 0)
        {
            bool matchesTask = (attacker.SlayerTask, npcType) switch
            {
                (0, 941 or 55 or 53 or 5363) => true, // Dragons
                (1, 9) => true,                         // Guards
                (2, 110 or 111 or 112) => true,         // Giants
                (3, 4387 or 6998) => true,              // Ghosts
                (4, 21) => true,                         // Heroes
                _ => false,
            };

            if (matchesTask)
            {
                attacker.SlayerAmount--;
                int slayerXp = npcType switch
                {
                    941 or 55 or 53 => 150,
                    5363 => 500,
                    4387 or 6998 => 400,
                    _ => 250,
                };
                attacker.AddSkillXP(slayerXp * attacker.SkillLvl[CombatConstants.SkillSlayer],
                    CombatConstants.SkillSlayer);
            }
        }

        // ── Dragon Slayer quest ────────────────────────────────────────────
        if (npcType == 742 && attacker.DragonSlayer == 3)
        {
            attacker.HeadTimer = 8;
            attacker.DragonSlayer = 4;
            _playerItems.AddItem(attacker, 11279, 1);
            attacker.LastTickMessage = "You slayed Elvarg and took his head!";
        }
    }

    /// <summary>
    /// Make the NPC retaliate against the attacker.
    /// </summary>
    private static void RetaliateNpc(NPC npc, Player attacker)
    {
        if (!npc.AttackingPlayer)
        {
            npc.AttackingPlayer = true;
            npc.AttackPlayer = attacker.PlayerId;
        }
    }

    /// <summary>
    /// Award melee combat XP based on attack style.
    /// </summary>
    private static void AwardMeleeCombatXp(Player attacker, int hitDamage)
    {
        double xpBase = 4.0 * hitDamage * CombatConstants.CombatXpRate;

        switch ((CombatStyle)attacker.AttackStyle)
        {
            case CombatStyle.Accurate:
                attacker.AddSkillXP(xpBase, CombatConstants.SkillAttack);
                attacker.AddSkillXP(2.0 * hitDamage * CombatConstants.CombatXpRate, CombatConstants.SkillHitpoints);
                break;
            case CombatStyle.Aggressive:
                attacker.AddSkillXP(xpBase, CombatConstants.SkillStrength);
                attacker.AddSkillXP(2.0 * hitDamage * CombatConstants.CombatXpRate, CombatConstants.SkillHitpoints);
                break;
            case CombatStyle.Defensive:
                attacker.AddSkillXP(xpBase, CombatConstants.SkillDefence);
                attacker.AddSkillXP(2.0 * hitDamage * CombatConstants.CombatXpRate, CombatConstants.SkillHitpoints);
                break;
            case CombatStyle.Controlled:
                double third = xpBase / 3.0;
                attacker.AddSkillXP(third, CombatConstants.SkillAttack);
                attacker.AddSkillXP(third, CombatConstants.SkillDefence);
                attacker.AddSkillXP(third, CombatConstants.SkillStrength);
                attacker.AddSkillXP(3.0 * hitDamage * CombatConstants.CombatXpRate, CombatConstants.SkillHitpoints);
                break;
        }
    }

    /// <summary>
    /// Reset the player's PvE combat state.
    /// </summary>
    public static void ResetAttack(Player p)
    {
        if (p == null) return;
        p.AttackingNPC = false;
        if (p.FaceToReq != 65535)
            p.RequestFaceTo(65535);
    }
}
