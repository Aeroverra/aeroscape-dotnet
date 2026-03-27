using AeroScape.Server.Core.Combat;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Frames;
using AeroScape.Server.Network.Frames;

namespace AeroScape.Server.Core.Items;

public sealed class GroundItemManager(ItemDefinitionLoader itemDefinitions, GameFrames frames, Engine engine)
{
    private const int MaxGroundItems = 1000;
    private readonly GroundItemState?[] _groundItems = new GroundItemState?[MaxGroundItems];

    public IReadOnlyList<GroundItemState?> GroundItems => _groundItems;

    public void Process()
    {
        for (var i = 0; i < _groundItems.Length; i++)
        {
            var groundItem = _groundItems[i];
            if (groundItem is null)
            {
                continue;
            }

            if (groundItem.ItemId < 0 || groundItem.ItemAmt <= 0)
            {
                _groundItems[i] = null;
                continue;
            }

            groundItem.ItemGroundTime--;
            if (groundItem.ItemGroundTime == 60)
            {
                if (!itemDefinitions.IsUntradable(groundItem.ItemId) && !string.IsNullOrEmpty(groundItem.ItemDroppedBy))
                {
                    groundItem.IsGlobal = true;
                    // Send createGlobalItem frame notifications like Java Items.java:71-85
                    NotifyPlayersInRange(groundItem, true);
                }
            }

            if (groundItem.ItemGroundTime <= 0)
            {
                // Send removeGroundItem frame notifications like Java Items.java:71-85
                NotifyPlayersInRange(groundItem, false);
                _groundItems[i] = null;
            }
        }
    }

    public bool CreateGroundItem(int itemId, int amount, int x, int y, int height, string owner = "")
    {
        if (itemId < 0 || amount <= 0)
        {
            return false;
        }

        if ((itemId >= 9747 && itemId <= 9814) || (itemId >= 9848 && itemId <= 9950) || (itemId >= 12169 && itemId <= 12171))
        {
            return false;
        }

        for (var i = 0; i < _groundItems.Length; i++)
        {
            if (_groundItems[i] is not null)
            {
                continue;
            }

            _groundItems[i] = new GroundItemState(i, itemId, amount, x, y, height, owner);
            _groundItems[i]!.IsGlobal = string.IsNullOrEmpty(owner);
            return true;
        }

        return false;
    }

    public int ItemExists(int itemId, int itemX, int itemY, int height)
    {
        for (var i = 0; i < _groundItems.Length; i++)
        {
            var groundItem = _groundItems[i];
            if (groundItem is null)
            {
                continue;
            }

            if (groundItem.ItemId == itemId && groundItem.ItemX == itemX && groundItem.ItemY == itemY && groundItem.ItemHeight == height)
            {
                return i;
            }
        }

        return -1;
    }

    public GroundItemState? GetPickupCandidate(Player player, int itemId, int itemX, int itemY)
    {
        if (CombatFormulas.GetDistance(player.AbsX, player.AbsY, itemX, itemY) > 0)
        {
            return null;
        }

        var index = ItemExists(itemId, itemX, itemY, player.HeightLevel);
        if (index < 0)
        {
            return null;
        }

        var groundItem = _groundItems[index];
        if (groundItem is null)
        {
            return null;
        }

        if (!groundItem.CanBeSeenBy(player.Username, itemDefinitions.IsUntradable(groundItem.ItemId)))
        {
            return null;
        }

        return groundItem;
    }

    public void Remove(int index)
    {
        if (index >= 0 && index < _groundItems.Length)
        {
            _groundItems[index] = null;
        }
    }
}

public sealed class GroundItemState(int index, int itemId, int itemAmt, int itemX, int itemY, int itemHeight, string itemDroppedBy)
{
    public int Index { get; } = index;
    public int ItemId { get; } = itemId;
    public int ItemAmt { get; } = itemAmt;
    public int ItemX { get; } = itemX;
    public int ItemY { get; } = itemY;
    public int ItemHeight { get; } = itemHeight;
    public string ItemDroppedBy { get; } = itemDroppedBy;
    public int ItemGroundTime { get; set; } = 240;
    public bool IsGlobal { get; set; }

    public bool CanBeSeenBy(string username, bool isUntradable)
    {
        if (string.IsNullOrEmpty(ItemDroppedBy))
        {
            return true;
        }

        if (isUntradable)
        {
            return ItemDroppedBy == username;
        }

        return IsGlobal || ItemDroppedBy == username;
    }

    private void NotifyPlayersInRange(GroundItemState groundItem, bool isCreate)
    {
        // Notify all players in range like Java Items.java:71-85
        foreach (var player in engine.Players)
        {
            if (player is null || !player.IsLoggedIn)
                continue;

            // Check if player is in range (same logic as visibility checks)
            int deltaX = Math.Abs(player.AbsX - groundItem.X);
            int deltaY = Math.Abs(player.AbsY - groundItem.Y);
            if (deltaX <= 12 && deltaY <= 12 && player.HeightLevel == groundItem.Height)
            {
                if (isCreate)
                {
                    Write(player, w => frames.CreateGroundItem(w, player, groundItem.ItemId, groundItem.Amount, 
                        groundItem.X, groundItem.Y, groundItem.Height));
                }
                else
                {
                    Write(player, w => frames.RemoveGroundItem(w, player, groundItem.ItemId, 
                        groundItem.X, groundItem.Y, groundItem.Height));
                }
            }
        }
    }

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
