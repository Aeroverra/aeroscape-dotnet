using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;
using AeroScape.Server.Core.Services;

namespace AeroScape.Server.Core.Handlers;

public class ItemOption1MessageHandler : IMessageHandler<ItemOption1Message>
{
    private readonly ILogger<ItemOption1MessageHandler> _logger;
    private readonly IClientUiService _ui;
    private readonly ConstructionService _construction;

    public ItemOption1MessageHandler(ILogger<ItemOption1MessageHandler> logger, IClientUiService ui, ConstructionService construction)
    {
        _logger = logger;
        _ui = ui;
        _construction = construction;
    }
    public Task HandleAsync(PlayerSession session, ItemOption1Message message, CancellationToken cancellationToken)
    {
        var player = session.Entity;
        if (player is null)
        {
            return Task.CompletedTask;
        }

        switch (message.InterfaceId)
        {
            case 1576:
                // Choice dialogue for teleport destinations
                player.Choice = 3;
                player.Dialogue = 0;
                _ui.ShowOptionDialogue(player, "Fight Pits", "Castle Wars", "Port Sarim");
                player.SmithingTimer = -1; // Reset smithing
                player.CookTimer = -1; // Reset cooking
                player.TalkAgent = false;
                player.DecorChange = false;
                break;

            case 396:
                // Construction - Garden Portal
                switch (player.ConstInterface)
                {
                    case 15361: // Garden - Portal
                        // TODO: Create local object portal
                        // p.frames.createLocalObject(p, 13405, p.lastObjectX, p.lastObjectY, 1, 10);
                        _ui.SendMessage(player, "Portal construction not yet implemented.");
                        break;
                }
                break;

            case 394:
                // Construction - Garden furniture based on constInterface
                switch (player.ConstInterface)
                {
                    case 15363: // Garden - Tree space
                        HandleGardenTreeSpace(player, message.ItemSlot);
                        break;

                    case 15365: // Garden - Big plant space 2
                        HandleGardenBigPlantSpace2(player, message.ItemSlot);
                        break;

                    case 15364: // Garden - Big plant space 1
                        HandleGardenBigPlantSpace1(player, message.ItemSlot);
                        break;

                    case 15366: // Garden - Small plant space 1
                        HandleGardenSmallPlantSpace1(player, message.ItemSlot);
                        break;

                    case 15367: // Garden - Small plant space 2
                        HandleGardenSmallPlantSpace2(player, message.ItemSlot);
                        break;
                }
                break;

            // TODO: Add other cases like unequipping (387), summoning, herb cleaning, etc.
        }

        _logger.LogInformation("[ItemOption1] Player {SessionId} used item option 1: ItemId={ItemId}, Slot={ItemSlot}, Interface={InterfaceId}", session.SessionId, message.ItemId, message.ItemSlot, message.InterfaceId);
        return Task.CompletedTask;
    }

    private void HandleGardenTreeSpace(Player player, int itemSlot)
    {
        // Requires 10,000 coins
        var cost = 10000;
        var xp = 1000;

        // TODO: Check if player has enough coins (995) and construction level
        // TODO: Add furniture using construction service
        _ui.SendMessage(player, $"Tree space construction requires {cost} coins (not fully implemented).");
    }

    private void HandleGardenBigPlantSpace2(Player player, int itemSlot)
    {
        var itemId = 8431 + itemSlot;
        var objectId = 13428 + itemSlot;
        var xp = itemSlot switch
        {
            0 => 31,  // Short plant
            1 => 70,  // Large leaf bush
            2 => 100, // Huge plant
            _ => 0
        };

        // TODO: Use construction service to add furniture
        _ui.SendMessage(player, $"Big plant space 2 construction (item slot {itemSlot}, XP: {xp}) not fully implemented.");
    }

    private void HandleGardenBigPlantSpace1(Player player, int itemSlot)
    {
        var itemId = 8431 + itemSlot;
        var objectId = 13425 + itemSlot;
        var xp = itemSlot switch
        {
            0 => 31,  // Fern
            1 => 70,  // Bush
            2 => 100, // Tall plant
            _ => 0
        };

        // TODO: Use construction service to add furniture
        _ui.SendMessage(player, $"Big plant space 1 construction (item slot {itemSlot}, XP: {xp}) not fully implemented.");
    }

    private void HandleGardenSmallPlantSpace1(Player player, int itemSlot)
    {
        var itemId = 8431 + itemSlot;
        var objectId = 13431 + itemSlot;
        var xp = itemSlot switch
        {
            0 => 31,  // Plant
            1 => 70,  // Small fern
            2 => 70,  // Fern
            _ => 0
        };

        // TODO: Use construction service to add furniture
        _ui.SendMessage(player, $"Small plant space 1 construction (item slot {itemSlot}, XP: {xp}) not fully implemented.");
    }

    private void HandleGardenSmallPlantSpace2(Player player, int itemSlot)
    {
        var itemId = 8431 + itemSlot;
        var objectId = 13434 + itemSlot;
        var xp = itemSlot switch
        {
            0 => 31,  // Dock leaf
            1 => 70,  // Thistle
            2 => 100, // Reeds
            _ => 0
        };

        // TODO: Use construction service to add furniture
        _ui.SendMessage(player, $"Small plant space 2 construction (item slot {itemSlot}, XP: {xp}) not fully implemented.");
    }
}
