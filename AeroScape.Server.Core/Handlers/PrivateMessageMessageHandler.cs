using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class PrivateMessageMessageHandler : IMessageHandler<PrivateMessageMessage>
{
    private readonly ILogger<PrivateMessageMessageHandler> _logger;

    public PrivateMessageMessageHandler(ILogger<PrivateMessageMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, PrivateMessageMessage message, CancellationToken cancellationToken)
    {
        // TODO: Look up target player by encoded name, send received private message frame.
        // If offline, send "Player is currently offline." to sender.
        _logger.LogInformation("[PM] Player {SessionId} → target {TargetName}: {Text}", session.SessionId, message.TargetName, message.Text);
        return Task.CompletedTask;
    }
}
