using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class ItemOnObjectMessageHandler : IMessageHandler<ItemOnObjectMessage>
{
    private readonly ILogger<ItemOnObjectMessageHandler> _logger;

    public ItemOnObjectMessageHandler(ILogger<ItemOnObjectMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, ItemOnObjectMessage message, CancellationToken cancellationToken)
    {
        // TODO: Implement item-on-object logic (smelting, smithing, cooking, farming, cannon placement, etc.)
        // Legacy handled furnace (56332), anvil (54540), range (58124/28173), farming patch (34573), and more.
        _logger.LogInformation("[ItemOnObject] Player {SessionId} used item {ItemId} on object {ObjectId}", session.SessionId, message.ItemId, message.ObjectId);
        return Task.CompletedTask;
    }
}
