using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Engine;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

/// <summary>
/// Handles the NPC Attack packet (opcode 123).
/// Sets the player's combat target to the specified NPC, so the next
/// game tick will drive the combat via PlayerVsNpcCombat.ProcessAttack.
/// Ported from PacketManager / NPCAttack.java logic.
/// </summary>
public class NPCAttackMessageHandler : IMessageHandler<NPCAttackMessage>
{
    private readonly ILogger<NPCAttackMessageHandler> _logger;
    private readonly GameEngine _engine;

    public NPCAttackMessageHandler(ILogger<NPCAttackMessageHandler> logger, GameEngine engine)
    {
        _logger = logger;
        _engine = engine;
    }

    public Task HandleAsync(PlayerSession session, NPCAttackMessage message, CancellationToken cancellationToken)
    {
        var player = session.Entity;
        if (player == null)
            return Task.CompletedTask;

        int npcIndex = message.NpcIndex;

        if (npcIndex <= 0 || npcIndex >= GameEngine.MaxNpcs)
        {
            _logger.LogWarning("[NPCAttack] Invalid NPC index {Index} from player {Player}",
                npcIndex, player.Username);
            return Task.CompletedTask;
        }

        var npc = _engine.Npcs[npcIndex];
        if (npc == null || npc.IsDead)
        {
            _logger.LogDebug("[NPCAttack] NPC {Index} is null or dead", npcIndex);
            return Task.CompletedTask;
        }

        // Set combat target — actual combat logic runs in the tick cycle
        player.AttackNPC = npcIndex;
        player.AttackingNPC = true;
        npc.FollowPlayer = player.PlayerId;

        // Reset any player-vs-player combat
        player.AttackingPlayer = false;
        player.AttackPlayer = 0;
        player.RequestFaceTo(npcIndex);

        _logger.LogDebug("[NPCAttack] Player {Player} targeting NPC {NpcIndex} (type {NpcType})",
            player.Username, npcIndex, npc.NpcType);

        return Task.CompletedTask;
    }
}
