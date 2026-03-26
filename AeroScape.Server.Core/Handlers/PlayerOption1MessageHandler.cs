using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class PlayerOption1MessageHandler : IMessageHandler<PlayerOption1Message>
{
    private readonly ILogger<PlayerOption1MessageHandler> _logger;

    public PlayerOption1MessageHandler(ILogger<PlayerOption1MessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, PlayerOption1Message message, CancellationToken cancellationToken)
    {
        // TODO: Implement Player Option 1 logic (e.g. Attack, Trade, Follow)
        _logger.LogInformation("[PlayerOption1] Player {SessionId} interacted with target {TargetIndex}", session.SessionId, message.TargetIndex);
        return Task.CompletedTask;
    }
}