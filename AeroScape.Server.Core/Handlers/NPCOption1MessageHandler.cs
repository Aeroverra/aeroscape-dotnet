using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class NPCOption1MessageHandler : IMessageHandler<NPCOption1Message>
{
    private readonly ILogger<NPCOption1MessageHandler> _logger;

    public NPCOption1MessageHandler(ILogger<NPCOption1MessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, NPCOption1Message message, CancellationToken cancellationToken)
    {
        // TODO: Implement NPC option 1 logic (talk-to, trade, pickpocket, catch implings, etc.)
        // Legacy switched on NPC type ID for dialogues, shops, skill capes, fishing, quests, familiars.
        _logger.LogInformation("[NPCOption1] Player {SessionId} interacted with NPC index {NpcIndex}", session.SessionId, message.NpcIndex);
        return Task.CompletedTask;
    }
}
