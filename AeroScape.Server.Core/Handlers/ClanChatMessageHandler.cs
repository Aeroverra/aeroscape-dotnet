using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class ClanChatMessageHandler : IMessageHandler<ClanChatMessage>
{
    private readonly ILogger<ClanChatMessageHandler> _logger;

    public ClanChatMessageHandler(ILogger<ClanChatMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, ClanChatMessage message, CancellationToken cancellationToken)
    {
        // TODO: Implement clan chat logic
        _logger.LogInformation("[ClanChat] Player {SessionId} ({PlayerName}) sent message to clan {ClanName}: {Message}", session.SessionId, message.PlayerName, message.ClanName, message.Message);
        return Task.CompletedTask;
    }
}