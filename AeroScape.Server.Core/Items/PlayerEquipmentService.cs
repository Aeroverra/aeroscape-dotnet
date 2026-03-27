using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Frames;

namespace AeroScape.Server.Core.Items;

public sealed class PlayerEquipmentService(ItemDefinitionLoader items, PlayerItemsService playerItems, GameFrames frames)
{
    private static readonly string[] Capes = ["cape", "Cape", "cloak", "Cloak"];
    private static readonly string[] Hats =
    [
        "helm", "hood", "Helm", "coif", "Coif", "hat", "mitre", "partyhat", "Hat", "helmet", "mask",
        "full helm (t)", "full helm (g)", "hat (t)", "hat (g)", "cav", "boater", "Feather headdress", "tiara", "Tiara",
        "Helm of neitiznot", "Mime mask", "Sleep", "sleep", "bandana", "Bandana", "eyepatch", "Eyepatch", "bunny ears", "Bunny Ears", "bunny", "Bunny"
    ];
    private static readonly string[] Boots = ["boots", "Boots"];
    private static readonly string[] Gloves = ["gloves", "gauntlets", "Gloves", "vambraces", "vamb", "bracers"];
    private static readonly string[] Shields = ["kiteshield", "sq shield", "Toktz-ket", "books", "book", "kiteshield (t)", "kiteshield (g)", "kiteshield(h)", "defender", "shield", "Saradomin kite", "Spirit Shield", "Book"];
    private static readonly string[] Amulets = ["amulet", "necklace", "stole", "Amulet of", "scarf", "Scarf"];
    private static readonly string[] Arrows = ["arrow", "arrows", "arrow(p)", "arrow(+)", "arrow(s)", "bolt", "Bolt rack", "Opal bolts", "Dragon bolts"];
    private static readonly string[] Rings = ["ring"];
    private static readonly string[] Body =
    [
        "platebody", "chainbody", "blouse", "robetop", "leathertop", "platemail", "top", "brassard", "Robe top", "body",
        "Saradomin plate",
        "chestplate", "torso", "shirt", "Varrock armour", "Prince tunic", "Runecrafter robe", "Zamorak d'hide", "Guthix d'hide", "Saradomin d'hide"
    ];
    private static readonly string[] Legs =
    [
        "platelegs", "knight robe", "plateskirt", "skirt", "bottoms", "chaps", "bottom", "tassets", "legs", "trousers", "shorts", "Shorts", "Bottom", "Bottoms"
    ];
    private static readonly string[] Weapons =
    [
        "secateurs", "scimitar", "Rubber chicken", "longsword", "Swords", "Longsword", "sword", "crozier", "longbow", "shortbow", "dagger", "dagger(p)", "dagger(+)", "dagger(s)", "mace", "halberd", "spear",
        "spear(p)", "spear(+)", "spear(s)", "spear(kp)", "Abyssal whip", "axe", "flail", "crossbow", "Torags hammers", "maul", "dart", "dart(p)", "javelin", "javelin(p)", "knife", "knife(p)", "Crossbow", "Toktz-xil", "Toktz-mej",
        "Tzhaar-ket", "staff", "Staff", "Scythe", "scythe", "sickle", "godsword", "c'bow", "Crystal bow", "Dark bow", "Magic butterfly net", "Gnomecopter", "Flowers", "Invisibility", "claws", "Claws", "banner", "Warhammer", "warhammer", "wand", "Wand"
    ];
    private static readonly string[] FullBody =
    [
        "top", "shirt", "Shirt", "blouse", "platebody", "Platebody", "Zamorak d'hide", "Ahrims robetop", "Karils leathertop",
        "brassard", "Robe top", "robetop", "platebody (t)", "platebody (g)", "chestplate", "torso", "chainbody", "Varrock armour",
        "Guthix d'hide", "Saradomin d'hide", "Prince tunic", "Wizard robe (g)", "Wizard robe (t)", "Runecrafter robe"
    ];
    private static readonly string[] FullHat = ["med helm", "Dharoks helm", "hood", "Initiate helm", "Coif", "Helm of neitiznot"];
    private static readonly string[] FullMask = ["full helm", "Slayer helmet", "Veracs helm", "Guthans helm", "Armadyl h", "Torags helm", "Karils coif", "full helm (t)", "full helm (g)", "Green h'ween mask", "Red h'ween mask", "Blue h'ween mask", "full helmet"];
    private static readonly int[] StaffItems = [7806, 7807, 7808, 7809, 6106, 6107, 6108, 6109, 6110, 6111, 4345, 6856, 6857, 6858, 6859, 6860, 6861, 6862, 6863, 8942, 1231, 4353, 1203, 4331];
    private static readonly int[] MemberItems = [4566, 5553, 5554, 5555, 5556, 5557, 2653, 2655, 2657, 2659, 2661, 2663, 2665, 2667, 2669, 2671, 2673, 2675, 3481, 3483, 3486, 3488, 12222, 534, 3101, 1337, 1361];

    public bool Equip(Player player, int itemId, int slot, int interfaceId)
    {
        if (interfaceId != 149 || slot < 0 || slot >= player.Items.Length || player.Items[slot] != itemId)
        {
            return false;
        }

        var targetSlot = GetItemType(itemId);
        if (targetSlot < 0)
        {
            return false;
        }

        if (!CanEquip(player, itemId, targetSlot))
        {
            return false;
        }

        if (itemId == 4021)
        {
            player.NpcType = 1463;
            player.AppearanceUpdateReq = true;
            player.UpdateReq = true;
        }

        if (targetSlot == 2 && player.Equipment[2] == 4021 && itemId != 4021)
        {
            player.NpcType = -1;
            player.AppearanceUpdateReq = true;
            player.UpdateReq = true;
        }

        if (itemId == 12842)
        {
            for (var i = 0; i < 13; i++)
            {
                if (i != 12 && i != 3 && player.Equipment[i] != -1)
                {
                    return false;
                }
            }
        }

        if (targetSlot == 3 && IsTwoHanded(itemId) && player.Equipment[5] != -1 && playerItems.FreeSlotCount(player) < 1)
        {
            return false;
        }

        var amount = items.IsStackable(itemId) ? player.ItemsN[slot] : 1;
        var previousItem = player.Equipment[targetSlot];
        var previousAmount = player.EquipmentN[targetSlot];

        if (targetSlot == 3 && IsTwoHanded(itemId) && player.Equipment[5] != -1)
        {
            if (!playerItems.AddItem(player, player.Equipment[5], player.EquipmentN[5]))
            {
                return false;
            }

            player.Equipment[5] = -1;
            player.EquipmentN[5] = 0;
        }

        if (targetSlot == 5 && player.Equipment[3] != -1 && IsTwoHanded(player.Equipment[3]))
        {
            if (!playerItems.AddItem(player, player.Equipment[3], player.EquipmentN[3]))
            {
                return false;
            }

            player.Equipment[3] = -1;
            player.EquipmentN[3] = 0;
        }

        if (items.IsStackable(itemId) && player.Equipment[targetSlot] == itemId)
        {
            player.EquipmentN[targetSlot] += amount;
            playerItems.DeleteItem(player, itemId, slot, amount);
            ApplyWeaponState(player);
            CheckSpecials(player);
            RecalculateBonuses(player);
            player.AppearanceUpdateReq = true;
            player.UpdateReq = true;
            return true;
        }

        player.Equipment[targetSlot] = itemId;
        player.EquipmentN[targetSlot] = amount;
        playerItems.DeleteItem(player, itemId, slot, amount);

        if (previousItem != -1)
        {
            playerItems.AddItem(player, previousItem, previousAmount > 0 ? previousAmount : 1);
        }

        ApplyWeaponState(player);
        CheckSpecials(player);
        RecalculateBonuses(player);
        
        // Check for ancient staff magic interface switch like Java Equipment.java:167-175
        if (targetSlot == 3 && itemId == 4675) // Ancient staff equipped in weapon slot
        {
            player.IsAncients = 1;
            // Send magic interface update based on HD client like Java
            if (player.UsingHd)
            {
                Write(player, w => frames.SetInterface(w, 1, 746, 93, 193)); // HD Ancients tab
            }
            else
            {
                Write(player, w => frames.SetInterface(w, 1, 548, 79, 193)); // Non-HD Ancients tab
            }
        }
        else if (targetSlot == 3 && itemId != 4675) // Other weapon equipped, reset ancients
        {
            player.IsAncients = 0;
        }
        
        // Send equipment frame updates like Java Equipment.java:252-253
        Write(player, w => frames.SetItems(w, 387, 28, 94, player.Equipment, player.EquipmentN));
        
        player.AppearanceUpdateReq = true;
        player.UpdateReq = true;
        return true;
    }

    public void RecalculateBonuses(Player player)
    {
        Array.Clear(player.EquipmentBonus);
        for (var slot = 0; slot < player.Equipment.Length; slot++)
        {
            var itemId = player.Equipment[slot];
            if (itemId < 0)
            {
                continue;
            }

            var bonuses = items.GetBonuses(itemId);
            for (var i = 0; i < player.EquipmentBonus.Length && i < bonuses.Length; i++)
            {
                player.EquipmentBonus[i] += bonuses[i];
            }
        }
    }

    public void ApplyWeaponState(Player player)
    {
        var weaponId = player.Equipment[3];
        player.WalkEmote = GetWalkEmote(weaponId);
        player.RunEmote = GetRunEmote(weaponId);
        player.StandEmote = GetStandEmote(weaponId);
        player.AttackEmote = GetAttackEmote(weaponId);
        player.AttackDelay = GetAttackDelay(weaponId);
    }

    public bool IsFullbody(int itemId) => ContainsAny(items.GetItemName(itemId), FullBody);

    public bool IsFullhat(int itemId) => EndsWithAny(items.GetItemName(itemId), FullHat);

    public bool IsFullmask(int itemId) => EndsWithAny(items.GetItemName(itemId), FullMask);

    private int GetItemType(int itemId)
    {
        var name = items.GetItemName(itemId);
        if (ContainsAny(name, Capes)) return 1;
        if (ContainsAny(name, Hats)) return 0;
        if (StartsOrEndsWithAny(name, Boots)) return 10;
        if (StartsOrEndsWithAny(name, Gloves)) return 9;
        if (ContainsAny(name, Shields)) return 5;
        if (StartsOrEndsWithAny(name, Amulets)) return 2;
        if (StartsOrEndsWithAny(name, Arrows)) return 13;
        if (StartsOrEndsWithAny(name, Rings)) return 12;
        if (ContainsAny(name, Body)) return 4;
        if (ContainsAny(name, Legs)) return 7;
        if (StartsOrEndsWithAny(name, Weapons)) return 3;
        return -1;
    }

    private bool IsTwoHanded(int itemId)
    {
        var weapon = items.GetItemName(itemId);
        return itemId is 4212 or 1231 or 4214 or 12842
            || weapon.EndsWith("2h sword", StringComparison.Ordinal)
            || weapon.EndsWith("Staff of Light", StringComparison.Ordinal)
            || weapon.EndsWith("net", StringComparison.Ordinal)
            || weapon.EndsWith("longbow", StringComparison.Ordinal)
            || weapon.EndsWith("shortbow", StringComparison.Ordinal)
            || weapon.EndsWith("Longbow", StringComparison.Ordinal)
            || weapon.EndsWith("Shortbow", StringComparison.Ordinal)
            || weapon.EndsWith("bow full", StringComparison.Ordinal)
            || weapon.EndsWith("halberd", StringComparison.Ordinal)
            || weapon.EndsWith("godsword", StringComparison.Ordinal)
            || weapon.Equals("Seercull", StringComparison.Ordinal)
            || weapon.Equals("Granite maul", StringComparison.Ordinal)
            || weapon.Equals("Karils crossbow", StringComparison.Ordinal)
            || weapon.Equals("Torags hammers", StringComparison.Ordinal)
            || weapon.Equals("Veracs flail", StringComparison.Ordinal)
            || weapon.Equals("Dharoks greataxe", StringComparison.Ordinal)
            || weapon.Equals("Guthans warspear", StringComparison.Ordinal)
            || weapon.Equals("Tzhaar-ket-om", StringComparison.Ordinal)
            || weapon.Equals("Saradomin sword", StringComparison.Ordinal)
            || weapon.Contains("claws", StringComparison.OrdinalIgnoreCase)
            || weapon.Contains("warhammer", StringComparison.OrdinalIgnoreCase);
    }

    private bool CanEquip(Player player, int itemId, int targetSlot)
    {
        if (player.Equipment[3] == 12842 && targetSlot != 3)
        {
            return false;
        }

        if (StaffItems.Contains(itemId) && player.Rights < 1)
        {
            return false;
        }

        if (MemberItems.Contains(itemId) && player.Rights < 1 && player.Member < 1)
        {
            return false;
        }

        var name = items.GetItemName(itemId);
        if ((name.Contains("cape", StringComparison.OrdinalIgnoreCase) || name.Contains("hood", StringComparison.OrdinalIgnoreCase)) && RequiresSkillCapeLevel(player, name))
        {
            return false;
        }

        if ((itemId == 3140 || itemId == 1127) && player.DragonSlayer != 5)
        {
            return false;
        }

        if ((itemId == 9813 || itemId == 9814) && player.QuestPoints < 2)
        {
            return false;
        }

        if (GetAttackRequirement(itemId) > player.GetLevelForXP(0) ||
            GetDefenceRequirement(itemId) > player.GetLevelForXP(1) ||
            GetStrengthRequirement(itemId) > player.GetLevelForXP(2) ||
            GetRangedRequirement(itemId) > player.GetLevelForXP(4) ||
            GetMagicRequirement(itemId) > player.GetLevelForXP(6) ||
            GetCraftingRequirement(itemId) > player.GetLevelForXP(20))
        {
            return false;
        }

        return true;
    }

    private bool RequiresSkillCapeLevel(Player player, string itemName)
    {
        for (var i = 0; i < 24; i++)
        {
            var skillName = SkillName(i);
            if (itemName.Contains(skillName, StringComparison.OrdinalIgnoreCase) && player.GetLevelForXP(i) < 120)
            {
                return true;
            }
        }

        if (itemName.Contains("woodcut", StringComparison.OrdinalIgnoreCase) && player.GetLevelForXP(8) < 120) return true;
        if (itemName.Contains("constru", StringComparison.OrdinalIgnoreCase) && player.GetLevelForXP(22) < 120) return true;
        if (itemName.Contains("runecraft", StringComparison.OrdinalIgnoreCase) && player.GetLevelForXP(20) < 120) return true;
        return false;
    }

    private void CheckSpecials(Player player)
    {
        player.SpecialBarInterface = -1;
        player.SpecialBarChild = -1;
        var weaponId = player.Equipment[3];
        if (weaponId == 4151) (player.SpecialBarInterface, player.SpecialBarChild) = (93, 10);
        else if (weaponId is 1215 or 1231 or 5680 or 5698 or 8872 or 8874 or 8876 or 8878) (player.SpecialBarInterface, player.SpecialBarChild) = (89, 12);
        else if (weaponId is 35 or 1305 or 4587 or 6746 or 11037) (player.SpecialBarInterface, player.SpecialBarChild) = (82, 12);
        else if (weaponId is 7158 or 11694 or 11696 or 11698 or 11700 or 11730 or 7806 or 7807 or 7808 or 7809) (player.SpecialBarInterface, player.SpecialBarChild) = (81, 12);
        else if (weaponId is 859 or 861 or 6724 or 10284 or 11235) (player.SpecialBarInterface, player.SpecialBarChild) = (77, 13);
        else if (weaponId == 8880) (player.SpecialBarInterface, player.SpecialBarChild) = (79, 10);
        else if (weaponId == 3101) (player.SpecialBarInterface, player.SpecialBarChild) = (78, 12);
        else if (weaponId is 1434 or 11061 or 10887) (player.SpecialBarInterface, player.SpecialBarChild) = (88, 12);
        else if (weaponId is 1377 or 6739) (player.SpecialBarInterface, player.SpecialBarChild) = (75, 12);
        else if (weaponId == 4153) (player.SpecialBarInterface, player.SpecialBarChild) = (76, 10);
        else if (weaponId == 3204) (player.SpecialBarInterface, player.SpecialBarChild) = (84, 10);
    }

    private static string SkillName(int skillId) => skillId switch
    {
        0 => "Attack",
        1 => "Defence",
        2 => "Strength",
        3 => "Hitpoints",
        4 => "Ranged",
        5 => "Prayer",
        6 => "Magic",
        7 => "Cooking",
        8 => "Woodcutting",
        9 => "Fletching",
        10 => "Fishing",
        11 => "Firemaking",
        12 => "Crafting",
        13 => "Smithing",
        14 => "Mining",
        15 => "Herblore",
        16 => "Agility",
        17 => "Thieving",
        18 => "Slayer",
        19 => "Farming",
        20 => "Runecrafting",
        21 => "Hunter",
        22 => "Construction",
        23 => "Summoning",
        _ => string.Empty
    };

    private int GetRangedRequirement(int itemId) => itemId switch
    {
        2499 or 2487 or 2493 => 50,
        2501 or 2489 or 2495 or 2505 => 60,
        2503 or 2491 or 2497 or 2507 => 70,
        1135 or 1099 or 1065 or 859 or 861 or 7370 or 7372 or 7378 or 7380 => 40,
        7374 or 7376 or 7382 or 7384 => 50,
        _ => GetRangedRequirementByName(items.GetItemName(itemId))
    };

    private static int GetRangedRequirementByName(string itemName)
    {
        if (itemName.Contains("3rd age range", StringComparison.OrdinalIgnoreCase) || itemName.Contains("3rd age vam", StringComparison.OrdinalIgnoreCase)) return 75;
        if (itemName == "Coif" || itemName.StartsWith("Studded", StringComparison.Ordinal)) return 20;
        if (itemName is "Karils coif" or "Karils leathertop" or "Karils leatherskirt" or "Karils crossbow") return 70;
        if (itemName is "Robin hood hat" or "Ranger boots" or "Seercull" or "Rune thrownaxe" or "Rune dart" or "Rune javelin" or "Rune knife" or "Rune arrow") return 40;
        if (itemName is "Crystal bow full" or "New crystal bow" or "Toktz-xil-ul" or "Bolt rack") return 70;
        if (itemName is "Adamant thrownaxe" or "Adamant dart" or "Adamant javelin" or "Adamant knife") return 30;
        return 0;
    }

    private int GetMagicRequirement(int itemId) => itemId switch
    {
        6914 or 6918 or 6920 or 6922 or 6924 => 60,
        1393 => 30,
        2415 or 2416 or 2417 => 60,
        4170 => 50,
        4675 => 60,
        3385 or 3387 or 3389 or 3391 or 3393 => 40,
        7399 or 7400 => 40,
        _ => items.GetItemName(itemId) switch
        {
            "Ahrims hood" or "Ahrims robetop" or "Ahrims robeskirt" or "Ahrims staff" or "Master wand" => 70,
            "Infinity hat" or "Infinity top" or "Infinity bottoms" or "Ancient staff" => 50,
            "Mystic hat" or "Mystic robe top" or "Mystic robe bottom" or "Mystic gloves" or "Mystic boots" => 40,
            _ => 0
        }
    };

    private int GetStrengthRequirement(int itemId) => itemId switch
    {
        1203 => 40,
        6528 or 6523 or 6525 or 6527 => 60,
        3122 => 50,
        11724 or 11726 or 11728 => 70,
        _ => 0
    };

    private int GetAttackRequirement(int itemId) => itemId switch
    {
        1215 or 1231 or 5680 or 5698 => 40,
        4151 or 4153 or 4587 or 1305 or 1434 => 70,
        4675 or 13406 => 50,
        4710 => 70,
        6523 or 6525 or 6527 => 60,
        7158 or 11694 or 11696 or 11698 or 11700 or 11730 => 75,
        3204 => 50,
        10887 or 11061 => 60,
        _ => GetAttackRequirementByName(items.GetItemName(itemId))
    };

    private static int GetAttackRequirementByName(string itemName)
    {
        if (itemName.Contains("dragon", StringComparison.OrdinalIgnoreCase) && itemName.Contains("dagger", StringComparison.OrdinalIgnoreCase)) return 60;
        if (itemName.Contains("godsword", StringComparison.OrdinalIgnoreCase)) return 75;
        if (itemName is "Abyssal whip" or "Granite maul" or "Saradomin sword" or "Staff of Light") return 70;
        // Add missing 3rd age validation like Java Equipment.java:545
        if (itemName.Contains("3rd age", StringComparison.OrdinalIgnoreCase)) return 75;
        return 0;
    }

    private int GetCraftingRequirement(int itemId) => itemId is 13614 or 13619 or 13624 ? 70 : 0;

    private int GetDefenceRequirement(int itemId) => itemId switch
    {
        3140 or 1127 => 40,
        2497 or 2495 or 2493 or 2491 or 2489 or 2487 => 40,
        10551 => 40,
        3122 => 50,
        4224 => 70,
        6615 or 6621 => 10,
        11724 or 11726 or 11728 => 70,
        _ => GetDefenceRequirementByName(items.GetItemName(itemId))
    };

    private static int GetDefenceRequirementByName(string itemName)
    {
        if (itemName.Contains("Initiate", StringComparison.OrdinalIgnoreCase)) return 20;
        if (itemName.Contains("rune", StringComparison.OrdinalIgnoreCase)) return 40;
        if (itemName.Contains("dragon", StringComparison.OrdinalIgnoreCase)) return 60;
        if (itemName.StartsWith("Barrows", StringComparison.OrdinalIgnoreCase) || itemName.StartsWith("Ahrims", StringComparison.OrdinalIgnoreCase) || itemName.StartsWith("Karils", StringComparison.OrdinalIgnoreCase) || itemName.StartsWith("Dharoks", StringComparison.OrdinalIgnoreCase) || itemName.StartsWith("Guthans", StringComparison.OrdinalIgnoreCase) || itemName.StartsWith("Torags", StringComparison.OrdinalIgnoreCase) || itemName.StartsWith("Veracs", StringComparison.OrdinalIgnoreCase)) return 70;
        return 0;
    }

    private int GetRunEmote(int id)
    {
        var weapon = items.GetItemName(id);
        if (id == 4718 || weapon.EndsWith("2h sword", StringComparison.Ordinal) || id == 6528 || weapon.EndsWith("godsword", StringComparison.Ordinal) || weapon.StartsWith("Anger", StringComparison.Ordinal) || weapon.Equals("Saradomin sword", StringComparison.Ordinal))
            return 7039;
        if (weapon is "Saradomin staff" or "Guthix staff" or "Zamorak staff") return 0x338;
        if (id == 12842) return 8961;
        if (id == 4755) return 1831;
        if (id == 11259) return 0x680;
        if (id == 4734) return 2077;
        if (id == 4726 || weapon.Contains("Spear", StringComparison.Ordinal) || weapon.EndsWith("halberd", StringComparison.Ordinal) || weapon.Contains("Staff", StringComparison.Ordinal) || weapon.Contains("staff", StringComparison.Ordinal)) return 1210;
        if (weapon.Equals("Abyssal whip", StringComparison.Ordinal)) return 1661;
        if (id == 4153) return 1664;
        return 0x338;
    }

    private int GetWalkEmote(int id)
    {
        var weapon = items.GetItemName(id);
        if (weapon is "Saradomin staff" or "Guthix staff" or "Zamorak staff") return 0x333;
        if (id == 4755) return 2060;
        if (id == 11259) return 0x67F;
        if (id == 4734) return 2076;
        if (id == 4153) return 1663;
        if (id == 12842) return 8961;
        if (weapon.Equals("Abyssal whip", StringComparison.Ordinal)) return 1660;
        if (id == 4718 || weapon.EndsWith("2h sword", StringComparison.Ordinal) || id == 6528 || weapon.EndsWith("godsword", StringComparison.Ordinal) || weapon.Equals("Saradomin sword", StringComparison.Ordinal)) return 7046;
        if (id == 4726 || weapon.Contains("spear", StringComparison.Ordinal) || weapon.EndsWith("halberd", StringComparison.Ordinal) || weapon.Contains("Staff", StringComparison.Ordinal) || weapon.Contains("staff", StringComparison.Ordinal)) return 1146;
        return 0x333;
    }

    private int GetStandEmote(int id)
    {
        var weapon = items.GetItemName(id);
        if (id == 4151) return 10080;
        if (weapon.EndsWith("2h sword", StringComparison.Ordinal) || weapon.EndsWith("godsword", StringComparison.Ordinal) || weapon.Equals("Saradomin sword", StringComparison.Ordinal)) return 7047;
        if (id == 4718) return 2065;
        if (id == 12842) return 8961;
        if (id == 11259) return 0x811;
        if (id == 4755) return 2061;
        if (id == 1337) return 2065;
        if (id == 4734) return 2074;
        if (id == 6528 || id == 1319) return 0x811;
        if (weapon is "Saradomin staff" or "Guthix staff" or "Zamorak staff") return 0x328;
        if (id == 4726 || weapon.EndsWith("spear", StringComparison.Ordinal) || weapon.EndsWith("halberd", StringComparison.Ordinal) || weapon.Contains("Staff", StringComparison.Ordinal) || weapon.Contains("staff", StringComparison.Ordinal) || id == 1305 || weapon.Equals("Staff of Light", StringComparison.Ordinal)) return 809;
        if (weapon.Equals("Abyssal whip", StringComparison.Ordinal)) return 1832;
        if (id == 4153) return 1662;
        return 0x328;
    }

    private int GetAttackEmote(int id)
    {
        var weapon = items.GetItemName(id);
        if (weapon.EndsWith("2h sword", StringComparison.Ordinal) || weapon.EndsWith("godsword", StringComparison.Ordinal) || weapon.StartsWith("Anger", StringComparison.Ordinal) || weapon.Equals("Saradomin sword", StringComparison.Ordinal)) return 7041;
        if (weapon.Equals("Abyssal whip", StringComparison.Ordinal)) return 1658;
        if (id == 4153) return 1665;
        if (id == 1231) return 2068;
        if (id == 4710 || weapon.Contains("staff", StringComparison.Ordinal) || weapon.Contains("Staff", StringComparison.Ordinal)) return 1665;
        if (id == 11235) return 426;
        if (id == 4718) return 2067;
        if (id == 4726) return 2082;
        if (id == 4734) return 2075;
        if (id == 3101) return 2068;
        if (id == 4747) return 2068;
        if (id == 4755) return 2062;
        if (id == 1337) return 2067;
        if (weapon.Contains("longsword", StringComparison.OrdinalIgnoreCase) || weapon.EndsWith("scimitar", StringComparison.Ordinal) || weapon.EndsWith("battleaxe", StringComparison.Ordinal)) return 451;
        if (weapon.EndsWith("shortbow", StringComparison.Ordinal) || weapon.EndsWith("bow full", StringComparison.Ordinal)) return 426;
        return 422;
    }

    private int GetAttackDelay(int id)
    {
        var weapon = items.GetItemName(id);
        if (weapon.EndsWith("2h sword", StringComparison.Ordinal) || weapon.EndsWith("godsword", StringComparison.Ordinal) || weapon.Equals("Saradomin sword", StringComparison.Ordinal) || weapon.Equals("Staff of Light", StringComparison.Ordinal)) return 5;
        if (id == 1203 || id == 3101) return 4;
        if (id == 1337) return 5;
        if (weapon.EndsWith("battleaxe", StringComparison.Ordinal)) return 4;
        if (weapon.EndsWith("longsword", StringComparison.Ordinal)) return 4;
        if (weapon.Equals("Abyssal whip", StringComparison.Ordinal) || weapon.EndsWith("scimitar", StringComparison.Ordinal) || weapon.EndsWith("dagger", StringComparison.Ordinal) || weapon.StartsWith("Anger", StringComparison.Ordinal)) return 4;
        return 5;
    }

    private static bool ContainsAny(string value, IEnumerable<string> needles) => needles.Any(value.Contains);

    private static bool StartsOrEndsWithAny(string value, IEnumerable<string> needles) =>
        needles.Any(needle => value.StartsWith(needle, StringComparison.Ordinal) || value.EndsWith(needle, StringComparison.Ordinal));

    private static bool EndsWithAny(string value, IEnumerable<string> needles) =>
        needles.Any(needle => value.EndsWith(needle, StringComparison.Ordinal));

    private static void Write(Player player, Action<FrameWriter> build)
    {
        var session = player.Session;
        if (session is null)
            return;

        using var w = new FrameWriter(4096);
        build(w);
        w.FlushToAsync(session.GetStream(), session.CancellationToken).GetAwaiter().GetResult();
    }
}
