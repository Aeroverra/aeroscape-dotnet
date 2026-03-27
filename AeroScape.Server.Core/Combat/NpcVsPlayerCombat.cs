using System;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Engine;
using Microsoft.Extensions.Logging;

namespace AeroScape.Server.Core.Combat;

/// <summary>
/// Handles NPC vs Player combat processing.
/// Called once per tick for each NPC with AttackingPlayer == true.
/// Ported from NPCPlayerCombat.attackPlayer(NPC n) in Java.
/// </summary>
public class NpcVsPlayerCombat
{
    private readonly GameEngine _engine;
    private readonly ILogger<NpcVsPlayerCombat> _logger;

    public NpcVsPlayerCombat(GameEngine engine, ILogger<NpcVsPlayerCombat> logger)
    {
        _engine = engine;
        _logger = logger;
    }

    /// <summary>
    /// Process one tick of NPC-vs-Player combat.
    /// </summary>
    public void ProcessAttack(NPC npc)
    {
        if (npc.AttackPlayer <= 0 || npc.AttackPlayer >= GameEngine.MaxPlayers)
        {
            ResetAttack(npc);
            return;
        }

        // Thread-safe access to prevent race conditions
        Player target;
        try
        {
            target = _engine.Players[npc.AttackPlayer];
        }
        catch (IndexOutOfRangeException)
        {
            ResetAttack(npc);
            return;
        }

        if (target == null || target.IsDead || npc.IsDead || target.Disconnected[1])
        {
            ResetAttack(npc);
            return;
        }

        // ── Distance / proximity check ─────────────────────────────────────
        int offsetX = (npc.AbsX - target.AbsX) * -1;
        int offsetY = (npc.AbsY - target.AbsY) * -1;

        // Java uses -3 to 3 range: !(offsetX > -3 && offsetX < 3) || !(offsetY > -3 && offsetY < 3)
        if (offsetX < -3 || offsetX > 3 || offsetY < -3 || offsetY > 3)
        {
            ResetAttack(npc);
            return;
        }

        // ── Combat delay gate ──────────────────────────────────────────────
        if (npc.CombatDelay > 0 || npc.AttackPlayer <= 0)
            return;

        int distance = CombatFormulas.GetDistance(target.AbsX, target.AbsY, npc.AbsX, npc.AbsY);
        if (distance > 1)
            return; // Need to be adjacent (following logic will bring NPC closer)

        // ── Face the target ────────────────────────────────────────────────
        npc.RequestFaceTo(target.PlayerId + 32768);

        // ── Calculate hit ──────────────────────────────────────────────────
        int maxHit = CombatFormulas.Random(npc.MaxHit);

        // ── NPC-type-specific attack animations ────────────────────────────
        int npcType = npc.NpcType;
        int attackAnim = GetNpcAttackAnim(npc);

        // ── Dragon fire mechanic ───────────────────────────────────────────
        bool isDragon = IsDragonType(npcType);
        if (isDragon && CombatFormulas.Random(1) == 1)
        {
            // Dragon breath attack
            npc.RequestGfx(1, 0);
            npc.RequestAnim(81, 0);

            int shieldId = target.Equipment[CombatConstants.SlotShield];
            if (shieldId == 1540 || shieldId == 11283)
            {
                // Anti-dragon shield or DFS absorbs most damage
                maxHit = CombatFormulas.Random(5);
            }
            else
            {
                maxHit = 10 + CombatFormulas.Random(20);
            }
        }
        else
        {
            npc.RequestAnim(attackAnim, 0);
        }

        // ── Protection prayer check ────────────────────────────────────────
        maxHit = ApplyMeleeProtection(target, maxHit);

        // ── Apply hit ──────────────────────────────────────────────────────
        target.AppendHit(maxHit, 0);
        target.RequestAnim(424, 0); // defend/flinch animation
        npc.CombatDelay = npc.AttackDelay;

        // ── Auto-retaliate ─────────────────────────────────────────────────
        if (target.AutoRetaliate == 0 && !target.AttackingNPC)
        {
            target.CombatDelay += 3;
            target.RequestFaceTo(npc.NpcId);
            target.AttackNPC = npc.NpcId;
            target.AttackingNPC = true;
        }
    }

    /// <summary>
    /// Get the appropriate attack animation for an NPC type.
    /// Ported from NPCPlayerCombat switch cases.
    /// </summary>
    private static int GetNpcAttackAnim(NPC npc)
    {
        return npc.NpcType switch
        {
            9 or 21 or 20 => 451,   // Guard / hero
            2 or 1 => 422,           // Man / woman
            _ => npc.AttackEmote > 0 ? npc.AttackEmote : 422,
        };
    }

    /// <summary>
    /// Check if an NPC type is a dragon (uses fire breath).
    /// </summary>
    private static bool IsDragonType(int npcType)
        => npcType is 742 or 5363 or 55 or 53 or 941;

    /// <summary>
    /// Apply Protect from Melee prayer reduction.
    /// Uses the same Hitter counter logic as the Java source.
    /// </summary>
    private static int ApplyMeleeProtection(Player target, int maxHit)
    {
        if (target.PrayerIcon == 0) // Protect from Melee
        {
            if (target.Hitter > 0)
            {
                target.Hitter--;
                return 0;
            }

            target.Hitter = 2 + CombatFormulas.Random(4);
        }
        return maxHit;
    }

    /// <summary>
    /// Reset the NPC's combat state.
    /// </summary>
    public static void ResetAttack(NPC npc)
    {
        if (npc == null) return;
        npc.AttackingPlayer = false;
        npc.AttackPlayer = 0;
    }
}
