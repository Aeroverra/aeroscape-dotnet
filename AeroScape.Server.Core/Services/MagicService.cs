using System;
using System.Collections.Generic;
using System.Linq;
using AeroScape.Server.Core.Combat;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Items;

namespace AeroScape.Server.Core.Services;

public sealed class MagicService(PlayerItemsService playerItems)
{
    public static readonly double[] ModernSpellXp =
    {
        0, 5.5, 13, 0, 7.5, 9.5, 21, 11.5, 25, 13.5, 29, 30, 31, 16.5, 35, 37,
        19.5, 41, 45, 43, 22.5, 48, 24.5, 30, 25.5, 53, 55.5, 28.5, 59, 30, 60,
        30, 61, 31.5, 65, 66, 67, 68, 35.5, 70, 35.5, 35, 35, 35, 68, 36, 73, 74,
        37.5, 76, 76, 78, 40, 83, 84, 42.5, 89, 90, 180, 92, 97, 100
    };

    public static readonly int[] ModernLevelRequirements =
    {
        0, 1, 3, 4, 5, 7, 9, 11, 13, 15, 17, 19, 20, 21, 23, 25, 27, 29, 31, 32,
        33, 35, 37, 39, 40, 41, 43, 45, 47, 49, 50, 50, 50, 51, 53, 55, 56, 57,
        58, 59, 60, 60, 60, 60, 60, 61, 62, 63, 64, 65, 66, 66, 68, 70, 73, 74,
        75, 79, 80, 80, 82, 87, 90
    };

    public bool TryCastModernAction(Player player, int buttonId)
    {
        if (buttonId == 0)
        {
            if (player.HomeTeleDelay <= 0)
            {
                player.HomeTele = 15;
                player.NormalHomeTele = true;
                return true;
            }

            return false;
        }

        return buttonId switch
        {
            9 => CastBonesSpell(player, 1963, 15, 2, 2, 1),
            15 => CastTeleport(player, buttonId, 3212, 3434, (556, 3), (563, 1), (554, 1)),
            18 => CastTeleport(player, buttonId, 3221, 3219, (556, 3), (563, 1), (557, 1)),
            21 => CastTeleport(player, buttonId, 2965, 3380, (556, 3), (563, 1), (555, 1)),
            26 => CastTeleport(player, buttonId, 2757, 3477, (556, 5), (563, 1)),
            32 => CastTeleport(player, buttonId, 2662, 3305, (555, 2), (563, 2)),
            37 => CastTeleport(player, buttonId, 2545, 3112, (557, 2), (563, 2)),
            40 => CastBonesSpell(player, 6883, 40, 2, 2, 1),
            44 => CastTeleport(player, buttonId, 2891, 3678, (554, 2), (563, 2)),
            47 => CastTeleport(player, buttonId, 2755, 2784, (554, 2), (563, 2), (555, 2), (1963, 1)),
            58 => CastCharge(player),
            _ => false,
        };
    }

    public bool TryCastLunarAction(Player player, int buttonId)
    {
        if (buttonId != 14 || player.SkillLvl[6] < 94 || player.VengOn)
            return false;

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (now - player.LastVengeanceTime < 30_000)
            return false;

        if (!HasRunes(player, (557, 10), (560, 2), (9075, 4)))
            return false;

        ConsumeRunes(player, (557, 10), (560, 2), (9075, 4));
        player.RequestAnim(4410, 0);
        player.RequestGfx(726, 0);
        player.VengOn = true;
        player.LastVengeanceTime = now;
        return true;
    }

    public bool TryCastAncientAction(Player player, int buttonId)
    {
        if (buttonId != 24)
            return false;

        if (player.HomeTeleDelay > 0)
            return false;

        player.HomeTele = 15;
        player.AncientsHomeTele = true;
        return true;
    }

    public bool TryConsumeCombatRunes(Player player, SpellDefinition spell)
    {
        // Validate spell definition exists to prevent null reference exceptions
        if (spell == null || spell.RuneRequirements == null)
            return false;
            
        if (!HasRunes(player, spell.RuneRequirements.Select(r => (r.RuneId, r.Amount)).ToArray()))
            return false;

        ConsumeRunes(player, spell.RuneRequirements.Select(r => (r.RuneId, r.Amount)).ToArray());
        return true;
    }

    public bool TryLowAlchemy(Player player, int itemId, int slot)
        => TryAlchemy(player, itemId, slot, 218152960, 3, 12);

    public bool TryHighAlchemy(Player player, int itemId, int slot)
        => TryAlchemy(player, itemId, slot, 570474496, 5, 34);

    public bool TrySuperheat(Player player, int itemId)
    {
        if (player.MagicDelay > 0 || player.SkillLvl[6] < ModernLevelRequirements[25] || !HasRunes(player, (561, 1), (554, 4)))
            return false;

        var recipe = FindSuperheatRecipe(player, itemId);
        if (recipe == null || player.SkillLvl[13] < recipe.Value.SmithLevel)
            return false;

        if (!HasRunes(player, (561, 1), (554, 4)) || !HasRunes(player, recipe.Value.Requirements))
            return false;

        ConsumeRunes(player, (561, 1), (554, 4));
        ConsumeRunes(player, recipe.Value.Requirements);
        playerItems.AddItem(player, recipe.Value.BarItemId, 1);
        player.AddSkillXP(ModernSpellXp[25] * 3, 6);
        player.MagicDelay = 3;
        return true;
    }

    public bool TryEnchant(Player player, int spellId, int itemId)
    {
        if (!Enchantments.TryGetValue(spellId, out var enchant))
            return false;

        if (player.SkillLvl[6] < enchant.LevelRequired || !HasRunes(player, enchant.Runes))
            return false;

        if (!enchant.Transforms.TryGetValue(itemId, out var result))
            return false;

        var slot = playerItems.GetItemSlot(player, itemId);
        if (slot < 0)
            return false;

        playerItems.DeleteItem(player, itemId, slot, 1);
        playerItems.AddItem(player, result, 1);
        ConsumeRunes(player, enchant.Runes);
        player.AddSkillXP(enchant.Xp * 3, 6);
        return true;
    }

    private bool TryAlchemy(Player player, int itemId, int slot, int spellId, int fireRunes, int levelIndex)
    {
        if (player.MagicDelay > 0 || player.SkillLvl[6] < ModernLevelRequirements[levelIndex] || !HasRunes(player, (561, 1), (554, fireRunes)))
            return false;

        if (slot < 0 || slot >= player.Items.Length || player.Items[slot] != itemId)
            return false;

        playerItems.DeleteItem(player, itemId, slot, 1);
        ConsumeRunes(player, (561, 1), (554, fireRunes));
        playerItems.AddItem(player, 995, 2);
        player.AddSkillXP(ModernSpellXp[levelIndex] * 3, 6);
        player.MagicDelay = 3;
        return true;
    }

    private bool CastBonesSpell(Player player, int outputItemId, int buttonId, int earth, int water, int nature)
    {
        if (player.SkillLvl[6] < ModernLevelRequirements[buttonId] || playerItems.InvItemCount(player, 526) <= 0)
            return false;

        if (!HasRunes(player, (557, earth), (555, water), (561, nature)))
            return false;

        var amount = playerItems.InvItemCount(player, 526);
        playerItems.DeleteItem(player, 526, amount);
        ConsumeRunes(player, (557, earth), (555, water), (561, nature));
        playerItems.AddItem(player, outputItemId, amount);
        player.AddSkillXP(ModernSpellXp[buttonId] * 3, 6);
        player.MagicDelay = 3;
        return true;
    }

    private bool CastTeleport(Player player, int buttonId, int x, int y, params (int ItemId, int Amount)[] runes)
    {
        if (player.MagicDelay > 0 || player.SkillLvl[6] < ModernLevelRequirements[buttonId] || !HasRunes(player, runes))
            return false;

        ConsumeRunes(player, runes);
        player.TeleX = x;
        player.TeleY = y;
        player.TeleDelay = 4;
        player.ClickDelay = 6;
        player.TeleFinishGfx = 1577;
        player.TeleFinishGfxHeight = 0;
        player.TeleFinishAnim = 8941;
        player.RequestAnim(8939, 0);
        player.RequestGfx(1576, 0);
        player.AddSkillXP(ModernSpellXp[buttonId] * 3, 6);
        return true;
    }

    private bool CastCharge(Player player)
    {
        if (player.ActionTimer != 0 || player.MagicDelay > 0 || player.SkillLvl[6] < ModernLevelRequirements[58])
            return false;

        if (!HasRunes(player, (565, 3), (554, 3), (556, 3)))
            return false;

        ConsumeRunes(player, (565, 3), (554, 3), (556, 3));
        player.ActionTimer = 2;
        player.ArenaSpellPower = 1.3;
        player.RequestAnim(811, 0);
        player.AddSkillXP(ModernSpellXp[58] * 3, 6);
        return true;
    }

    private bool HasRunes(Player player, params (int ItemId, int Amount)[] runes)
        => runes.All(r => playerItems.HasItemAmount(player, r.ItemId, r.Amount));

    private void ConsumeRunes(Player player, params (int ItemId, int Amount)[] runes)
    {
        foreach (var rune in runes)
            playerItems.DeleteItem(player, rune.ItemId, rune.Amount);
    }

    private readonly record struct SuperheatRecipe(int OreItemId, int BarItemId, int SmithLevel, params (int ItemId, int Amount)[] Requirements);
    private static readonly SuperheatRecipe[] SuperheatRecipes =
    [
        new(436, 2349, 1, (436, 1), (438, 1)),  // Bronze: copper + tin
        new(438, 2349, 1, (436, 1), (438, 1)),  // Bronze: tin + copper
        new(440, 2351, 15, (440, 1)),           // Iron: iron ore only
        new(440, 2353, 30, (440, 1), (453, 2)), // Steel: iron ore + 2 coal
        new(447, 2359, 50, (447, 1), (453, 4)), // Mithril
        new(449, 2361, 70, (449, 1), (453, 6)), // Adamant
        new(451, 2363, 85, (451, 1), (453, 8)), // Rune
        new(668, 9467, 8, (668, 1)),            // Elemental
        new(442, 2355, 20, (442, 1)),           // Silver
        new(444, 2357, 40, (444, 1)),           // Gold
    ];

    private sealed record EnchantDefinition(int LevelRequired, double Xp, (int ItemId, int Amount)[] Runes, Dictionary<int, int> Transforms);
    private static readonly Dictionary<int, EnchantDefinition> Enchantments = new()
    {
        [83935232] = new(5, ModernSpellXp[4], [(564, 1), (557, 1)], new Dictionary<int, int> { [1637] = 2550, [1692] = 1727, [11069] = 3853, [5641] = 64 }),
        [268484608] = new(25, ModernSpellXp[15], [(564, 1), (556, 3)], new Dictionary<int, int> { [1639] = 2552, [1694] = 1729, [11072] = 5521, [5643] = 65 }),
        [469811200] = new(49, ModernSpellXp[28], [(564, 1), (555, 3)], new Dictionary<int, int> { [1641] = 2568, [1696] = 1731, [11079] = 11192, [5645] = 66 }),
        [604028928] = new(57, ModernSpellXp[36], [(564, 1), (557, 10), (554, 5)], new Dictionary<int, int> { [1643] = 2570, [1698] = 1733, [11085] = 11193, [5647] = 67 }),
        [855687168] = new(68, ModernSpellXp[51], [(564, 1), (561, 1), (554, 15)], new Dictionary<int, int> { [1645] = 2572, [1700] = 1735, [11092] = 11194 }),
        [1023459328] = new(87, ModernSpellXp[60], [(564, 1), (554, 20), (565, 20)], new Dictionary<int, int> { [1647] = 2574, [1702] = 1737, [11115] = 11195, [6581] = 6583 }),
    };

    private SuperheatRecipe? FindSuperheatRecipe(Player player, int oreItemId)
    {
        if (oreItemId == 440) // Iron ore
        {
            var coalCount = playerItems.InvItemCount(player, 453);
            // Java: if ((itemID == 440) && hasReq(p, 453, 2)) - requires exactly 2 coal for steel
            if (coalCount == 2)
            {
                // Return steel bar recipe only if we have exactly 2 coal + 1 iron
                var ironCount = playerItems.InvItemCount(player, 440);
                if (ironCount >= 1)
                    return GetSuperheatRecipeByBar(2353); // Steel bar
            }
            if (coalCount == 1)
            {
                // Player has only 1 coal -> error message (but this needs to be handled by caller)
                return null; // This will cause spell to fail with appropriate error
            }
            if (coalCount == 0)
                return GetSuperheatRecipeByBar(2351); // Iron bar (no coal)
            return null;
        }

        foreach (var recipe in SuperheatRecipes)
        {
            if (recipe.OreItemId == oreItemId)
                return recipe;
        }

        return null;
    }

    private static SuperheatRecipe? GetSuperheatRecipeByBar(int barItemId)
    {
        foreach (var recipe in SuperheatRecipes)
        {
            if (recipe.BarItemId == barItemId)
                return recipe;
        }

        return null;
    }
}
