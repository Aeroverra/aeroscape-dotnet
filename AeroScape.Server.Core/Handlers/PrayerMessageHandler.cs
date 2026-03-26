using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class PrayerMessageHandler : IMessageHandler<PrayerMessage>
{
    private readonly ILogger<PrayerMessageHandler> _logger;

    public PrayerMessageHandler(ILogger<PrayerMessageHandler> logger)
    {
        _logger = logger;
    }
    private static readonly int[] PrayerConfig =
    {
        83, 84, 85, 862, 863, 86, 87, 88, 89, 90, 91, 864, 865, 92, 93, 94, 95,
        96, 97, 866, 867, 98, 99, 100, 1168, 1052, 1053
    };

    private static readonly int[] PrayerLevel =
    {
        1, 4, 7, 8, 9, 10, 13, 16, 19, 22, 25, 26, 27, 28, 31, 34, 37, 40, 43,
        44, 45, 46, 49, 52, 35, 60, 70
    };

    private static readonly int[] DrainRate =
    {
        3, 4, 5, 6, 7, 8, 9, 10, 6, 7, 6, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21,
        22, 23, 24, 15, 26, 28
    };

    /// <summary>
    /// Defines which prayers must be turned off when activating a given prayer index.
    /// </summary>
    private static readonly int[][] ConflictingPrayers =
    {
        /* 0  */ new[] { 5, 13, 25, 26 },
        /* 1  */ new[] { 3, 4, 6, 11, 12, 14, 19, 20, 25, 26 },
        /* 2  */ new[] { 3, 4, 7, 11, 12, 15, 19, 20, 25, 26 },
        /* 3  */ new[] { 1, 2, 4, 6, 7, 11, 12, 14, 15, 19, 20, 25, 26 },
        /* 4  */ new[] { 1, 2, 3, 6, 7, 11, 12, 14, 15, 19, 20, 25, 26 },
        /* 5  */ new[] { 0, 13, 25, 26 },
        /* 6  */ new[] { 1, 3, 4, 11, 12, 14, 19, 20, 25, 26 },
        /* 7  */ new[] { 2, 3, 4, 11, 12, 15, 19, 20, 25, 26 },
        /* 8  */ Array.Empty<int>(),
        /* 9  */ Array.Empty<int>(),
        /* 10 */ Array.Empty<int>(),
        /* 11 */ new[] { 1, 2, 3, 4, 6, 7, 12, 14, 15, 19, 20, 25, 26 },
        /* 12 */ new[] { 1, 2, 3, 4, 6, 7, 11, 14, 15, 19, 20, 25, 26 },
        /* 13 */ new[] { 0, 5, 25, 26 },
        /* 14 */ new[] { 1, 3, 4, 6, 11, 12, 19, 20, 25, 26 },
        /* 15 */ new[] { 2, 3, 4, 7, 11, 12, 19, 20, 25, 26 },
        /* 16 */ new[] { 17, 18, 21, 22, 23, 24 },
        /* 17 */ new[] { 16, 18, 21, 22, 23, 24 },
        /* 18 */ new[] { 16, 17, 21, 22, 23, 24 },
        /* 19 */ new[] { 1, 2, 3, 4, 6, 7, 11, 12, 14, 15, 20, 25, 26 },
        /* 20 */ new[] { 1, 2, 3, 4, 6, 7, 11, 12, 14, 15, 19, 25, 26 },
        /* 21 */ new[] { 16, 17, 18, 22, 23, 24 },
        /* 22 */ new[] { 16, 17, 18, 21, 23, 24 },
        /* 23 */ new[] { 16, 17, 18, 21, 22, 24 },
        /* 24 */ new[] { 16, 17, 18, 21, 22, 23 },
        /* 25 */ new[] { 0, 1, 2, 3, 4, 5, 6, 7, 11, 12, 13, 14, 15, 19, 20, 26 },
        /* 26 */ new[] { 0, 1, 2, 3, 4, 5, 6, 7, 11, 12, 13, 14, 15, 19, 20, 25 },
    };

    public Task HandleAsync(PlayerSession session, PrayerMessage message, CancellationToken cancellationToken)
    {
        // TODO: Integrate with player skill levels, prayer state, config frames, and head icons.
        // The legacy logic iterates button ids from 5..58 step 2 to map to prayer index 0..26.
        // Each prayer toggle deactivates conflicting prayers, adjusts drain rate, and updates head icons.
        _logger.LogInformation("[Prayer] Player {SessionId} pressed prayer button {ButtonId}", session.SessionId, message.ButtonId);

        int prayerIndex = (message.ButtonId - 5) / 2;
        if (prayerIndex < 0 || prayerIndex >= PrayerConfig.Length)
        {
            _logger.LogWarning("[Prayer] Invalid prayer index {PrayerIndex} for button {ButtonId}", prayerIndex, message.ButtonId);
            return Task.CompletedTask;
        }

        _logger.LogInformation("[Prayer] Prayer index={PrayerIndex}, requiredLevel={RequiredLevel}, configId={ConfigId}, drainRate={DrainRate}", prayerIndex, PrayerLevel[prayerIndex], PrayerConfig[prayerIndex], DrainRate[prayerIndex]);

        // TODO: Check player prayer skill level >= PrayerLevel[prayerIndex]
        // TODO: Call SwitchConflictingPrayers using ConflictingPrayers[prayerIndex]
        // TODO: Toggle prayer on/off, update config frame, drain rate, head icon, appearance

        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the overhead prayer icon index for the given prayer, or -1 if none.
    /// </summary>
    public static int GetHeadIcon(int prayerIndex) => prayerIndex switch
    {
        16 => 2,  // Protect from Magic
        17 => 1,  // Protect from Ranged
        18 => 0,  // Protect from Melee
        21 => 3,  // Retribution
        22 => 5,  // Smite
        23 => 4,  // Redemption
        24 => 7,  // Summoning (Soul Split-era)
        _ => -1,
    };
}
