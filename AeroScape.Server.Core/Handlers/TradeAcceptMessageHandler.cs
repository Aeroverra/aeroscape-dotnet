using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class TradeAcceptMessageHandler : IMessageHandler<TradeAcceptMessage>
{
    private readonly ILogger<TradeAcceptMessageHandler> _logger;

    public TradeAcceptMessageHandler(ILogger<TradeAcceptMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, TradeAcceptMessage message, CancellationToken cancellationToken)
    {
        // TODO: Validate partner, confirm trade, swap items.
        // Legacy decoded: playerId = readUnsignedWord() - 33024, /256, +1
        _logger.LogInformation("[TradeAccept] Player {SessionId} accepted trade with partner {PartnerId}", session.SessionId, message.PartnerId);
        return Task.CompletedTask;
    }
}
