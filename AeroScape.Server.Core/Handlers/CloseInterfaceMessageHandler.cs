using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class CloseInterfaceMessageHandler : IMessageHandler<CloseInterfaceMessage>
{
    private readonly ILogger<CloseInterfaceMessageHandler> _logger;

    public CloseInterfaceMessageHandler(ILogger<CloseInterfaceMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, CloseInterfaceMessage message, CancellationToken cancellationToken)
    {
        // TODO: Close open interface, restore tabs/inventory.
        // Legacy also showed update notes on first two closes.
        _logger.LogInformation("[CloseInterface] Player {SessionId}", session.SessionId);
        return Task.CompletedTask;
    }
}
