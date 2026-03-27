using AeroScape.Server.Core.Engine;
using AeroScape.Server.Core.Entities;
using System.Linq;

namespace AeroScape.Server.Core.Items;

public sealed class TradingService(GameEngine engine, PlayerItemsService playerItems, ItemDefinitionLoader itemDefinitions)
{
    public void RequestTrade(Player player, int targetIndex)
    {
        if (targetIndex <= 0 || targetIndex >= engine.Players.Length)
        {
            return;
        }

        var target = engine.Players[targetIndex];
        if (target is null || !target.Online || target.PlayerId == player.PlayerId)
        {
            return;
        }

        player.TradePlayer = target.PlayerId;
        if (target.TradePlayer == player.PlayerId)
        {
            OpenFirstScreen(player, target);
        }
    }

    public void ConfirmTrade(Player player)
    {
        var partner = GetPartner(player);
        if (partner is null)
        {
            return;
        }

        switch (player.TradeStage)
        {
            case 1:
                player.TradeAccept[0] = true;
                if (partner.TradeAccept[0])
                {
                    player.TradeStage = 2;
                    partner.TradeStage = 2;
                    player.TradeAccept[0] = false;
                    partner.TradeAccept[0] = false;
                    OpenSecondScreen(player, partner);
                }
                else
                {
                    RefreshScreens(player, partner);
                }
                break;
            case 2:
                player.TradeAccept[1] = true;
                if (partner.TradeAccept[1])
                {
                    CompleteTrade(player, partner);
                }
                else
                {
                    RefreshScreens(player, partner);
                }
                break;
        }
    }

    public void DeclineTrade(Player player)
    {
        var partner = GetPartner(player);
        ReturnItems(player);
        ResetTrade(player);
        if (partner is not null)
        {
            ReturnItems(partner);
            ResetTrade(partner);
        }
    }

    public void OfferItemBySlot(Player player, int itemSlot, int amount)
    {
        var partner = GetPartner(player);
        if (partner is null || player.TradeStage != 1 || itemSlot < 0 || itemSlot >= player.Items.Length)
        {
            return;
        }

        var itemId = player.Items[itemSlot];
        if (itemId < 0)
        {
            return;
        }

        if (!playerItems.HaveItem(player, itemId, amount))
        {
            amount = itemDefinitions.IsStackable(itemId) ? player.ItemsN[itemSlot] : playerItems.InvItemCount(player, itemId);
        }

        if (amount <= 0)
        {
            return;
        }

        OfferItem(player, itemId, amount);
        player.TradeAccept[0] = false;
        partner.TradeAccept[0] = false;
        RefreshScreens(player, partner);
    }

    public void RemoveItemByTradeSlot(Player player, int tradeSlot, int amount)
    {
        var partner = GetPartner(player);
        if (partner is null || tradeSlot < 0 || tradeSlot >= player.TradeItems.Length || player.TradeItems[tradeSlot] < 0)
        {
            return;
        }

        var itemId = player.TradeItems[tradeSlot];
        if (itemDefinitions.IsStackable(itemId))
        {
            var toRemove = Math.Min(amount, player.TradeItemsN[tradeSlot]);
            if (toRemove <= 0 || !playerItems.AddItem(player, itemId, toRemove))
            {
                return;
            }

            player.TradeItemsN[tradeSlot] -= toRemove;
            if (player.TradeItemsN[tradeSlot] <= 0)
            {
                player.TradeItems[tradeSlot] = -1;
                player.TradeItemsN[tradeSlot] = 0;
            }
        }
        else
        {
            var removed = 0;
            while (removed < amount)
            {
                var slot = GetTradeItemSlot(player, itemId);
                if (slot < 0 || !playerItems.AddItem(player, itemId, 1))
                {
                    break;
                }

                player.TradeItems[slot] = -1;
                player.TradeItemsN[slot] = 0;
                removed++;
            }
        }

        player.TradeAccept[0] = false;
        partner.TradeAccept[0] = false;
        RefreshScreens(player, partner);
    }

    public void HandleActionButton(Player player, int interfaceId, int packetOpcode, int buttonId, int slotId)
    {
        switch (interfaceId)
        {
            case 334:
                if (buttonId == 20)
                {
                    ConfirmTrade(player);
                }
                else if (buttonId is 8 or 21)
                {
                    DeclineTrade(player);
                }
                break;
            case 335:
                if (buttonId == 16)
                {
                    ConfirmTrade(player);
                }
                else if (buttonId is 12 or 18)
                {
                    DeclineTrade(player);
                }
                else if (buttonId == 30)
                {
                    var amount = packetOpcode switch
                    {
                        233 => 1,
                        21 => 5,
                        169 => 10,
                        214 => slotId >= 0 && slotId < player.TradeItems.Length ? player.TradeItemsN[slotId] : 0,
                        173 => player.BankX,
                        _ => 0
                    };
                    if (amount > 0)
                    {
                        RemoveItemByTradeSlot(player, slotId, amount);
                    }
                }
                break;
            case 336:
                var offerAmount = packetOpcode switch
                {
                    233 => 1,
                    21 => 5,
                    169 => 10,
                    214 => slotId >= 0 && slotId < player.Items.Length ? playerItems.InvItemCount(player, player.Items[slotId]) : 0,
                    173 => player.BankX,
                    _ => 0
                };
                if (offerAmount > 0)
                {
                    OfferItemBySlot(player, slotId, offerAmount);
                }
                break;
        }
    }

    private void OpenFirstScreen(Player player, Player partner)
    {
        player.TradePlayer = partner.PlayerId;
        partner.TradePlayer = player.PlayerId;
        player.TradeStage = 1;
        partner.TradeStage = 1;
        player.TradeAccept[0] = false;
        player.TradeAccept[1] = false;
        partner.TradeAccept[0] = false;
        partner.TradeAccept[1] = false;
        player.InterfaceId = 335;
        partner.InterfaceId = 335;
        RefreshScreens(player, partner);
    }

    private void CompleteTrade(Player player, Player partner)
    {
        var playerItemsSnapshot = SnapshotTradeItems(player);
        var partnerItemsSnapshot = SnapshotTradeItems(partner);

        // Critical: Validate inventory space before completing trade to prevent duplication
        if (!CanReceiveTradeItems(player, partnerItemsSnapshot) || 
            !CanReceiveTradeItems(partner, playerItemsSnapshot))
        {
            // Not enough inventory space - abort trade and restore items
            RestoreTradeItems(player);
            RestoreTradeItems(partner);
            player.LastTickMessage = "Not enough inventory space to complete trade.";
            partner.LastTickMessage = "Not enough inventory space to complete trade.";
            DeclineTrade(player);
            return;
        }

        // Space validated - safe to complete trade
        // Clear trade containers first to prevent duplication
        ClearTradeContainers(player);
        ClearTradeContainers(partner);
        
        foreach (var (itemId, amount) in partnerItemsSnapshot)
        {
            playerItems.AddItem(player, itemId, amount);
        }

        foreach (var (itemId, amount) in playerItemsSnapshot)
        {
            playerItems.AddItem(partner, itemId, amount);
        }

        ResetTrade(player);
        ResetTrade(partner);
    }

    private bool CanReceiveTradeItems(Player player, (int ItemId, int Amount)[] items)
    {
        // Calculate required inventory slots for trade items
        var requiredSlots = 0;
        foreach (var (itemId, amount) in items)
        {
            if (itemDefinitions.IsStackable(itemId) || itemDefinitions.IsNoted(itemId))
            {
                // Stackable items only need one slot per unique item type
                var existingSlot = GetInventorySlot(player, itemId);
                if (existingSlot < 0) // New item type
                    requiredSlots++;
            }
            else
            {
                // Non-stackable items need one slot per item
                requiredSlots += amount;
            }
        }

        var availableSlots = GetFreeInventorySlotCount(player);
        return availableSlots >= requiredSlots;
    }

    private int GetFreeInventorySlotCount(Player player)
    {
        var count = 0;
        for (var i = 0; i < player.Items.Length; i++)
        {
            if (player.Items[i] == -1)
                count++;
        }
        return count;
    }

    private int GetInventorySlot(Player player, int itemId)
    {
        for (var i = 0; i < player.Items.Length; i++)
        {
            if (player.Items[i] == itemId)
                return i;
        }
        return -1;
    }

    private void RestoreTradeItems(Player player)
    {
        // Return all trade items to player inventory
        for (var i = 0; i < player.TradeItems.Length; i++)
        {
            if (player.TradeItems[i] > 0)
            {
                playerItems.AddItem(player, player.TradeItems[i], player.TradeItemsN[i]);
            }
        }
    }

    private void ClearTradeContainers(Player player)
    {
        Array.Fill(player.TradeItems, -1);
        Array.Fill(player.TradeItemsN, 0);
    }

    private void OfferItem(Player player, int itemId, int amount)
    {
        if (itemDefinitions.IsStackable(itemId))
        {
            var slot = GetTradeItemSlot(player, itemId);
            if (slot < 0)
            {
                slot = GetFreeTradeSlot(player);
                if (slot < 0)
                {
                    return;
                }

                player.TradeItems[slot] = itemId;
            }

            if (!playerItems.DeleteItem(player, itemId, amount))
            {
                return;
            }

            player.TradeItemsN[slot] += amount;
            RefreshScreens(player, GetPartner(player));
            return;
        }

        for (var i = 0; i < amount; i++)
        {
            var slot = GetFreeTradeSlot(player);
            if (slot < 0 || !playerItems.DeleteItem(player, itemId, 1))
            {
                return;
            }

            player.TradeItems[slot] = itemId;
            player.TradeItemsN[slot] = 1;
        }

        RefreshScreens(player, GetPartner(player));
    }

    private void ReturnItems(Player player)
    {
        foreach (var (itemId, amount) in SnapshotTradeItems(player))
        {
            playerItems.AddItem(player, itemId, amount);
        }
    }

    private List<(int ItemId, int Amount)> SnapshotTradeItems(Player player)
    {
        var items = new List<(int ItemId, int Amount)>();
        for (var i = 0; i < player.TradeItems.Length; i++)
        {
            if (player.TradeItems[i] >= 0 && player.TradeItemsN[i] > 0)
            {
                items.Add((player.TradeItems[i], player.TradeItemsN[i]));
            }
        }

        return items;
    }

    private void ResetTrade(Player player)
    {
        Array.Fill(player.TradeItems, -1);
        Array.Fill(player.TradeItemsN, 0);
        player.TradeAccept[0] = false;
        player.TradeAccept[1] = false;
        player.TradePlayer = 0;
        player.TradeStage = 0;
        player.InterfaceId = -1;
        player.TradeStatusText = string.Empty;
        player.TradePartnerText = string.Empty;
        player.TradeFreeSlotText = string.Empty;
        player.TradeConfirmTextSelf = string.Empty;
        player.TradeConfirmTextPartner = string.Empty;
    }

    private Player? GetPartner(Player player) =>
        player.TradePlayer > 0 && player.TradePlayer < engine.Players.Length ? engine.Players[player.TradePlayer] : null;

    private int GetTradeItemSlot(Player player, int itemId)
    {
        for (var i = 0; i < player.TradeItems.Length; i++)
        {
            if (player.TradeItems[i] == itemId)
            {
                return i;
            }
        }

        return -1;
    }

    private int GetFreeTradeSlot(Player player)
    {
        for (var i = 0; i < player.TradeItems.Length; i++)
        {
            if (player.TradeItems[i] == -1)
            {
                return i;
            }
        }

        return -1;
    }

    private void OpenSecondScreen(Player player, Player partner)
    {
        player.InterfaceId = 334;
        partner.InterfaceId = 334;
        RefreshScreens(player, partner);
    }

    private void RefreshScreens(Player player, Player? partner)
    {
        if (partner is null)
        {
            return;
        }

        RefreshFirstScreen(player, partner);
        RefreshFirstScreen(partner, player);
        RefreshSecondScreen(player, partner);
        RefreshSecondScreen(partner, player);
    }

    private void RefreshFirstScreen(Player player, Player partner)
    {
        player.TradePartnerText = $"Trading With: {partner.Username}";
        player.TradeFreeSlotText = $"{partner.Username} has {playerItems.FreeSlotCount(partner)} free inventory slots.";
        player.TradeStatusText = player.TradeAccept[0]
            ? "Waiting for other player..."
            : partner.TradeAccept[0] ? "The other player has accepted." : string.Empty;
    }

    private void RefreshSecondScreen(Player player, Player partner)
    {
        player.TradeConfirmTextSelf = BuildTradeString(player);
        player.TradeConfirmTextPartner = BuildTradeString(partner);
        player.TradeStatusText = player.TradeAccept[1]
            ? "Waiting for other player..."
            : partner.TradeAccept[1] ? "The other player has accepted." : "I agree that if I get scammed, I will not get my item returned.";
    }

    private string BuildTradeString(Player player)
    {
        var items = SnapshotTradeItems(player);
        if (items.Count == 0)
        {
            return "Absolutely nothing!";
        }

        return string.Join("<br>", items.Select(item =>
            item.Amount > 1
                ? $"{itemDefinitions.GetItemName(item.ItemId)} x {item.Amount}"
                : itemDefinitions.GetItemName(item.ItemId)));
    }
}
