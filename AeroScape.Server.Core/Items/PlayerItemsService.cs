using AeroScape.Server.Core.Entities;

namespace AeroScape.Server.Core.Items;

public sealed class PlayerItemsService(ItemDefinitionLoader items)
{
    private bool IsInventoryStackType(int itemId) => items.IsStackable(itemId) || items.IsNoted(itemId);

    public bool HasItemAmount(Player player, int itemId, int amount) => InvItemCount(player, itemId) >= amount;

    public bool HaveItem(Player player, int itemId, int amount = 1) => HoldItem(player, itemId, amount);

    public bool HoldItem(Player player, int itemId, int amount)
    {
        if (IsInventoryStackType(itemId))
        {
            var slot = GetItemSlot(player, itemId);
            return slot >= 0 && player.ItemsN[slot] >= amount;
        }

        var found = 0;
        for (var i = 0; i < player.Items.Length; i++)
        {
            if (player.Items[i] != itemId)
            {
                continue;
            }

            if (player.ItemsN[i] >= amount)
            {
                return true;
            }

            found++;
        }

        return found >= amount;
    }

    public bool AddItem(Player player, int itemId, int itemAmount)
    {
        if (player is null || itemId < 0 || itemAmount <= 0)
        {
            return false;
        }

        if (!IsInventoryStackType(itemId))
        {
            while (itemAmount > 0)
            {
                var slot = FindInvSlot(player);
                if (slot < 0)
                {
                    return false;
                }

                player.Items[slot] = itemId;
                player.ItemsN[slot] = 1;
                itemAmount--;
            }

            return true;
        }

        var existingSlot = GetItemSlot(player, itemId);
        if (existingSlot >= 0)
        {
            player.ItemsN[existingSlot] = Math.Min(ItemDefinitionLoader.MaxItemAmount, player.ItemsN[existingSlot] + itemAmount);
            return true;
        }

        var freeSlot = FindInvSlot(player);
        if (freeSlot < 0)
        {
            return false;
        }

        player.Items[freeSlot] = itemId;
        player.ItemsN[freeSlot] = itemAmount;
        return true;
    }

    public bool DeleteItem(Player player, int itemId, int amount) => DeleteItem(player, itemId, GetItemSlot(player, itemId), amount);

    public bool DeleteItem(Player player, int itemId, int index, int amount)
    {
        if (player is null || itemId < 0 || amount <= 0 || index < 0 || index >= player.Items.Length)
        {
            return false;
        }

        if (player.Items[index] != itemId)
        {
            index = GetItemSlot(player, itemId);
            if (index < 0)
            {
                return false;
            }
        }

        if (!IsInventoryStackType(itemId))
        {
            for (var i = 0; i < player.Items.Length && amount > 0; i++)
            {
                if (player.Items[i] != itemId)
                {
                    continue;
                }

                player.Items[i] = -1;
                player.ItemsN[i] = 0;
                amount--;
            }

            return amount == 0;
        }

        if (player.ItemsN[index] > amount)
        {
            player.ItemsN[index] -= amount;
            return true;
        }

        player.Items[index] = -1;
        player.ItemsN[index] = 0;
        return true;
    }

    public int GetItemSlot(Player player, int itemId)
    {
        for (var i = 0; i < player.Items.Length; i++)
        {
            if (player.Items[i] == itemId)
            {
                return i;
            }
        }

        return -1;
    }

    public int FindInvSlot(Player player)
    {
        for (var i = 0; i < player.Items.Length; i++)
        {
            if (player.Items[i] == -1)
            {
                return i;
            }
        }

        return -1;
    }

    public int FreeSlotCount(Player player)
    {
        var count = 0;
        for (var i = 0; i < player.Items.Length; i++)
        {
            if (player.Items[i] == -1)
            {
                count++;
            }
        }

        return count;
    }

    public int InvItemCount(Player player, int itemId)
    {
        var amount = 0;
        for (var i = 0; i < player.Items.Length; i++)
        {
            if (player.Items[i] != itemId)
            {
                continue;
            }

            amount += IsInventoryStackType(itemId) ? player.ItemsN[i] : 1;
        }

        return amount;
    }

    public void SwapInventoryItems(Player player, int fromSlot, int toSlot)
    {
        if (fromSlot < 0 || fromSlot >= player.Items.Length || toSlot < 0 || toSlot >= player.Items.Length)
        {
            return;
        }

        (player.Items[fromSlot], player.Items[toSlot]) = (player.Items[toSlot], player.Items[fromSlot]);
        (player.ItemsN[fromSlot], player.ItemsN[toSlot]) = (player.ItemsN[toSlot], player.ItemsN[fromSlot]);
    }

    public bool TransferItem(Player fromPlayer, Player toPlayer, int itemId, int amount)
    {
        if (amount <= 0 || !HaveItem(fromPlayer, itemId, amount))
        {
            return false;
        }

        if (!AddItem(toPlayer, itemId, amount))
        {
            return false;
        }

        return DeleteItem(fromPlayer, itemId, amount);
    }
}
