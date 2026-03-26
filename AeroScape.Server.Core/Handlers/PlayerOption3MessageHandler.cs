using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class PlayerOption3MessageHandler : IMessageHandler<PlayerOption3Message>
{
    private readonly ILogger<PlayerOption3MessageHandler> _logger;

    public PlayerOption3MessageHandler(ILogger<PlayerOption3MessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, PlayerOption3Message message, CancellationToken cancellationToken)
    {
        // TODO: Implement Player Option 3 logic (Duel / Clan challenge).
        // Legacy behaviour: validate target player, distance check, then branch on
        // whether the player is at the clan lobby (clan challenge) or elsewhere (duel request).
        // Clan path: validate clan membership, send challenge, and if both ready, teleport
        // both clans into the clan battle arena.
        // Duel path: send duel request, and if both ready, teleport to duel arena.
        _logger.LogInformation("[PlayerOption3] Player {SessionId} sent duel/clan-challenge to target index {TargetIndex}", session.SessionId, message.TargetIndex);
        return Task.CompletedTask;
    }
}
