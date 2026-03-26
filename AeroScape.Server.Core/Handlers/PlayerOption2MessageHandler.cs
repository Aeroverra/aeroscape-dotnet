using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class PlayerOption2MessageHandler : IMessageHandler<PlayerOption2Message>
{
    private readonly ILogger<PlayerOption2MessageHandler> _logger;

    public PlayerOption2MessageHandler(ILogger<PlayerOption2MessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, PlayerOption2Message message, CancellationToken cancellationToken)
    {
        // TODO: Implement Player Option 2 logic (Trade request).
        // Legacy behaviour: validate target player, distance check, send trade request,
        // face target, and initiate the trade system via pTrade.tradePlayer().
        _logger.LogInformation("[PlayerOption2] Player {SessionId} sent trade/option-2 to target index {TargetIndex}", session.SessionId, message.TargetIndex);
        return Task.CompletedTask;
    }
}
