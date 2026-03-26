using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Engine;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class NPCOption3MessageHandler : IMessageHandler<NPCOption3Message>
{
    private readonly ILogger<NPCOption3MessageHandler> _logger;
    private readonly GameEngine _engine;

    public NPCOption3MessageHandler(ILogger<NPCOption3MessageHandler> logger, GameEngine engine)
    {
        _logger = logger;
        _engine = engine;
    }

    public Task HandleAsync(PlayerSession session, NPCOption3Message message, CancellationToken cancellationToken)
    {
        var player = session.Entity;
        if (player is null || message.NpcIndex <= 0 || message.NpcIndex >= _engine.Npcs.Length)
            return Task.CompletedTask;

        var npc = _engine.Npcs[message.NpcIndex];
        if (npc is null)
            return Task.CompletedTask;

        switch (npc.NpcType)
        {
            case 553:
                player.SetCoords(3253, 3401, 0);
                break;
            case 4906:
                player.Dialogue = 15;
                break;
            case 1861:
                player.Dialogue = 25;
                break;
        }

        _logger.LogInformation("[NPCOption3] Player {Username} npcType={NpcType}", player.Username, npc.NpcType);
        return Task.CompletedTask;
    }
}
