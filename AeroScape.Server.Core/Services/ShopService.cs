using System;
using System.Collections.Generic;
using AeroScape.Server.Core.Engine;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Items;

namespace AeroScape.Server.Core.Services;

public sealed record ShopStock(int[] Items, int[] DefaultAmounts, int[] Prices);

public sealed class ShopService
{
    private readonly InventoryService _inventory;
    private readonly ItemDefinitionLoader _items;
    private readonly IClientUiService _ui;
    private long _lastRestockTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    private int _counter;

    public IReadOnlyDictionary<int, ShopStock> Definitions => _definitions;
    private readonly Dictionary<int, ShopStock> _definitions = new()
    {
        [1] = new(CreateEmptyItems(40), Fill(40, 0), Fill(40, 0)),
        [2] = new(new[] { 590, 946, 1359, 1275, 12844, 301, 305, 307, 311, 11259 }, Fill(10, 100), new[] { 1, 1, 1, 1, 60, 1, 1, 1, 1, 1 }),
        [3] = new(new[] { 1135, 1099, 1065, 2499, 2493, 2487, 2501, 2495, 2489, 2503, 2497, 2491, 10382, 10378, 10380, 10376, 10390, 10386, 10388, 10384, 10374, 10370, 10372, 10368, 2581, 2577, -1 }, Fill(27, 100), new[] { 100, 100, 100, 205, 205, 205, 400, 400, 400, 605, 605, 605, 800, 800, 800, 800, 300, 800, 800, 800, 800, 800, 800, 800, 905, 905, 0 }),
        [4] = new(new[] { 11335, 11283, 11732, 3140, 4087, 1187, 4151, 391, 1305, 4587, 5698, 10828, 1149, 8850, 121, 113, 11758, 4675 }, Fill(18, 100), new[] { 15000, 34500, 3500, 50000, 25000, 13500, 16000, 500, 950, 1000, 1000, 3500, 5000, 2500, 350, 350, 5000, 12500 }),
        [5] = new(new[] { 1321, 1323, 1325, 1327, 1329, 1331, 1333, 1117, 1115, 1119, 1121, 1125, 1123, 1127, 1075, 1067, 1069, 1077, 1071, 1073, 1079, 1155, 1153, 1157, 1165, 1159, 1161, 1163, 1191, 1193, 1195, 1197, 1199, 1201 }, Fill(34, 100), new[] { 30, 50, 70, 100, 230, 350, 500, 30, 50, 70, 100, 230, 350, 500, 20, 30, 50, 80, 150, 300, 400, 10, 30, 50, 70, 130, 200, 300, 20, 40, 60, 120, 200, 300 }),
        [6] = new(new[] { 1052, 6585, 775, 1837, 2643, 3061, 10075, 10065, 10067, 10069, 10071, 6568, 1704, 10402, 10406, 10748, 10750, 13192, 13190 }, Fill(19, 100), new[] { 100, 1750, 50, 50, 500, 50, 50, 50, 50, 50, 50, 500, 250, 3250, 3500, 3250, 3500, 4000, 4000 }),
        [7] = new(new[] { 1755, 1623, 1621, 1619, 1617, 1631, 6571, 1733, 1734, 1746, 2506, 2508, 2510, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, new[] { 1, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 }, new[] { 1, 2, 3, 4, 5, 10, 15, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }),
        [8] = new(new[] { 4155, 13263, 13290, 7159, 4156, 4158, 4170, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, Fill(27, 100), new[] { 10, 4500, 1500, 50, 400, 400, 1900, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }),
        [9] = new(new[] { 1038, 1040, 1042, 1044, 1046, 1048, 1050, 1057, 1055, 1053, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }, Fill(27, 100), new[] { 9900, 9900, 9900, 9900, 9900, 9900, 9900, 9900, 9900, 9900, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }),
        [10] = new(new[] { 4708, 4712, 4714, 4710, 4716, 4720, 4722, 4718, 4724, 4728, 4730, 4726, 4730, 4734, 4736, 4732, 4745, 4749, 4751, 4747, 4753, 4757, 4759, 4755, 4740, 7462, 7461 }, Fill(27, 100), new[] { 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 2000, 20, 750, 400 }),
        [11] = new(new[] { 12047, 12043, 12059, 12808, 12073, 12075, 12077, 12079, 12081, 12083, 12802, 12804, 12806, 12776, 12788, 12786, 12796, 12822, 12790, 12469, 12471, 12473, 12475, 12210, 12216, 12219, 12222 }, Fill(27, 100), new[] { 10, 20, 20, 30, 40, 40, 40, 50, 50, 70, 70, 80, 80, 90, 110, 120, 120, 120, 150, 7000, 7000, 7000, 700, 100, 250, 500, 1000 }),
        [12] = new(new[] { 13614, 13615, 13616, 13617, 13618, 13619, 13620, 13621, 13622, 13623, 13624, 13625, 13626, 13627, 13629, 13630, 13631, 13632, 13633, 13634, 13635, 13636, 13637, 13638, 13639, 13640, 13642 }, Fill(27, 100), new[] { 500, 500, 500, 500, 500, 1500, 1500, 1500, 1500, 1500, 2500, 2500, 2500, 2500, 2500, 500, 500, 500, 500, 500, 500, 500, 500, 500, 500, 500, 500 }),
        [13] = new(new[] { 7806, 7807, 7808, 7809, 6106, 6107, 6108, 6109, 6110, 6111, 4345, 6856, 6857, 6858, 6859, 6860, 6861, 6862, 6863, 8942, 3101, 1361, 1231, 1337, 4353, 1203, 4331, -1, -1, -1, -1, -1, -1, -1 }, new[] { 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 }, new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 10, 5000, 5000, 5000, 5000, 5000, 5000, 5000 }),
        [14] = new(new[] { 4566, 5553, 5554, 5555, 5556, 5557, 2653, 2655, 2657, 2659, 2661, 2663, 2665, 2667, 2669, 2671, 2673, 2675, 3481, 3483, 3486, 3488, 12222, 534, 3101, 1337, 1361 }, new[] { 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 1, 5, 5 }, new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1000, 1000, 5000 }),
        [16] = new(new[] { 8798, 8799, 8800, 8802, 8803 }, Fill(5, 10), Fill(5, 5000)),
        [17] = new(CreateEmptyItems(40), Fill(40, 0), Fill(40, 0)),
        [18] = new(new[] { 4089, 4091, 4093, 4095, 4097, 4099, 4101, 4103, 4105, 4107, 4109, 4111, 4113, 4115, 4117, 6918, 6916, 6920, 6922, 6924, 3840, 3842, 3844, 6889, 4675, 6908, 6910, 6912, 6914 }, Fill(29, 100), Fill(29, 100)),
    };

    private readonly Dictionary<int, int[]> _runtimeAmounts = new();
    private readonly Dictionary<int, int[]> _runtimeItems = new();

    public ShopService(InventoryService inventory, ItemDefinitionLoader items, IClientUiService ui)
    {
        _inventory = inventory;
        _items = items;
        _ui = ui;
        foreach (var pair in _definitions)
        {
            _runtimeAmounts[pair.Key] = (int[])pair.Value.DefaultAmounts.Clone();
            _runtimeItems[pair.Key] = (int[])pair.Value.Items.Clone();
        }
    }

    public void Process(GameEngine engine)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (now - _lastRestockTicks < 10_000)
            return;

        foreach (var pair in _definitions)
        {
            if (pair.Key is 1 or 17)
                continue;

            var stock = pair.Value;
            var amounts = _runtimeAmounts[pair.Key];
            for (int i = 0; i < amounts.Length && i < stock.DefaultAmounts.Length; i++)
            {
                if (amounts[i] < stock.DefaultAmounts[i])
                    amounts[i]++;
            }
        }

        _counter++;
        if (_counter > 15)
        {
            RestockDynamicShop(1);
            RestockDynamicShop(17);
            _counter = 0;
        }
        _lastRestockTicks = now;
    }

    public bool OpenShop(Player player, int shopId)
    {
        if (!_definitions.TryGetValue(shopId, out var stock))
            return false;

        player.PartyShop = shopId == 17;
        player.ShopId = shopId;
        player.ShopItems = _runtimeItems[shopId];
        player.ShopItemsN = _runtimeAmounts[shopId];
        _ui.OpenShop(player, shopId switch
        {
            1 => "General Store",
            17 => "Party Room",
            _ => "General Store"
        });
        return true;
    }

    public bool Buy(Player player, int itemId, int amount)
    {
        if (!_definitions.TryGetValue(player.ShopId, out var stock))
            return false;

        // Add null checks and array length validation
        if (player.ShopItems == null || player.ShopItemsN == null || 
            player.ShopItems.Length != player.ShopItemsN.Length)
            return false;

        int slot = Array.IndexOf(player.ShopItems, itemId);
        if (slot < 0 || slot >= player.ShopItemsN.Length)
            return false;

        amount = Math.Min(amount, player.ShopItemsN[slot]);
        if (amount <= 0)
            return false;

        int price = GetPrice(player.ShopId, slot);
        int coins = _inventory.Count(player, 995);
        if (coins < price * amount)
            return false;

        if (!_inventory.DeleteItem(player, 995, price * amount))
            return false;

        if (!_inventory.AddItem(player, itemId, amount))
        {
            _inventory.AddItem(player, 995, price * amount);
            return false;
        }

        player.ShopItemsN[slot] -= amount;
        if (player.ShopId is 1 or 17 && player.ShopItemsN[slot] <= 0)
        {
            player.ShopItems[slot] = -1;
            player.ShopItemsN[slot] = 0;
        }

        _ui.RefreshShop(player);
        return true;
    }

    public bool Sell(Player player, int itemId, int amount)
    {
        // Allow coins to be sold to general stores (shops 1 and 17) like Java
        if (itemId == 995 && !player.PartyShop && player.ShopId != 1 && player.ShopId != 17)
            return false;

        if (_inventory.Count(player, itemId) < amount)
            return false;

        int slot = Array.IndexOf(player.ShopItems, itemId);
        bool shopShouldBuy = slot >= 0 || player.ShopId is 1 or 17;
        if (!shopShouldBuy)
            return false;

        if (slot < 0)
        {
            slot = FindFreeSlot(player.ShopItems);
            if (slot < 0)
                return false;

            player.ShopItems[slot] = itemId;
            player.ShopItemsN[slot] = 0;
        }

        int price = Math.Max(1, (int)(GetPrice(player.ShopId, slot) * 0.75));
        if (!_inventory.DeleteItem(player, itemId, amount))
            return false;

        player.ShopItemsN[slot] += amount;
        if (!player.PartyShop)
            _inventory.AddItem(player, 995, price * amount);

        _ui.RefreshShop(player);
        return true;
    }

    public int GetPrice(int shopId, int slot)
    {
        if (!_definitions.TryGetValue(shopId, out var stock))
            return 1;
        if (slot < 0)
            return 1;
        if (shopId is 1 or 17)
        {
            var itemId = _runtimeItems[shopId][slot];
            return Math.Max(_items.Get(itemId)?.ShopValue ?? 1, 1);
        }
        if (slot >= stock.Prices.Length)
            return 1;
        return Math.Max(stock.Prices[slot], 1);
    }

    private static int[] Fill(int count, int value)
    {
        var result = new int[count];
        Array.Fill(result, value);
        return result;
    }

    private static int[] CreateEmptyItems(int count)
    {
        var result = new int[count];
        Array.Fill(result, -1);
        return result;
    }

    private void RestockDynamicShop(int shopId)
    {
        var items = _runtimeItems[shopId];
        var amounts = _runtimeAmounts[shopId];
        for (int i = 0; i < amounts.Length; i++)
        {
            if (amounts[i] <= 0)
            {
                items[i] = -1;
                amounts[i] = 0;
                continue;
            }

            amounts[i]--;
            if (amounts[i] <= 0)
            {
                items[i] = -1;
                amounts[i] = 0;
            }
        }
    }

    private static int FindFreeSlot(int[] items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == -1)
                return i;
        }

        return -1;
    }
}
