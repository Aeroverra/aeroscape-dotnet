using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AeroScape.Server.Core.Entities;

namespace AeroScape.Server.Core.Services;

public sealed class ConstructionService
{
    private readonly IClientUiService _ui;

    public ConstructionService(IClientUiService ui)
    {
        _ui = ui;
    }

    public static readonly int[,] RoomInfo =
    {
        {1864, 5056, 0, 0},
        {1856, 5112, 1000, 1},
        {1856, 5064, 1000, 1},
        {1872, 5112, 5000, 5},
        {1890, 5112, 5000, 10},
        {1856, 5096, 10000, 15},
        {1904, 5112, 10000, 20},
        {1880, 5104, 15000, 25},
        {1896, 5088, 25000, 30},
        {1880, 5088, 25000, 32},
        {1912, 5104, 25000, 35},
        {1888, 5096, 50000, 40},
        {1904, 5064, 50000, 42},
        {1872, 5096, 50000, 45},
        {1864, 5088, 100000, 50},
        {1872, 5064, 75000, 55},
        {1904, 5096, 150000, 60},
        {1904, 5080, 150000, 65},
        {1888, 5080, 7500, 70},
        {1856, 5080, 7500, 70},
        {1872, 5080, 7500, 70},
        {1912, 5088, 250000, 75},
    };

    private readonly ConcurrentDictionary<int, HouseState> _houses = new();

    public bool HaveWateringCan(Player player)
        => Enumerable.Range(5333, 8).Any(can => CountItem(player, can) > 0);

    public void DecreaseCan(Player player)
    {
        for (var can = 5333; can <= 5340; can++)
        {
            if (CountItem(player, can) <= 0)
                continue;

            DeleteItem(player, can, 1);
            if (can > 5333)
                AddItem(player, can - 1, 1);
            return;
        }
    }

    public bool AddRoom(Player player, int roomId)
    {
        EnsureLoaded(player);
        if (roomId < 0 || roomId + 1 > RoomInfo.GetLength(0))
            return false;

        var requiredLevel = RoomInfo[roomId + 1, 3];
        var price = RoomInfo[roomId + 1, 2];
        if (player.SkillLvl[22] < requiredLevel || CountItem(player, 995) < price)
            return false;

        var house = _houses.GetOrAdd(player.PersistentId, _ => new HouseState());
        if (!TryResolveNextRoom(player, out var roomX, out var roomY))
            return false;

        house.Rooms[(roomX, roomY)] = roomId + 1;
        Save(player, house);
        DeleteItem(player, 995, price);
        return true;
    }

    public bool AddFurniture(Player player, int level, int[] items, int[] amounts, int spot, int objectId, bool needCan)
    {
        EnsureLoaded(player);
        if (player.SkillLvl[22] < level)
            return false;

        for (var i = 0; i < items.Length; i++)
        {
            if (items[i] != 0 && CountItem(player, items[i]) < amounts[i])
                return false;
        }

        if (needCan && !HaveWateringCan(player))
            return false;

        for (var i = 0; i < items.Length; i++)
        {
            if (items[i] != 0)
                DeleteItem(player, items[i], amounts[i]);
        }

        if (needCan)
            DecreaseCan(player);

        var house = _houses.GetOrAdd(player.PersistentId, _ => new HouseState());
        var roomCoords = GetRoomCoords(player);
        house.Furniture[(roomCoords.RoomX, roomCoords.RoomY, spot)] = objectId;
        Save(player, house);
        return true;
    }

    public void RemoveFurniture(Player player, int spot)
    {
        if (_houses.TryGetValue(player.PersistentId, out var house))
        {
            var roomCoords = GetRoomCoords(player);
            house.Furniture.Remove((roomCoords.RoomX, roomCoords.RoomY, spot));
            Save(player, house);
        }
    }

    public void HandleBuildClick(Player player, int x, int y, int objectId)
    {
        player.LastObjectX = x;
        player.LastObjectY = y;
        player.ConstInterface = objectId;

        switch (objectId)
        {
            case 15314:
                _ui.ShowInterface(player, 402);
                break;
            case 15307:
            case 15308:
                SetNextRoom(player, x, y);
                _ui.ShowInterface(player, 402);
                break;
            case 15361:
                _ui.ShowInterface(player, 396);
                break;
            case 15364:
            case 15365:
            case 15366:
            case 15367:
                _ui.ShowInterface(player, 394);
                break;
            case 13431:
            case 13432:
            case 13433:
                RemoveFurniture(player, 1);
                break;
            case 13434:
            case 13435:
            case 13436:
                RemoveFurniture(player, 2);
                break;
            case 13425:
            case 13426:
            case 13427:
                RemoveFurniture(player, 3);
                break;
            case 13428:
            case 13429:
            case 13430:
                RemoveFurniture(player, 4);
                break;
        }
    }

    private static int CountItem(Player player, int itemId)
    {
        var count = 0;
        for (var i = 0; i < player.Items.Length; i++)
        {
            if (player.Items[i] != itemId)
                continue;

            count += player.ItemsN[i] > 0 ? player.ItemsN[i] : 1;
        }

        return count;
    }

    private static void DeleteItem(Player player, int itemId, int amount)
    {
        for (var i = 0; i < player.Items.Length && amount > 0; i++)
        {
            if (player.Items[i] != itemId)
                continue;

            var stack = player.ItemsN[i] > 0 ? player.ItemsN[i] : 1;
            if (stack > amount)
            {
                player.ItemsN[i] = stack - amount;
                return;
            }

            amount -= stack;
            player.Items[i] = -1;
            player.ItemsN[i] = 0;
        }
    }

    private static bool AddItem(Player player, int itemId, int amount)
    {
        for (var a = 0; a < amount; a++)
        {
            for (var i = 0; i < player.Items.Length; i++)
            {
                if (player.Items[i] != -1)
                    continue;

                player.Items[i] = itemId;
                player.ItemsN[i] = 1;
                goto next;
            }

            return false;
        next: ;
        }

        return true;
    }

    private sealed class HouseState
    {
        public Dictionary<(int X, int Y), int> Rooms { get; } = [];
        public Dictionary<(int X, int Y, int Spot), int> Furniture { get; } = [];
    }

    private void EnsureLoaded(Player player)
    {
        if (_houses.ContainsKey(player.PersistentId))
            return;

        var house = new HouseState();
        if (!string.IsNullOrWhiteSpace(player.ConstructionRoomsData))
        {
            foreach (var part in player.ConstructionRoomsData.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var pieces = part.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (pieces.Length == 3 &&
                    int.TryParse(pieces[0], out var x) &&
                    int.TryParse(pieces[1], out var y) &&
                    int.TryParse(pieces[2], out var room))
                {
                    house.Rooms[(x, y)] = room;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(player.ConstructionFurnitureData))
        {
            foreach (var part in player.ConstructionFurnitureData.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var pieces = part.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (pieces.Length == 4 &&
                    int.TryParse(pieces[0], out var x) &&
                    int.TryParse(pieces[1], out var y) &&
                    int.TryParse(pieces[2], out var spot) &&
                    int.TryParse(pieces[3], out var objectId))
                {
                    house.Furniture[(x, y, spot)] = objectId;
                }
            }
        }

        _houses[player.PersistentId] = house;
    }

    private static void Save(Player player, HouseState house)
    {
        player.ConstructionRoomsData = string.Join(',', house.Rooms.Select(pair => $"{pair.Key.X}:{pair.Key.Y}:{pair.Value}"));
        player.ConstructionFurnitureData = string.Join(',', house.Furniture.Select(pair => $"{pair.Key.X}:{pair.Key.Y}:{pair.Key.Spot}:{pair.Value}"));
    }

    private static (int RoomX, int RoomY) GetRoomCoords(Player player)
    {
        int roomX = (int)System.Math.Floor((player.LastObjectX - 8 * (player.MapRegionX - 6)) / 8.0);
        int roomY = (int)System.Math.Floor((player.LastObjectY - 8 * (player.MapRegionY - 6)) / 8.0);
        return (roomX, roomY);
    }

    private static void SetNextRoom(Player player, int x, int y)
    {
        int objArrayX = (int)System.Math.Floor((x - 8 * (player.MapRegionX - 6)) / 8.0);
        int objArrayY = (int)System.Math.Floor((y - 8 * (player.MapRegionY - 6)) / 8.0);
        int objLocalX = x - 8 * (player.MapRegionX - 6);
        int objLocalY = y - 8 * (player.MapRegionY - 6);
        int squareX = objLocalX - (8 * objArrayX);
        int squareY = objLocalY - (8 * objArrayY);

        if ((squareX == 0 && squareY is 3 or 4))
            player.NextRoom[0] = 4;
        if ((squareX == 7 && squareY is 3 or 4))
            player.NextRoom[0] = 2;
        if ((squareY == 0 && squareX is 3 or 4))
            player.NextRoom[0] = 3;
        if ((squareY == 7 && squareX is 3 or 4))
            player.NextRoom[0] = 1;

        player.NextRoom[1] = objArrayX;
        player.NextRoom[2] = objArrayY;
    }

    private static bool TryResolveNextRoom(Player player, out int roomX, out int roomY)
    {
        roomX = player.NextRoom[1];
        roomY = player.NextRoom[2];

        switch (player.NextRoom[0])
        {
            case 1:
                roomY++;
                return true;
            case 2:
                roomX++;
                return true;
            case 3:
                roomY--;
                return true;
            case 4:
                roomX--;
                return true;
            default:
                return false;
        }
    }
}
