using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class DropItemMessageHandler : IMessageHandler<DropItemMessage>
{
    private readonly ILogger<DropItemMessageHandler> _logger;

    public DropItemMessageHandler(ILogger<DropItemMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, DropItemMessage message, CancellationToken cancellationToken)
    {
        // TODO: Implement drop item logic
        // E.g., remove item from inventory, spawn ground item
        _logger.LogInformation("[DropItem] Player {SessionId} dropped item {ItemId} from slot {Slot}", session.SessionId, message.ItemId, message.Slot);
        return Task.CompletedTask;
    }
}