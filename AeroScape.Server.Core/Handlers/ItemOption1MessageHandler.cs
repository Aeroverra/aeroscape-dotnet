using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class ItemOption1MessageHandler : IMessageHandler<ItemOption1Message>
{
    private readonly ILogger<ItemOption1MessageHandler> _logger;

    public ItemOption1MessageHandler(ILogger<ItemOption1MessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, ItemOption1Message message, CancellationToken cancellationToken)
    {
        // TODO: Implement item option 1 logic (use, eat, drink, unequip, summon familiar, etc.)
        // Legacy switch was on InterfaceId:
        //   387 → Unequip item
        //   394/396 → Construction furniture
        //   1576 → Choice dialogue (Fight Pits / Castle Wars / Port Sarim)
        //   12175, 12171, 12187, ... → Summoning familiar pouches
        //   199, 207 → Herb cleaning
        _logger.LogInformation("[ItemOption1] Player {SessionId} used item option 1: ItemId={ItemId}, Slot={ItemSlot}, Interface={InterfaceId}", session.SessionId, message.ItemId, message.ItemSlot, message.InterfaceId);
        return Task.CompletedTask;
    }
}
