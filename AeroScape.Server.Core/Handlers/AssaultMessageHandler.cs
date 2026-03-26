using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

/// <summary>
/// Handles Barbarian Assault minigame messages.
/// Legacy constants preserved for protocol fidelity.
/// </summary>
public class AssaultMessageHandler : IMessageHandler<AssaultMessage>
{
    private readonly ILogger<AssaultMessageHandler> _logger;

    public AssaultMessageHandler(ILogger<AssaultMessageHandler> logger)
    {
        _logger = logger;
    }
    // NPC IDs per wave (indices 0-4 = waves 1-5)
    private static readonly int[] HealerIds  = { 5238, 5239, 5240, 5241, 5242 };
    private static readonly int[] RangerIds  = { 5229, 5230, 5231, 5232, 5233 };
    private static readonly int[] FighterIds = { 5044, 5045, 5213, 5214, 5215 };
    private static readonly int[] RunnerIds  = { 5220, 5221, 5222, 5223, 5224 };

    // NPC count per wave
    private static readonly int[] NpcCountPerWave = { 2, 4, 6, 8, 10 };

    // HP and max-hit per wave
    private static readonly int[] HpPerWave     = { 25, 40, 65, 80, 100 };
    private static readonly int[] MaxHitPerWave  = { 5, 9, 13, 15, 18 };

    // Waiting-room and arena coordinates per wave (index 0-4 = waves 1-5)
    private static readonly (int X, int Y)[] WaitingRoomCoords =
    {
        (2579, 5298), (2587, 5298), (2599, 5298), (2607, 5298), (2579, 5288),
    };

    private static readonly (int X, int Y)[] LobbyCoords =
    {
        (2579, 5299), (2587, 5299), (2599, 5299), (2607, 5299), (2579, 5289),
    };

    private const int ArenaBaseX = 1886;
    private const int ArenaBaseY = 5472;
    private const int MinPlayersToStart = 3;

    public Task HandleAsync(PlayerSession session, AssaultMessage message, CancellationToken cancellationToken)
    {
        // TODO: Integrate with world engine, NPC spawning, player state, and coordinate systems.
        _logger.LogInformation("[Assault] Player {SessionId} action={Action} wave={Wave}", session.SessionId, message.Action, message.Wave);

        switch (message.Action)
        {
            case AssaultAction.GoIn:
                HandleGoIn(session, message.Wave);
                break;

            case AssaultAction.StartWave:
                HandleStartWave(message.Wave);
                break;

            case AssaultAction.EndGame:
                // TODO: Teleport all players back to lobby, clear game state.
                _logger.LogInformation("[Assault] Game ended.");
                break;

            case AssaultAction.PlayerDied:
                // TODO: Teleport all assault players out, send "Oh no! Someone died!" message.
                _logger.LogInformation("[Assault] A player died — ending game.");
                break;

            case AssaultAction.NpcDied:
                // TODO: Track kill counts per type, check wave completion.
                _logger.LogInformation("[Assault] NPC killed in wave.");
                break;
        }

        return Task.CompletedTask;
    }

    private void HandleGoIn(PlayerSession session, int wave)
    {
        if (wave < 1 || wave > 5)
        {
            _logger.LogInformation("[Assault] Invalid wave {Wave}", wave);
            return;
        }

        // TODO: Check player.WaveId vs requested wave.
        // TODO: Toggle waiting state — first call enters waiting room, second call leaves.
        // TODO: If >= MinPlayersToStart waiting and no game in progress, teleport all to arena and start wave.
        var (wx, wy) = WaitingRoomCoords[wave - 1];
        _logger.LogInformation("[Assault] Player entering waiting room for wave {Wave} at ({X}, {Y})", wave, wx, wy);
    }

    private void HandleStartWave(int wave)
    {
        if (wave < 1 || wave > 5) return;

        int idx = wave - 1;
        int count = NpcCountPerWave[idx];
        int hp = HpPerWave[idx];
        int mh = MaxHitPerWave[idx];

        // TODO: Spawn NPCs using world NPC system:
        //   count × HealerIds[idx], RangerIds[idx], FighterIds[idx], RunnerIds[idx]
        //   each with maxHP=hp, maxHit=mh, positioned randomly in arena bounds.
        _logger.LogInformation("[Assault] Starting wave {Wave}: {Count} of each type, HP={Hp}, MaxHit={MaxHit}", wave, count, hp, mh);
        _logger.LogInformation("[Assault] Healers={Healers}, Rangers={Rangers}, Fighters={Fighters}, Runners={Runners}", HealerIds[idx], RangerIds[idx], FighterIds[idx], RunnerIds[idx]);
    }
}
