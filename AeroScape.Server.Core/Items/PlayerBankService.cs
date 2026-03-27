using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Frames;
using AeroScape.Server.Network.Frames;

namespace AeroScape.Server.Core.Items;

public sealed class PlayerBankService(ItemDefinitionLoader itemDefinitions, PlayerItemsService playerItems, GameFrames frames)
{
    public const int Size = Player.BankSize;

    public void Deposit(Player player, int inventorySlot, int amount)
    {
        if (inventorySlot < 0 || inventorySlot >= player.Items.Length || player.Items[inventorySlot] == -1)
        {
            return;
        }

        var itemId = player.Items[inventorySlot];
        var inventoryCount = playerItems.InvItemCount(player, itemId);
        amount = Math.Min(amount, inventoryCount);
        if (amount <= 0)
        {
            return;
        }

        if (itemDefinitions.IsNoted(itemId))
        {
            var unnotedId = itemDefinitions.FindUnnote(itemId);
            if (unnotedId < 0)
            {
                return;
            }

            itemId = unnotedId;
        }

        var bankCount = GetBankItemCount(player, itemId);
        var freeBankSlot = player.ViewingBankTab == 10
            ? GetFreeBankSlot(player)
            : player.TabStartSlot[player.ViewingBankTab] + GetItemsInTab(player, player.ViewingBankTab);

        if (player.ViewingBankTab != 10)
        {
            Insert(player, GetFreeBankSlot(player), freeBankSlot);
            IncreaseTabStartSlots(player, player.ViewingBankTab);
            // Removed automatic switch to main tab - should stay on current tab (Java PlayerBank.java:36-37)
            SendTabConfig(player);
        }

        if (bankCount + amount < 0)
        {
            amount = ItemDefinitionLoader.MaxItemAmount - bankCount;
            player.LastTickMessage = "Your bank is full";
        }

        if (bankCount == 0 && freeBankSlot == -1)
        {
            player.LastTickMessage = "Not enough space in your bank.";
            return;
        }

        if (bankCount > 0)
        {
            var bankSlot = GetBankItemSlot(player, itemId);
            player.BankItemsN[bankSlot] += amount;
        }
        else
        {
            player.BankItems[freeBankSlot] = itemId;
            player.BankItemsN[freeBankSlot] = amount;
        }

        playerItems.DeleteItem(player, player.Items[inventorySlot], inventorySlot, amount);
        RefreshBankUi(player);
    }

    public void Withdraw(Player player, int bankSlot, int amount)
    {
        if (bankSlot < 0 || bankSlot >= Size || player.BankItems[bankSlot] == -1)
        {
            return;
        }

        var itemId = player.BankItems[bankSlot];
        var bankItemCount = GetBankItemCount(player, itemId);
        amount = Math.Min(amount, bankItemCount);
        if (amount <= 0)
        {
            return;
        }

        var tabId = GetTabByItemSlot(player, bankSlot);
        var withdrawItemId = itemId;
        var inventoryCount = playerItems.InvItemCount(player, itemId);
        if (inventoryCount + amount < 0)
        {
            amount = ItemDefinitionLoader.MaxItemAmount - inventoryCount;
            player.LastTickMessage = "You can't carry more of that item!";
        }

        if (itemDefinitions.IsStackable(itemId))
        {
            if (!playerItems.AddItem(player, itemId, amount))
            {
                player.LastTickMessage = "Not enough space in your inventory.";
                return;
            }
        }
        else if (player.WithdrawNote && itemDefinitions.CanBeNoted(itemId))
        {
            withdrawItemId = itemDefinitions.FindNote(itemId);
            if (!playerItems.AddItem(player, withdrawItemId, amount))
            {
                player.LastTickMessage = "Not enough space in your inventory.";
                return;
            }
        }
        else
        {
            var remaining = amount;
            while (remaining > 0 && playerItems.AddItem(player, withdrawItemId, 1))
            {
                remaining--;
            }

            amount -= remaining;
            if (remaining > 0)
            {
                player.LastTickMessage = "Not enough space in your inventory.";
            }
        }
        player.BankItemsN[bankSlot] -= amount;
        if (player.BankItemsN[bankSlot] <= 0)
        {
            player.BankItems[bankSlot] = -1;
            player.BankItemsN[bankSlot] = 0;
            DecreaseTabStartSlots(player, tabId);
            AlignBank(player);
            SendTabConfig(player);
        }

        RefreshBankUi(player);
    }

    public void AlignBank(Player player)
    {
        var tempItems = player.BankItems.ToArray();
        var tempAmounts = player.BankItemsN.ToArray();
        var index = 0;
        for (var i = 0; i < Size; i++)
        {
            if (tempItems[i] == -1)
            {
                continue;
            }

            player.BankItems[index] = tempItems[i];
            player.BankItemsN[index] = tempAmounts[i];
            index++;
        }

        for (var i = index; i < Size; i++)
        {
            player.BankItems[i] = -1;
            player.BankItemsN[i] = 0;
        }
    }

    public int GetBankItemSlot(Player player, int itemId)
    {
        for (var i = 0; i < Size; i++)
        {
            if (player.BankItems[i] == itemId)
            {
                return i;
            }
        }

        return -1;
    }

    public int GetFreeBankSlot(Player player)
    {
        for (var i = 0; i < Size; i++)
        {
            if (player.BankItems[i] == -1)
            {
                return i;
            }
        }

        return -1;
    }

    public int GetBankItemCount(Player player, int itemId)
    {
        var slot = GetBankItemSlot(player, itemId);
        return slot >= 0 ? player.BankItemsN[slot] : 0;
    }

    public void IncreaseTabStartSlots(Player player, int startId)
    {
        for (var i = startId + 1; i < player.TabStartSlot.Length; i++)
        {
            player.TabStartSlot[i]++;
        }
    }

    public void DecreaseTabStartSlots(Player player, int startId)
    {
        if (startId == 10)
        {
            return;
        }

        for (var i = startId + 1; i < player.TabStartSlot.Length; i++)
        {
            player.TabStartSlot[i]--;
        }

        if (GetItemsInTab(player, startId) == 0)
        {
            CollapseTab(player, startId);
        }
    }

    public void Insert(Player player, int fromId, int toId)
    {
        if (fromId < 0 || toId < 0 || fromId >= Size || toId >= Size || fromId == toId)
        {
            return;
        }

        var tempItem = player.BankItems[fromId];
        var tempAmount = player.BankItemsN[fromId];
        if (toId > fromId)
        {
            for (var i = fromId; i < toId; i++)
            {
                player.BankItems[i] = player.BankItems[i + 1];
                player.BankItemsN[i] = player.BankItemsN[i + 1];
            }
        }
        else
        {
            for (var i = fromId; i > toId; i--)
            {
                player.BankItems[i] = player.BankItems[i - 1];
                player.BankItemsN[i] = player.BankItemsN[i - 1];
            }
        }

        player.BankItems[toId] = tempItem;
        player.BankItemsN[toId] = tempAmount;
        RefreshBankUi(player);
    }

    public int GetItemsInTab(Player player, int tabId) => player.TabStartSlot[tabId + 1] - player.TabStartSlot[tabId];

    public int GetTabByItemSlot(Player player, int itemSlot)
    {
        var tabId = 0;
        for (var i = 0; i < player.TabStartSlot.Length; i++)
        {
            if (itemSlot >= player.TabStartSlot[i])
            {
                tabId = i;
            }
        }

        return tabId;
    }

    public void CollapseTab(Player player, int tabId)
    {
        var size = GetItemsInTab(player, tabId);
        if (size <= 0)
        {
            return;
        }

        var tempItems = new int[size];
        var tempAmounts = new int[size];
        for (var i = 0; i < size; i++)
        {
            var slot = player.TabStartSlot[tabId] + i;
            tempItems[i] = player.BankItems[slot];
            tempAmounts[i] = player.BankItemsN[slot];
            player.BankItems[slot] = -1;
            player.BankItemsN[slot] = 0;
        }

        AlignBank(player);
        for (var i = tabId; i < player.TabStartSlot.Length - 1; i++)
        {
            player.TabStartSlot[i] = player.TabStartSlot[i + 1] - size;
        }

        player.TabStartSlot[10] -= size;
        SendTabConfig(player);
        for (var i = 0; i < size; i++)
        {
            var slot = GetFreeBankSlot(player);
            player.BankItems[slot] = tempItems[i];
            player.BankItemsN[slot] = tempAmounts[i];
        }

        RefreshBankUi(player);
    }

    public int GetArrayIndex(int tabId) => tabId switch
    {
        39 or 52 => 2,
        37 or 53 => 3,
        35 or 54 => 4,
        33 or 55 => 5,
        31 or 56 => 6,
        29 or 57 => 7,
        27 or 58 => 8,
        25 or 59 => 9,
        41 or 51 => 10,
        _ => -1
    };

    public void HandleBankSwitch(Player player, int fromSlot, int toSlot)
    {
        if (fromSlot < 0 || fromSlot >= Size || toSlot < 0 || toSlot >= Size)
        {
            return;
        }

        if (!player.InsertMode)
        {
            (player.BankItems[fromSlot], player.BankItems[toSlot]) = (player.BankItems[toSlot], player.BankItems[fromSlot]);
            (player.BankItemsN[fromSlot], player.BankItemsN[toSlot]) = (player.BankItemsN[toSlot], player.BankItemsN[fromSlot]);
            RefreshBankUi(player);
            return;
        }

        if (GetTabByItemSlot(player, fromSlot) == GetTabByItemSlot(player, toSlot))
        {
            Insert(player, fromSlot, toSlot);
            return;
        }

        var tabIndex = GetTabByItemSlot(player, toSlot);
        var fromTab = GetTabByItemSlot(player, fromSlot);
        Insert(player, fromSlot, toSlot > fromSlot ? toSlot - 1 : toSlot);
        IncreaseTabStartSlots(player, tabIndex);
        DecreaseTabStartSlots(player, fromTab);
        SendTabConfig(player);
        RefreshBankUi(player);
    }

    public void MoveToBankTab(Player player, int fromSlot, int tabId)
    {
        var tabIndex = GetArrayIndex(tabId);
        if (tabIndex == -1)
        {
            return;
        }

        if (tabId == 41 && GetTabByItemSlot(player, fromSlot) == 10)
        {
            return;
        }

        var toSlot = tabIndex == 10
            ? GetFreeBankSlot(player)
            : player.TabStartSlot[tabIndex] + GetItemsInTab(player, tabIndex);

        var fromTab = GetTabByItemSlot(player, fromSlot);
        Insert(player, fromSlot, toSlot > fromSlot ? toSlot - 1 : toSlot);
        IncreaseTabStartSlots(player, tabIndex);
        DecreaseTabStartSlots(player, fromTab);
        SendTabConfig(player);
        RefreshBankUi(player);
    }

    public void SendTabConfig(Player player)
    {
        var config = 0;
        config += GetItemsInTab(player, 2);
        config += GetItemsInTab(player, 3) * 1024;
        config += GetItemsInTab(player, 4) * 1048576;
        player.BankTabConfig1 = config;

        config = 0;
        config += GetItemsInTab(player, 5);
        config += GetItemsInTab(player, 6) * 1024;
        config += GetItemsInTab(player, 7) * 1048576;
        player.BankTabConfig2 = config;

        config = -2013265920;
        config += GetItemsInTab(player, 8);
        config += GetItemsInTab(player, 9) * 1024;
        player.BankTabConfig3 = config;
    }

    public void SetBankX(Player player, int amount)
    {
        player.BankX = amount;
    }

    private void RefreshBankUi(Player player)
    {
        // Java sends INDEX of first free slot, not COUNT like we were doing
        int firstFreeSlot = -1;
        for (var i = 0; i < Size; i++)
        {
            if (player.BankItems[i] == -1)
            {
                firstFreeSlot = i;
                break;
            }
        }
        
        // Set count for compatibility but send index to UI like Java
        int freeSlotCount = 0;
        for (var i = 0; i < Size; i++)
        {
            if (player.BankItems[i] == -1)
            {
                freeSlotCount++;
            }
        }
        player.BankFreeSlotCount = freeSlotCount;

        // Send missing frame updates like Java PlayerBank.java:72-76
        Write(player, w =>
        {
            // Java: p.frames.setString(p, "" + getFreeBankSlot(p), 762, 97) - sends SLOT INDEX
            frames.SetString(w, firstFreeSlot.ToString(), 762, 97);
            frames.SetItems(w, -1, 64207, 95, player.BankItems, player.BankItemsN);
            // Add missing inventory frame updates like Java PlayerBank.java:72-76
            frames.SetItems(w, -1, 64209, 93, player.Items, player.ItemsN);
            frames.SetItems(w, 149, 0, 93, player.Items, player.ItemsN);
        });
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
