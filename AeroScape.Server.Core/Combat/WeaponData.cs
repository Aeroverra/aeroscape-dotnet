using System;
using System.Collections.Generic;
using System.Collections.Frozen;

namespace AeroScape.Server.Core.Combat;

/// <summary>
/// Static weapon classification data ported from the Java combat classes.
/// Identifies bows, arrow types, projectile graphics, and special attack definitions.
/// </summary>
public static class WeaponData
{
    // ── Bow item IDs ───────────────────────────────────────────────────────
    private static readonly FrozenSet<int> _bowIds = new HashSet<int>
    {
        4212, 4214, 4215, 4216, 4217, 4218, 4219, 4220, 4221, 4222, 4223,
        837, 767, 4734, 839, 841, 843, 845, 847, 849, 851, 853, 855, 857,
        859, 861, 2883, 4827, 6724, 11235,
    }.ToFrozenSet();

    /// <summary>Check if the given item ID is a bow/crossbow.</summary>
    public static bool IsBow(int itemId) => _bowIds.Contains(itemId);

    // ── Arrow item IDs → projectile GFX ────────────────────────────────────
    private static readonly Dictionary<int, ArrowInfo> _arrows = new()
    {
        // itemId → (airGfx, backGfx)
        [882] = new ArrowInfo(10, 19),  // Bronze
        [884] = new ArrowInfo(11, 18),  // Iron
        [886] = new ArrowInfo(12, 20),  // Steel
        [888] = new ArrowInfo(13, 21),  // Mithril
        [890] = new ArrowInfo(14, 22),  // Adamant
        [892] = new ArrowInfo(15, 24),  // Rune
        [78]  = new ArrowInfo(10, 19),  // Bolt rack (uses bronze GFX as fallback)
        [4740] = new ArrowInfo(10, 19), // Bolt rack alt
    };

    /// <summary>Check if the given ammo slot item is a valid arrow.</summary>
    public static bool IsValidArrow(int itemId) => _arrows.ContainsKey(itemId);

    /// <summary>Get the projectile GFX ID for an arrow in flight.</summary>
    public static int GetArrowFlightGfx(int itemId) =>
        _arrows.TryGetValue(itemId, out var info) ? info.AirGfx : 500;

    /// <summary>Get the draw-back GFX ID for an arrow.</summary>
    public static int GetArrowDrawGfx(int itemId) =>
        _arrows.TryGetValue(itemId, out var info) ? info.BackGfx : 500;

    public readonly record struct ArrowInfo(int AirGfx, int BackGfx);

    // ── Special attack definitions ─────────────────────────────────────────

    /// <summary>
    /// Lookup table for melee special attacks.
    /// Key = weapon item ID, Value = special attack definition.
    /// Ported from PlayerCombat.attackPlayer and PlayerNPCCombat.attackNPC.
    /// </summary>
    public static readonly FrozenDictionary<int, SpecialAttack> PlayerVsPlayerSpecialAttacks =
        new Dictionary<int, SpecialAttack>
        {
            // Armadyl Godsword
            [11694] = new(50, 1.6, 7074, 1222, 0, false),
            // Bandos Godsword
            [11696] = new(100, 1.2, 7073, 1223, 0, false),
            // Saradomin Godsword
            [11698] = new(75, 1.3, 7071, 1220, 0, false),
            // Zamorak Godsword
            [11700] = new(75, 1.3, 7070, 1221, 0, false),
            // Abyssal Whip
            [4151] = new(50, 1.3, 1658, -1, 341, false),
            // Dragon Claws (multi-hit, handled specially)
            [3101] = new(50, 1.1, 2068, 274, -1, true),
            // Dragon Longsword
            [1305] = new(25, 1.0, 1058, 248, -1, false),
            // Dragon Scimitar
            [4587] = new(70, 1.3, 2081, 347, -1, false),
            // Dragon Mace
            [1434] = new(40, 1.1, 1060, 251, -1, false),
            // Dragon Halberd (double hit)
            [3204] = new(100, 1.1, 1665, 282, -1, false),
            // Dragon Dagger
            [5698] = new(25, 1.0, 1062, 252, -1, false),
            // Saradomin Sword
            [11730] = new(50, 1.1, 7072, 1224, -1, false),
            // Dragon Battleaxe (strength boost, not a damage spec)
            [1377] = new(100, 1.0, 1978, 1222, -1, false),
            // Anger weapons
            [7806] = new(50, 0, 19784, 1222, -1, false),
            [7807] = new(100, 0, 1978, 1222, -1, false),
            [7808] = new(75, 0, 1978, 1222, -1, false),
            [7809] = new(50, 0, 1978, 1222, -1, false),
        }.ToFrozenDictionary();

    /// <summary>
    /// NPC combat special attacks intentionally differ from PvP in the legacy Java server.
    /// This table matches PlayerNPCCombat.java exactly and omits specials that were not allowed.
    /// </summary>
    public static readonly FrozenDictionary<int, SpecialAttack> PlayerVsNpcSpecialAttacks =
        new Dictionary<int, SpecialAttack>
        {
            [11694] = new(50, 1.25, 7074, 1222, 0, false),
            [11696] = new(100, 1.1, 7073, 1223, 0, false),
            [11698] = new(75, 1.1, 7071, 1220, 0, false),
            [11700] = new(75, 1.1, 7070, 1221, 0, false),
            [4151] = new(50, 0.9, 1658, -1, 341, false),
            [3101] = new(50, 1.1, 2068, 274, -1, true),
            [1305] = new(25, 1.0, 1058, 248, -1, false),
            [4587] = new(70, 1.0, 2081, 347, -1, false),
            [1434] = new(40, 1.1, 1060, 251, -1, false),
            [3204] = new(100, 1.1, 1665, 282, -1, false),
            [5698] = new(25, 1.0, 1062, 252, -1, false),
        }.ToFrozenDictionary();
}

/// <summary>
/// Definition of a weapon's special attack.
/// </summary>
/// <param name="EnergyCost">Special energy required (0-100).</param>
/// <param name="DamageMultiplier">Multiplier applied to max hit (0 = fixed damage weapon like anger).</param>
/// <param name="AnimId">Animation to play on the attacker.</param>
/// <param name="AttackerGfx">GFX on the attacker (-1 = none).</param>
/// <param name="TargetGfx">GFX on the target (-1 = none).</param>
/// <param name="IsMultiHit">True if this is a multi-hit special (e.g. Dragon Claws).</param>
public readonly record struct SpecialAttack(
    int EnergyCost,
    double DamageMultiplier,
    int AnimId,
    int AttackerGfx,
    int TargetGfx,
    bool IsMultiHit
);
