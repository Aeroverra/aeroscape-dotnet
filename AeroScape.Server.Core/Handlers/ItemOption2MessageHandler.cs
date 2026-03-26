using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class ItemOption2MessageHandler : IMessageHandler<ItemOption2Message>
{
    private readonly ILogger<ItemOption2MessageHandler> _logger;

    public ItemOption2MessageHandler(ILogger<ItemOption2MessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, ItemOption2Message message, CancellationToken cancellationToken)
    {
        // TODO: Implement item option 2 logic
        // Legacy switch was on InterfaceId:
        //   387 → Unequip item from equipment screen
        _logger.LogInformation("[ItemOption2] Player {SessionId} used item option 2: ItemId={ItemId}, Slot={ItemSlot}, Interface={InterfaceId}", session.SessionId, message.ItemId, message.ItemSlot, message.InterfaceId);
        return Task.CompletedTask;
    }
}
