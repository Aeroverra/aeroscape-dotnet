using System;
using AeroScape.Server.Core.Entities;

namespace AeroScape.Server.Core.Services;

public sealed class InventoryService
{
    public int Count(Player player, int itemId)
    {
        int count = 0;
        for (int i = 0; i < player.Items.Length; i++)
        {
            if (player.Items[i] == itemId)
                count += Math.Max(player.ItemsN[i], 1);
        }

        return count;
    }

    public int FindItemSlot(Player player, int itemId)
    {
        for (int i = 0; i < player.Items.Length; i++)
        {
            if (player.Items[i] == itemId)
                return i;
        }

        return -1;
    }

    public int FindFreeSlot(Player player)
    {
        for (int i = 0; i < player.Items.Length; i++)
        {
            if (player.Items[i] < 0)
                return i;
        }

        return -1;
    }

    public bool AddItem(Player player, int itemId, int amount = 1)
    {
        if (amount <= 0)
            return false;

        int existing = FindItemSlot(player, itemId);
        if (existing >= 0)
        {
            player.ItemsN[existing] += amount;
            return true;
        }

        for (int remaining = amount; remaining > 0; remaining--)
        {
            int slot = FindFreeSlot(player);
            if (slot < 0)
                return false;

            player.Items[slot] = itemId;
            player.ItemsN[slot] = 1;
        }

        return true;
    }

    public bool DeleteItem(Player player, int itemId, int amount = 1)
    {
        if (amount <= 0)
            return false;

        int remaining = amount;
        for (int i = 0; i < player.Items.Length && remaining > 0; i++)
        {
            if (player.Items[i] != itemId)
                continue;

            int stack = Math.Max(player.ItemsN[i], 1);
            int remove = Math.Min(stack, remaining);
            stack -= remove;
            remaining -= remove;

            if (stack <= 0)
            {
                player.Items[i] = -1;
                player.ItemsN[i] = 0;
            }
            else
            {
                player.ItemsN[i] = stack;
            }
        }

        return remaining == 0;
    }
}
