using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Engine;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Services;
using AeroScape.Server.Core.Session;
using AeroScape.Server.Core.Combat;

namespace AeroScape.Server.Core.Handlers;

public class NPCOption3MessageHandler : IMessageHandler<NPCOption3Message>
{
    private readonly ILogger<NPCOption3MessageHandler> _logger;
    private readonly GameEngine _engine;
    private readonly NPCInteractionService _npcInteractionService;

    public NPCOption3MessageHandler(ILogger<NPCOption3MessageHandler> logger, GameEngine engine, NPCInteractionService npcInteractionService)
    {
        _logger = logger;
        _engine = engine;
        _npcInteractionService = npcInteractionService;
    }

    public Task HandleAsync(PlayerSession session, NPCOption3Message message, CancellationToken cancellationToken)
    {
        var player = session.Entity;
        if (player is null || message.NpcIndex <= 0 || message.NpcIndex >= _engine.Npcs.Length)
            return Task.CompletedTask;

        var npc = _engine.Npcs[message.NpcIndex];
        if (npc is null)
            return Task.CompletedTask;

        // Deferred walk-to-NPC pattern: set pending option if not adjacent
        if (!player.NpcOption3)
        {
            player.ClickId = message.NpcIndex;
            player.ClickX = npc.AbsX;
            player.ClickY = npc.AbsY;
            
            if (CombatFormulas.GetDistance(player.AbsX, player.AbsY, player.ClickX, player.ClickY) > 30)
                return Task.CompletedTask;
            
            player.NpcOption3 = true;
        }

        if (player.ClickId <= 0 || player.ClickId >= _engine.Npcs.Length || _engine.Npcs[player.ClickId] is null)
        {
            player.NpcOption3 = false;
            return Task.CompletedTask;
        }

        if (CombatFormulas.GetDistance(player.AbsX, player.AbsY, player.ClickX, player.ClickY) > 1)
            return Task.CompletedTask;

        player.NpcOption3 = false;
        
        // Use the clicked NPC from player state
        npc = _engine.Npcs[player.ClickId];
        
        // Delegate to the interaction service
        _npcInteractionService.HandleNPCOption3(player, npc);
        
        return Task.CompletedTask;
    }
}
