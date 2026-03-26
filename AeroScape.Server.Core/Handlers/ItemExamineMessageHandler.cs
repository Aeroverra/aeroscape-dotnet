using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class ItemExamineMessageHandler : IMessageHandler<ItemExamineMessage>
{
    private readonly ILogger<ItemExamineMessageHandler> _logger;

    public ItemExamineMessageHandler(ILogger<ItemExamineMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, ItemExamineMessage message, CancellationToken cancellationToken)
    {
        // TODO: Look up item description from item definition provider and send to player.
        _logger.LogInformation("[ItemExamine] Player {SessionId} examined item {ItemId}", session.SessionId, message.ItemId);
        return Task.CompletedTask;
    }
}
