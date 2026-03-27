using AeroScape.Server.Core.Entities;

namespace AeroScape.Server.Core.Skills;

/// <summary>
/// Mining skill — ported from DavidScape/Skills/Mining.java.
/// 
/// Data-driven: rock definitions and pickaxe definitions are static arrays.
/// Follows the same tick-based pattern as Woodcutting via GatheringSkillBase.
/// </summary>
public class MiningSkill : GatheringSkillBase
{
    // ── Rock definitions ────────────────────────────────────────────────────

    /// <param name="ObjectIds">Game object IDs that map to this rock type.</param>
    /// <param name="OreItemId">Item ID of the ore produced.</param>
    /// <param name="LevelRequired">Minimum Mining level.</param>
    /// <param name="BaseXp">Base XP per ore (scaled by level).</param>
    /// <param name="Name">Display name for messages.</param>
    public record RockDefinition(
        int[] ObjectIds,
        int OreItemId,
        int LevelRequired,
        int BaseXp,
        string Name);

    public static readonly RockDefinition[] Rocks =
    [
        new([2110, 2090, 11189, 11190, 11191, 2091],  436,  1,  50,  "Copper rock"),
        new([2094, 11186, 11187, 11188, 2095],         438,  1,  50,  "Tin rock"),
        new([2092, 2093],                               440,  15, 75,  "Iron rock"),
        new([2100, 2101],                               442,  20, 100, "Silver rock"),
        new([2096, 2097],                               453,  30, 150, "Coal rock"),
        new([11183, 11184, 11185, 2098, 2099],         444,  40, 240, "Gold rock"),
        new([2102, 2103],                               447,  55, 300, "Mithril rock"),
        new([2104, 2105],                               449,  70, 400, "Adamantite rock"),
        new([2106, 2107],                               451,  80, 600, "Runite rock"),
        new([4028, 4029, 4030],                         3211, 1,  5,   "Limestone rock"),
        new([6669, 6670, 6671],                         2892, 10, 20,  "Elemental rock"),
        new([16687],                                    1436, 1,  75,  "Rune essence"),
    ];

    // ── Pickaxe definitions ─────────────────────────────────────────────────
    // Ordered best → worst for detection priority.

    /// <param name="ItemId">Item ID of the pickaxe.</param>
    /// <param name="LevelRequired">Minimum Mining level to use.</param>
    /// <param name="Animation">Animation ID when using.</param>
    /// <param name="Name">Display name.</param>
    public record PickaxeDefinition(int ItemId, int LevelRequired, int Animation, string Name);

    public static readonly PickaxeDefinition[] Pickaxes =
    [
        new(1275, 41, 624, "Rune pickaxe"),
        new(1273, 31, 628, "Adamant pickaxe"),
        new(1271, 21, 629, "Mithril pickaxe"),
        new(1269, 5,  627, "Steel pickaxe"),
        new(1267, 1,  626, "Iron pickaxe"),
        new(1265, 1,  625, "Bronze pickaxe"),
    ];

    // ── Instance state ──────────────────────────────────────────────────────
    private RockDefinition? _currentRock;
    private PickaxeDefinition? _currentPickaxe;
    private int _ticksPerOre = 4;

    public MiningSkill(Player player) : base(player) { }

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Begin mining a rock. Called from ObjectOption1 handler.
    /// </summary>
    public void StartMining(int objectId)
    {
        // 1. Find which rock was clicked
        _currentRock = FindRock(objectId);
        if (_currentRock == null)
            return;

        // 2. Find the best pickaxe
        _currentPickaxe = FindBestPickaxe();
        if (_currentPickaxe == null)
        {
            // TODO: p.frames.sendMessage(p, "You don't have a pickaxe which you have the Mining level to use.");
            return;
        }

        // 3. Check level
        int miningLevel = _player.SkillLvl[SkillConstants.Mining];
        if (miningLevel < _currentRock.LevelRequired)
        {
            // TODO: p.frames.sendMessage(p, $"You need a mining level of {_currentRock.LevelRequired} to mine this ore.");
            return;
        }

        // 4. Start
        // TODO: p.frames.sendMessage(p, "You swing your pickaxe at the rock.");
        _ticksPerOre = _currentRock.Name == "Rune essence" ? 1 : 4;
        CurrentAnimation = _currentPickaxe.Animation;
        GatherTimer = _ticksPerOre;
        AnimationTimer = 2;
        IsActive = true;

        _player.RequestAnim(CurrentAnimation, 0);
    }

    public override void Reset()
    {
        base.Reset();
        _currentRock = null;
        _currentPickaxe = null;
    }

    // ── Core logic ──────────────────────────────────────────────────────────

    protected override void OnGatherComplete()
    {
        if (_currentRock == null || _currentPickaxe == null)
        {
            Reset();
            return;
        }

        if (FreeSlotCount() < 1)
        {
            // TODO: p.frames.sendMessage(p, "Not enough inventory space to mine any more ore!");
            Reset();
            return;
        }

        // Grant ore
        AddItem(_currentRock.OreItemId);

        // Grant XP: Java formula is (BaseXp * miningLevel / 3) / 3 = BaseXp * miningLevel / 9, but divide by 3 not 9
        // Java: giveMiningXP((getXpForOre(rockid) * p.skillLvl[14]) / 3) then p.addSkillXP(xp / 3, 14)
        // Final divisor should be 3, not 9
        int miningLevel = _player.SkillLvl[SkillConstants.Mining];
        double xp = (_currentRock.BaseXp * miningLevel) / 3.0;
        _player.AddSkillXP(xp, SkillConstants.Mining);

        // TODO: p.frames.sendMessage(p, "You get some ore.");

        // Mining gives one ore per rock (except rune essence which is continuous)
        if (_currentRock.Name == "Rune essence")
        {
            GatherTimer = _ticksPerOre;
            _player.RequestAnim(CurrentAnimation, 0);
        }
        else
        {
            // Rock depleted after one ore (matches Java MaxAmount = 1 for all rocks)
            Reset();
        }
    }

    // ── Lookup helpers ──────────────────────────────────────────────────────

    public static RockDefinition? FindRock(int objectId)
    {
        foreach (var rock in Rocks)
        {
            foreach (var id in rock.ObjectIds)
            {
                if (id == objectId)
                    return rock;
            }
        }
        return null;
    }

    private PickaxeDefinition? FindBestPickaxe()
    {
        int miningLevel = _player.SkillLvl[SkillConstants.Mining];
        foreach (var pick in Pickaxes)
        {
            if (miningLevel >= pick.LevelRequired && HasItemOrEquipped(pick.ItemId))
                return pick;
        }
        return null;
    }
}
