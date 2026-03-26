using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class NpcExamineMessageHandler : IMessageHandler<NpcExamineMessage>
{
    private readonly ILogger<NpcExamineMessageHandler> _logger;

    public NpcExamineMessageHandler(ILogger<NpcExamineMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, NpcExamineMessage message, CancellationToken cancellationToken)
    {
        // TODO: Look up NPC description and send to player.
        _logger.LogInformation("[NpcExamine] Player {SessionId} examined NPC {NpcId}", session.SessionId, message.NpcId);
        return Task.CompletedTask;
    }
}
