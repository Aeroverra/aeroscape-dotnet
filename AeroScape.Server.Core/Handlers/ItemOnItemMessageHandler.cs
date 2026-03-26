using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class ItemOnItemMessageHandler : IMessageHandler<ItemOnItemMessage>
{
    private readonly ILogger<ItemOnItemMessageHandler> _logger;

    public ItemOnItemMessageHandler(ILogger<ItemOnItemMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, ItemOnItemMessage message, CancellationToken cancellationToken)
    {
        // TODO: Implement item on item logic (e.g., combining items, firemaking, fletching)
        _logger.LogInformation("[ItemOnItem] Player {SessionId} used item {ItemUsedId} on item {UsedWithId}", session.SessionId, message.ItemUsedId, message.UsedWithId);
        return Task.CompletedTask;
    }
}