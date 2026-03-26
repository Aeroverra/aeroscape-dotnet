using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class PickupItemMessageHandler : IMessageHandler<PickupItemMessage>
{
    private readonly ILogger<PickupItemMessageHandler> _logger;

    public PickupItemMessageHandler(ILogger<PickupItemMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, PickupItemMessage message, CancellationToken cancellationToken)
    {
        // TODO: Implement pickup item logic
        // Legacy behaviour:
        //   - Check distance to ground item coords
        //   - If not adjacent, set itemPickup flag and wait for walking to complete
        //   - Look up ground item by (itemId, x, y, heightLevel) in world item list
        //   - If found, add item to player inventory and remove from ground
        _logger.LogInformation("[PickupItem] Player {SessionId} picking up item {ItemId} at ({ItemX}, {ItemY})", session.SessionId, message.ItemId, message.ItemX, message.ItemY);
        return Task.CompletedTask;
    }
}
