using System.Threading;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Services;

namespace AeroScape.Server.Core.Handlers;

public sealed class ButtonMessageHandler : IMessageHandler<ButtonMessage>
{
    private readonly ShopService _shops;
    private readonly IClientUiService _ui;

    public ButtonMessageHandler(ShopService shops, IClientUiService ui)
    {
        _shops = shops;
        _ui = ui;
    }

    public async Task HandleAsync(PlayerSession session, ButtonMessage message, CancellationToken cancellationToken)
    {
        var player = session.Entity;
        if (player == null)
            return;

        // Handle shop interface buttons
        if (message.InterfaceId == 620) // Shop interface
        {
            if (message.ButtonId == 24) // Shop item button
            {
                // Bounds check to prevent array index out of range exception
                if (message.SlotId < 0 || message.SlotId >= player.ShopItems.Length)
                    return;
                    
                int itemId = player.ShopItems[message.SlotId];
                switch (message.ItemId) // Using ItemId as packet type identifier
                {
                    case 233: // Value
                        int shopValue = _shops.GetPrice(player.ShopId, message.SlotId);
                        _ui.SendMessage(player, $"This item costs {shopValue} coin{(shopValue != 1 ? "s" : "")}.");
                        break;
                    case 21: // Buy 1
                        _shops.Buy(player, itemId, 1);
                        break;
                    case 169: // Buy 5
                        _shops.Buy(player, itemId, 5);
                        break;
                    case 214: // Buy 10
                        _shops.Buy(player, itemId, 10);
                        break;
                    case 90: // Examine
                        // Would need item definition service for descriptions
                        _ui.SendMessage(player, "Examine functionality not yet implemented.");
                        break;
                }
            }
        }
        
        await Task.CompletedTask;
    }
}