using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class NPCOption2MessageHandler : IMessageHandler<NPCOption2Message>
{
    private readonly ILogger<NPCOption2MessageHandler> _logger;

    public NPCOption2MessageHandler(ILogger<NPCOption2MessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, NPCOption2Message message, CancellationToken cancellationToken)
    {
        // TODO: Implement NPC Option 2 logic (secondary NPC interaction).
        // Legacy behaviour: validate NPC index, distance check, face NPC, reset skilling,
        // then switch on NPC type for:
        //   - Dragon dialogues (6901, 6903, 6905, 6907)
        //   - Minigame rewards (5029)
        //   - Shops (6970, 549, 548, 521, 682)
        //   - Makeover (598 – gender-dependent interface)
        //   - Ranged tutor items (1861)
        //   - Fishing (316 trout, 312 shark, 313 manta)
        //   - Thieving (20 paladin, 21 hero, 1/9 man, 2234 farmer)
        //   - Banking (2270, 2619, 494, 495)
        _logger.LogInformation("[NPCOption2] Player {SessionId} used option-2 on NPC index {NpcIndex}", session.SessionId, message.NpcIndex);
        return Task.CompletedTask;
    }
}
