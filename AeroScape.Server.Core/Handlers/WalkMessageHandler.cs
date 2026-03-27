using System.Threading;
using System.Threading.Tasks;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Movement;
using AeroScape.Server.Core.Services;
using AeroScape.Server.Core.Session;
using Microsoft.Extensions.Logging;

namespace AeroScape.Server.Core.Handlers;

/// <summary>
/// Handles walking packets (regular walk, mini-map walk, etc.).
/// Translated from legacy DavidScape Walking.java.
/// </summary>
public class WalkMessageHandler : IMessageHandler<WalkMessage>
{
    private readonly ILogger<WalkMessageHandler> _logger;
    private readonly WalkQueue _walkQueue;
    private readonly IClientUiService _ui;

    public WalkMessageHandler(ILogger<WalkMessageHandler> logger, WalkQueue walkQueue, IClientUiService ui)
    {
        _logger = logger;
        _walkQueue = walkQueue;
        _ui = ui;
    }

    public async Task HandleAsync(PlayerSession session, WalkMessage message, CancellationToken cancellationToken)
    {
        var player = session.Entity;
        if (player is null)
            return;

        _logger.LogDebug(
            "Player {Username} walking: first=({X}, {Y}) steps={Steps} running={Running} opcode={Opcode}",
            player.Username,
            message.FirstX,
            message.FirstY,
            message.PathX.Length + 1,
            message.IsRunning,
            message.PacketId);

        // Check for freeze delay - match Java Walking.java:55-57
        if (player.FreezeDelay > 0)
        {
            _ui.SendMessage(player, "You cant move! Your frozen!");
            return;
        }

        // Reset all interaction states as per Java Walking.java lines 45-54
        player.ItemPickup = false;
        player.PlayerOption1 = false;
        player.PlayerOption2 = false;
        player.PlayerOption3 = false;
        player.NpcOption1 = false;
        player.NpcOption2 = false;
        player.ObjectOption1 = false;
        player.ObjectOption2 = false;
        player.AttackingPlayer = false;
        player.AttackingNPC = false;
        player.usingAutoCast = false;

        // UI restoration would be handled here if methods were available in IClientUiService
        // For now, we'll just close any open interface
        // TODO: Add methods to IClientUiService for RemoveShownInterface, RestoreTabs, etc.

        // Reset face-to request if needed (Java line 60-62)
        if (player.FaceToReq != 65535)
        {
            player.RequestFaceTo(65535);
        }

        _walkQueue.HandleWalk(player, message);
    }
}
