using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class MagicOnPlayerMessageHandler : IMessageHandler<MagicOnPlayerMessage>
{
    private readonly ILogger<MagicOnPlayerMessageHandler> _logger;

    public MagicOnPlayerMessageHandler(ILogger<MagicOnPlayerMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, MagicOnPlayerMessage message, CancellationToken cancellationToken)
    {
        // TODO: Implement magic-on-player PvP logic.
        // Legacy checked wilderness boundaries, duel arena partner, clan field teammates,
        // and jail/lobby restrictions before allowing spell casting.
        // InterfaceId 388 handled modern PvP spells (e.g. Ice Barrage with ButtonId 3).
        _logger.LogInformation("[MagicOnPlayer] Player {SessionId} cast spell (button {ButtonId}, interface {InterfaceId}) on player {PlayerId}", session.SessionId, message.ButtonId, message.InterfaceId, message.PlayerId);
        return Task.CompletedTask;
    }
}
