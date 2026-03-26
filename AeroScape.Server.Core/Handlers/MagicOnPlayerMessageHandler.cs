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
/// Handles Magic-on-Player packet (opcode 70).
/// Sets up PvP magic combat by resolving the spell and setting the combat target.
/// The actual casting is driven by the tick cycle's PlayerVsPlayerCombat service.
/// Ported from MagicOnPlayer / PacketManager logic.
/// </summary>
public class MagicOnPlayerMessageHandler : IMessageHandler<MagicOnPlayerMessage>
{
    private readonly ILogger<MagicOnPlayerMessageHandler> _logger;
    private readonly GameEngine _engine;

    public MagicOnPlayerMessageHandler(ILogger<MagicOnPlayerMessageHandler> logger, GameEngine engine)
    {
        _logger = logger;
        _engine = engine;
    }

    public Task HandleAsync(PlayerSession session, MagicOnPlayerMessage message, CancellationToken cancellationToken)
    {
        var player = session.Entity;
        if (player == null)
            return Task.CompletedTask;

        int targetId = message.PlayerId;

        if (targetId <= 0 || targetId >= GameEngine.MaxPlayers)
            return Task.CompletedTask;

        var target = _engine.Players[targetId];
        if (target == null || target.IsDead || !target.Online)
            return Task.CompletedTask;

        // Resolve spell
        // Note: PvP magic uses different interface buttons than PvE.
        // For now we set the target and let the combat system handle it
        // via the standard melee PvP path (which includes checking for autocast).
        // Full PvP magic spell resolution will be expanded in future phases.

        player.AttackPlayer = targetId;
        player.AttackingPlayer = true;

        // Reset NPC combat
        player.AttackingNPC = false;
        player.AttackNPC = 0;

        // Skull in wilderness
        if (Player.IsWildernessArea(player.AbsX, player.AbsY) && player.SkulledDelay <= 0)
        {
            player.SkulledDelay = CombatConstants.SkullDuration;
            player.SkulledUpdateReq = true;
        }

        _logger.LogDebug("[MagicOnPlayer] Player {Player} magic-targeting player {Target}",
            player.Username, target.Username);

        return Task.CompletedTask;
    }
}
