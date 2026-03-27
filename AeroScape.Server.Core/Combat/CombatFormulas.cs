using System;
using System.Threading;

namespace AeroScape.Server.Core.Combat;

/// <summary>
/// Pure-function combat formulas ported from the Java PlayerCombat / PlayerNPCCombat / MagicNPC classes.
/// No side-effects — these only compute numbers.
/// </summary>
public static class CombatFormulas
{
    // Thread-safe random number generator using ThreadLocal
    private static readonly ThreadLocal<Random> _rng = new(() => new Random());

    /// <summary>
    /// Roll a random integer from 0..range (inclusive on both ends).
    /// Mirrors Misc.random(int) from Java.
    /// </summary>
    public static int Random(int range)
    {
        if (range <= 0) return 0;
        // Prevent integer overflow when range is int.MaxValue
        if (range == int.MaxValue) return _rng.Value.Next(range);
        return _rng.Value.Next(range + 1);
    }

    /// <summary>
    /// Euclidean tile distance between two coordinates.
    /// Mirrors Misc.getDistance.
    /// </summary>
    public static int GetDistance(int x1, int y1, int x2, int y2)
    {
        int dx = x2 - x1;
        int dy = y2 - y1;
        return (int)Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Maximum melee hit based on strength level and equipment strength bonus.
    /// Formula: floor(str * (bonus * 0.00175 + 0.1) + 2.05) * prayerMultiplier
    /// Ported from PlayerCombat.maxMeleeHit / PlayerNPCCombat.maxMeleeHit.
    /// </summary>
    public static int MaxMeleeHit(int strengthLevel, int strengthBonus, double prayerMultiplier = 1.0)
    {
        double a = strengthLevel;
        double b = strengthBonus;
        double f = b * 0.00175 + 0.1;
        double h = Math.Floor(a * f + 2.05);
        return (int)(h * prayerMultiplier);
    }

    /// <summary>
    /// Maximum ranged hit based on range level and range equipment bonus.
    /// Same formula as melee but using range stats.
    /// Ported from PlayerNPCCombat.maxRangeHit.
    /// </summary>
    public static int MaxRangeHit(int rangeLevel, int rangeBonus)
    {
        double a = rangeLevel;
        double b = rangeBonus;
        double f = b * 0.00175 + 0.1;
        double h = Math.Floor(a * f + 2.05);
        return (int)h;
    }

    /// <summary>
    /// Get the max hit for a standard magic spell by its spell list id (1-16).
    /// Ported from MagicNPC.getMaxHit.
    /// </summary>
    public static int MagicMaxHit(int spellId)
    {
        int maxHit = 0;
        for (int i = 1; i <= spellId; i++)
        {
            if (i <= 4)
                maxHit += 2;
            else
                maxHit += 1;
        }
        return maxHit;
    }

    /// <summary>
    /// Bonus magic damage from equipment.
    /// Ported from MagicNPC.getBonusDamage.
    /// </summary>
    public static int MagicBonusDamage(int magicLevel, int magicBonus)
    {
        double c = magicLevel;
        double d = magicBonus;
        double f = d * 0.00175 + 0.1;
        double h = Math.Floor(c * f + 1.06) / 4;
        return (int)h;
    }

    /// <summary>
    /// Total magic damage for a spell hit.
    /// Ported from MagicNPC.getDamage.
    /// </summary>
    public static int MagicDamage(int spellId, int magicLevel, int magicBonus)
    {
        int maxHit = MagicMaxHit(spellId) + MagicBonusDamage(magicLevel, magicBonus);
        return Random(maxHit);
    }

    /// <summary>
    /// Wilderness level at a given Y coordinate.
    /// </summary>
    public static int WildernessLevel(int absY)
        => (absY - 3520) + 1;

    /// <summary>
    /// Check if two players are within each other's wilderness combat range.
    /// Ported from PlayerCombat.isInWildRange.
    /// </summary>
    public static bool IsInWildRange(int combatLevel1, int absY1, int combatLevel2, int absY2)
    {
        int wildLvl = WildernessLevel(absY2);
        if (wildLvl < 1) wildLvl = 1;

        int diff = Math.Abs(combatLevel1 - combatLevel2);
        return diff <= wildLvl;
    }

    /// <summary>
    /// Apply special attack multiplier to a max hit.
    /// </summary>
    public static int SpecialMaxHit(int baseMaxHit, double multiplier)
        => Random((int)(baseMaxHit * multiplier));
}
