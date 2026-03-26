using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class ObjectOption1MessageHandler : IMessageHandler<ObjectOption1Message>
{
    private readonly ILogger<ObjectOption1MessageHandler> _logger;

    public ObjectOption1MessageHandler(ILogger<ObjectOption1MessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, ObjectOption1Message message, CancellationToken cancellationToken)
    {
        // TODO: Implement object option 1 logic
        // Legacy behaviour — massive switch on objectId covering:
        //   - Banks (2213, 2672, 280, 4483, 25808, 26972)
        //   - Staircase navigation (36793/36794, 34548/34550, 26106/25604, 1738/1740, etc.)
        //   - Door/gate toggling (1530, 1533, 34809, 25638/25640, etc.)
        //   - Altar switching (6552 ancient ↔ modern, 17010 lunar ↔ modern)
        //   - Prayer altars (409, 34616, 19145, 26286/26288/26289) → restore prayer
        //   - Runecrafting altars (2478-2489) → craft runes from essence
        //   - Woodcutting trees (1276-1309, 5551-5553, etc.) → start woodcutting skill
        //   - Mining rocks (2090-2107, 4028-4030, 6669-6671, 16687) → start mining skill
        //   - Castle Wars portals & barriers (4387/4388/4408, 4469/4470, 4902/4903, etc.)
        //   - GodWars dungeon doors & kill-count gates (26425-26428, 26444, 26303, etc.)
        //   - Agility obstacles (2282, 2294, 20211, 2302, 1948, 25161)
        //   - Barbarian Assault rune supply (20150)
        //   - Construction portal (15482)
        //   - Farming patches (8132, 7871, 7855, 8111, 7941)
        //   - Gravestone retrieval (12719), Barrows chest (10284)
        //   - Wilderness ditch (23271)
        _logger.LogInformation("[ObjectOption1] Player {SessionId} used object option 1: ObjectId={ObjectId}, X={ObjectX}, Y={ObjectY}", session.SessionId, message.ObjectId, message.ObjectX, message.ObjectY);
        return Task.CompletedTask;
    }
}
