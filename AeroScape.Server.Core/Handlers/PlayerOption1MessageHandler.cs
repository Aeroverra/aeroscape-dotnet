using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Combat;
using AeroScape.Server.Core.Engine;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

/// <summary>
/// Handles Player Option 1 packet (opcode 160) — typically "Attack" in wilderness.
/// Sets the player's PvP combat target so the tick cycle drives the fight.
/// Ported from PlayerOption1.java / PacketManager.
/// </summary>
public class PlayerOption1MessageHandler : IMessageHandler<PlayerOption1Message>
{
    private readonly ILogger<PlayerOption1MessageHandler> _logger;
    private readonly GameEngine _engine;

    public PlayerOption1MessageHandler(ILogger<PlayerOption1MessageHandler> logger, GameEngine engine)
    {
        _logger = logger;
        _engine = engine;
    }

    public Task HandleAsync(PlayerSession session, PlayerOption1Message message, CancellationToken cancellationToken)
    {
        var player = session.Entity;
        if (player == null)
            return Task.CompletedTask;

        int targetIndex = message.TargetIndex;

        if (targetIndex <= 0 || targetIndex >= GameEngine.MaxPlayers)
        {
            _logger.LogWarning("[PlayerOption1] Invalid target index {Index} from player {Player}",
                targetIndex, player.Username);
            return Task.CompletedTask;
        }

        var target = _engine.Players[targetIndex];
        if (target == null || target.IsDead || !target.Online)
        {
            _logger.LogDebug("[PlayerOption1] Target {Index} is unavailable", targetIndex);
            return Task.CompletedTask;
        }

        // Set PvP combat target — actual combat runs in the tick cycle
        player.AttackPlayer = targetIndex;
        player.AttackingPlayer = true;

        // Reset any NPC combat
        player.AttackingNPC = false;
        player.AttackNPC = 0;

        // Apply skull if initiating in wilderness
        if (Player.IsWildernessArea(player.AbsX, player.AbsY) && player.SkulledDelay <= 0)
        {
            player.SkulledDelay = CombatConstants.SkullDuration;
            player.SkulledUpdateReq = true;
        }

        _logger.LogDebug("[PlayerOption1] Player {Player} targeting player {Target} for PvP",
            player.Username, target.Username);

        return Task.CompletedTask;
    }
}
