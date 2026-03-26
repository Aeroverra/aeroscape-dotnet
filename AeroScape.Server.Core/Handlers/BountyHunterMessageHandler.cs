using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class BountyHunterMessageHandler : IMessageHandler<BountyHunterMessage>
{
    private readonly ILogger<BountyHunterMessageHandler> _logger;

    public BountyHunterMessageHandler(ILogger<BountyHunterMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, BountyHunterMessage message, CancellationToken cancellationToken)
    {
        // TODO: Implement bounty hunter target logic
        _logger.LogInformation("[BountyHunter] Player {SessionId} updated target to {TargetId}", session.SessionId, message.TargetId);
        return Task.CompletedTask;
    }
}