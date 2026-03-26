using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class NPCAttackMessageHandler : IMessageHandler<NPCAttackMessage>
{
    private readonly ILogger<NPCAttackMessageHandler> _logger;

    public NPCAttackMessageHandler(ILogger<NPCAttackMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, NPCAttackMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[NPCAttack] Player {SessionId} attacked NPC {NpcIndex}", session.SessionId, message.NpcIndex);
        return Task.CompletedTask;
    }
}