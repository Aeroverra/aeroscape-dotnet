using System.Collections.Generic;
using AeroScape.Server.Core.Entities;

namespace AeroScape.Server.Core.Skills;

/// <summary>
/// Smithing skill — ported from DavidScape/Skills/Smithing.java.
///
/// The Java code was extremely repetitive: ~1500 lines of switch statements mapping
/// metal types to item IDs and button IDs to products. This C# version uses a
/// data-driven approach with definition tables.
///
/// Smithing involves two phases:
///   1. Smelting: ore + furnace → bar (via ItemOnObject handler)
///   2. Smithing: bar + anvil → item (via interface 300 button handler)
/// </summary>
public class SmithingSkill
{
    private readonly Player _player;
    private static readonly Dictionary<int, int> ButtonToProductIndex = new()
    {
        [19] = 0,
        [27] = 1,
        [35] = 2,
        [43] = 3,
        [51] = 4,
        [59] = 5,
        [67] = 6,
        [75] = 7,
        [107] = 8,
        [115] = 9,
        [123] = 10,
        [131] = 11,
        [139] = 12,
        [147] = 13,
        [155] = 14,
        [179] = 15,
        [187] = 16,
        [195] = 17,
        [203] = 18,
        [211] = 19,
        [219] = 20,
        [227] = 21,
        [235] = 22,
        [243] = 23,
        [268] = 24,
    };

    /// <summary>The current metal type being smithed (1-6). -1 = none.</summary>
    public int CurrentMetalType { get; set; } = -1;

    public SmithingSkill(Player player)
    {
        _player = player;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Metal / Bar definitions
    // ══════════════════════════════════════════════════════════════════════════

    /// <param name="Type">Metal type index (1=Bronze, 2=Iron, 3=Steel, 4=Mithril, 5=Adamant, 6=Rune).</param>
    /// <param name="BarItemId">Item ID of the bar.</param>
    /// <param name="BaseLevelOffset">Starting smithing level for this metal tier.</param>
    /// <param name="XpPerBar">XP per bar used in smithing.</param>
    /// <param name="Name">Display name.</param>
    public record MetalDefinition(int Type, int BarItemId, int BaseLevelOffset, int XpPerBar, string Name);

    public static readonly MetalDefinition[] Metals =
    [
        new(1, 2349, 1,  125, "Bronze"),
        new(2, 2351, 15, 250, "Iron"),
        new(3, 2353, 30, 375, "Steel"),
        new(4, 2359, 50, 500, "Mithril"),
        new(5, 2361, 70, 625, "Adamant"),
        new(6, 2363, 85, 750, "Rune"),
    ];

    // ══════════════════════════════════════════════════════════════════════════
    //  Smithable product definitions
    // ══════════════════════════════════════════════════════════════════════════

    /// <param name="Name">Product name.</param>
    /// <param name="BarsRequired">How many bars needed.</param>
    /// <param name="LevelOffset">Level offset from the metal's base level (0 = base, 1 = base+1, etc.).</param>
    /// <param name="ItemIds">Item IDs indexed by metal type (0=Bronze..5=Rune). -1 if not available.</param>
    public record SmithableProduct(string Name, int BarsRequired, int LevelOffset, int[] ItemIds);

    public static readonly SmithableProduct[] Products =
    [
        new("Dagger",          1, 0,  [1205, 1813, 1207, 1209, 1211, 1213]),
        new("Axe",             1, 1,  [1351, 1349, 1353, 1355, 1357, 1359]),
        new("Mace",            1, 2,  [1422, 1420, 1424, 1428, 1430, 1432]),
        new("Med helm",        1, 3,  [1139, 1137, 1141, 1143, 1145, 1147]),
        new("Bolts",           1, 3,  [877,  9377, 9378, 9379, 9380, 9381]),
        new("Sword",           1, 4,  [1277, 1279, 1281, 1285, 1287, 1289]),
        new("Dart tips",       1, 4,  [819,  820,  821,  822,  823,  824]),
        new("Nails",           1, 4,  [4819, 4820, 1539, 4822, 4823, 4824]),
        new("Arrow tips",      1, 5,  [39,   40,   41,   42,   43,   44]),
        new("Scimitar",        2, 5,  [1321, 1323, 1325, 1329, 1331, 1333]),
        new("Crossbow limbs",  1, 6,  [9420, 9423, 9425, 9427, 9429, 9431]),
        new("Longsword",       2, 6,  [1291, 1293, 1295, 1299, 1301, 1303]),
        new("Throwing knife",  1, 7,  [864,  863,  865,  866,  867,  868]),
        new("Full helm",       2, 7,  [1155, 1153, 1157, 1159, 1161, 1163]),
        new("Square shield",   2, 8,  [1173, 1175, 1177, 1181, 1183, 1185]),
        new("Warhammer",       3, 9,  [2347, 1335, 1339, 1343, 1345, 1347]),
        new("Battleaxe",       3, 10, [1375, 1363, 1365, 1369, 1371, 1373]),
        new("Chainbody",       3, 11, [1103, 1101, 1105, 1109, 1111, 1113]),
        new("Kiteshield",      3, 12, [1189, 1191, 1193, 1197, 1199, 1201]),
        new("Claws",           2, 13, [3095, 3096, 3097, 3099, 3100, 3101]),
        new("2H sword",        3, 14, [1307, 1309, 1311, 1315, 1317, 1319]),
        new("Plateskirt",      3, 16, [1087, 1081, 1083, 1085, 1091, 1093]),
        new("Platelegs",       3, 16, [1075, 1067, 1069, 1071, 1073, 1079]),
        new("Platebody",       5, 18, [1117, 1115, 1119, 1121, 1123, 1127]),
        new("Pickaxe",         2, 5,  [1265, 1267, 1269, 1273, 1271, 1275]),
    ];

    // ══════════════════════════════════════════════════════════════════════════
    //  Smelting definitions (Ore → Bar via furnace)
    // ══════════════════════════════════════════════════════════════════════════

    /// <param name="BarItemId">Resulting bar item ID.</param>
    /// <param name="OreRequirements">Array of (itemId, amount) tuples for required ores.</param>
    /// <param name="LevelRequired">Minimum Smithing level.</param>
    /// <param name="Xp">XP granted per bar smelted.</param>
    /// <param name="Name">Display name.</param>
    public record SmeltDefinition(
        int BarItemId,
        (int ItemId, int Amount)[] OreRequirements,
        int LevelRequired,
        double Xp,
        string Name);

    public static readonly SmeltDefinition[] Smelts =
    [
        new(2349, [(436, 1), (438, 1)],    1,  6.2,  "Bronze bar"),
        new(2351, [(440, 1)],               15, 12.5, "Iron bar"),
        new(2355, [(442, 1)],               20, 13.7, "Silver bar"),
        new(2353, [(440, 1), (453, 2)],     30, 17.5, "Steel bar"),
        new(2357, [(444, 1)],               40, 22.5, "Gold bar"),
        new(2359, [(447, 1), (453, 4)],     50, 30.0, "Mithril bar"),
        new(2361, [(449, 1), (453, 6)],     70, 37.5, "Adamant bar"),
        new(2363, [(451, 1), (453, 8)],     85, 50.0, "Rune bar"),
    ];

    /// <summary>Furnace object IDs.</summary>
    public static readonly int[] FurnaceObjects = [56332, 11666, 3994, 4304];

    /// <summary>Anvil object IDs.</summary>
    public static readonly int[] AnvilObjects = [54540, 2783];

    // ══════════════════════════════════════════════════════════════════════════
    //  Public API
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Open the smithing interface for a given metal type.
    /// Called from ObjectOption2 handler (bar on anvil).
    /// </summary>
    /// <param name="barItemId">The bar item ID used on the anvil.</param>
    public void OpenSmithingInterface(int barItemId)
    {
        var metal = FindMetalByBar(barItemId);
        if (metal == null)
            return;

        CurrentMetalType = metal.Type;

        // TODO: When frames are implemented:
        // p.frames.showInterface(p, 300);
        // Set all the string labels, item icons, bar counts, level requirements
        // (This will be a large method similar to the Java Iof/amoutofbars/LvlReq methods)
    }

    /// <summary>
    /// Smith an item. Called from ActionButton handler when player clicks
    /// a product on interface 300.
    /// </summary>
    /// <param name="productIndex">Index into the Products array.</param>
    /// <param name="amount">How many to smith.</param>
    public void SmithItem(int productIndex, int amount = 1)
    {
        if (ButtonToProductIndex.TryGetValue(productIndex, out var mappedProductIndex))
            productIndex = mappedProductIndex;

        if (CurrentMetalType < 1 || CurrentMetalType > 6)
            return;

        if (productIndex < 0 || productIndex >= Products.Length)
            return;

        var product = Products[productIndex];
        var metal = FindMetal(CurrentMetalType);
        if (metal == null)
            return;

        int metalIndex = metal.Type - 1; // 0-based for array access
        int itemId = product.ItemIds[metalIndex];
        if (itemId == -1)
            return; // Product not available for this metal

        int requiredLevel = metal.BaseLevelOffset + product.LevelOffset;
        int smithLevel = _player.SkillLvl[SkillConstants.Smithing];

        if (smithLevel < requiredLevel)
        {
            // TODO: p.frames.sendMessage(p, "You need a Smithing level of " + requiredLevel + " to make this.");
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            // Check bar count
            if (ItemCount(metal.BarItemId) < product.BarsRequired)
            {
                // TODO: p.frames.sendMessage(p, "You do not have enough bars to make this item!");
                break;
            }

            // Consume bars
            for (int b = 0; b < product.BarsRequired; b++)
                DeleteItem(metal.BarItemId);

            // Grant item
            AddItem(itemId);

            // Grant XP
            double xp = (product.BarsRequired * metal.XpPerBar) / 40.0;
            _player.AddSkillXP(xp, SkillConstants.Smithing);

            // TODO: p.requestAnim(898, 0); // Smithing animation
            // TODO: p.frames.sendMessage(p, $"You used {product.BarsRequired} bars!");
        }

        // TODO: p.frames.removeShownInterface(p);
    }

    /// <summary>
    /// Smelt ore into a bar at a furnace.
    /// Called from ItemOnObject handler (ore on furnace).
    /// </summary>
    /// <param name="oreItemId">The ore item used on the furnace.</param>
    /// <param name="amount">How many to smelt.</param>
    public void SmeltOre(int oreItemId, int amount = 1)
    {
        var smelt = FindSmeltByOre(oreItemId, ItemCount(453));
        if (smelt == null)
            return;

        int smithLevel = _player.SkillLvl[SkillConstants.Smithing];
        if (smithLevel < smelt.LevelRequired)
        {
            // TODO: p.frames.sendMessage(p, $"You need a Smithing level of {smelt.LevelRequired} to smelt this.");
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            // Check all ore requirements
            bool hasAll = true;
            foreach (var (reqItemId, reqAmount) in smelt.OreRequirements)
            {
                if (ItemCount(reqItemId) < reqAmount)
                {
                    hasAll = false;
                    break;
                }
            }

            if (!hasAll)
            {
                // TODO: p.frames.sendMessage(p, "You don't have the right ores to smelt this bar.");
                break;
            }

            // Consume ores
            foreach (var (reqItemId, reqAmount) in smelt.OreRequirements)
            {
                for (int r = 0; r < reqAmount; r++)
                    DeleteItem(reqItemId);
            }

            // Grant bar
            AddItem(smelt.BarItemId);

            // Grant XP
            _player.AddSkillXP(smelt.Xp, SkillConstants.Smithing);

            // TODO: p.requestAnim(smelting animation, 0);
            // TODO: p.frames.sendMessage(p, "You smelt the ore into a bar.");
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Lookup helpers
    // ══════════════════════════════════════════════════════════════════════════

    public static MetalDefinition? FindMetal(int type)
    {
        foreach (var m in Metals)
            if (m.Type == type) return m;
        return null;
    }

    public static MetalDefinition? FindMetalByBar(int barItemId)
    {
        foreach (var m in Metals)
            if (m.BarItemId == barItemId) return m;
        return null;
    }

    public static SmeltDefinition? FindSmeltByOre(int oreItemId, int coalCount = 0)
    {
        if (oreItemId == 440)
        {
            if (coalCount >= 2)
                return FindSmeltByBar(2353);
            if (coalCount == 0)
                return FindSmeltByBar(2351);
            return null;
        }

        foreach (var s in Smelts)
        {
            foreach (var (reqItem, _) in s.OreRequirements)
            {
                if (reqItem == oreItemId)
                    return s;
            }
        }
        return null;
    }

    private static SmeltDefinition? FindSmeltByBar(int barItemId)
    {
        foreach (var s in Smelts)
            if (s.BarItemId == barItemId) return s;
        return null;
    }

    public static bool IsFurnaceObject(int objectId)
    {
        foreach (var id in FurnaceObjects)
            if (id == objectId) return true;
        return false;
    }

    public static bool IsAnvilObject(int objectId)
    {
        foreach (var id in AnvilObjects)
            if (id == objectId) return true;
        return false;
    }

    // ── Inventory helpers ───────────────────────────────────────────────────

    private int ItemCount(int itemId)
    {
        int count = 0;
        for (int i = 0; i < _player.Items.Length; i++)
            if (_player.Items[i] == itemId) count += _player.ItemsN[i];
        return count;
    }

    private bool AddItem(int itemId)
    {
        for (int i = 0; i < _player.Items.Length; i++)
        {
            if (_player.Items[i] == -1)
            {
                _player.Items[i] = itemId;
                _player.ItemsN[i] = 1;
                return true;
            }
        }
        return false;
    }

    private bool DeleteItem(int itemId)
    {
        for (int i = 0; i < _player.Items.Length; i++)
        {
            if (_player.Items[i] == itemId)
            {
                _player.Items[i] = -1;
                _player.ItemsN[i] = 0;
                return true;
            }
        }
        return false;
    }
}
