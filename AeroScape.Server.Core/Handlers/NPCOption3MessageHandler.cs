using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class NPCOption3MessageHandler : IMessageHandler<NPCOption3Message>
{
    private readonly ILogger<NPCOption3MessageHandler> _logger;

    public NPCOption3MessageHandler(ILogger<NPCOption3MessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, NPCOption3Message message, CancellationToken cancellationToken)
    {
        // TODO: Implement NPC Option 3 logic (tertiary NPC interaction).
        // Legacy behaviour: validate NPC index, distance check, face NPC, reset skilling,
        // then switch on NPC type for:
        //   - Makeover Mage interface (548)
        //   - Teleport to GE (553)
        //   - Slayer shop (1599)
        //   - Woodcutting tutor dialogue (4906)
        //   - Range tutor dialogue (1861)
        //   - Crafting tutor dialogue (4900)
        _logger.LogInformation("[NPCOption3] Player {SessionId} used option-3 on NPC index {NpcIndex}", session.SessionId, message.NpcIndex);
        return Task.CompletedTask;
    }
}
