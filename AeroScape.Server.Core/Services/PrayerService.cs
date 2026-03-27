using System;
using AeroScape.Server.Core.Entities;

namespace AeroScape.Server.Core.Services;

public sealed class PrayerService
{
    public static readonly int[] PrayerConfig =
    {
        83, 84, 85, 862, 863, 86, 87, 88, 89, 90, 91, 864, 865, 92, 93, 94, 95,
        96, 97, 866, 867, 98, 99, 100, 1168, 1052, 1053
    };

    public static readonly int[] PrayerLevel =
    {
        1, 4, 7, 8, 9, 10, 13, 16, 19, 22, 25, 26, 27, 28, 31, 34, 37, 40, 43,
        44, 45, 46, 49, 52, 35, 60, 70
    };

    public static readonly int[] DrainRate =
    {
        3, 4, 5, 6, 7, 8, 9, 10, 6, 7, 6, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21,
        22, 23, 24, 15, 26, 28
    };

    private static readonly int[][] ConflictingPrayers =
    {
        new[] { 5, 13, 25, 26 },
        new[] { 3, 4, 6, 11, 12, 14, 19, 20, 25, 26 },
        new[] { 3, 4, 7, 11, 12, 15, 19, 20, 25, 26 },
        new[] { 1, 2, 4, 6, 7, 11, 12, 14, 15, 19, 20, 25, 26 },
        new[] { 1, 2, 3, 6, 7, 11, 12, 14, 15, 19, 20, 25, 26 },
        new[] { 0, 13, 25, 26 },
        new[] { 1, 3, 4, 11, 12, 14, 19, 20, 25, 26 },
        new[] { 2, 3, 4, 11, 12, 15, 19, 20, 25, 26 },
        Array.Empty<int>(),
        Array.Empty<int>(),
        Array.Empty<int>(),
        new[] { 1, 2, 3, 4, 6, 7, 12, 14, 15, 19, 20, 25, 26 },
        new[] { 1, 2, 3, 4, 6, 7, 11, 14, 15, 19, 20, 25, 26 },
        new[] { 0, 5, 25, 26 },
        new[] { 1, 3, 4, 6, 11, 12, 19, 20, 25, 26 },
        new[] { 2, 3, 4, 7, 11, 12, 19, 20, 25, 26 },
        new[] { 17, 18, 21, 22, 23, 24 },
        new[] { 16, 18, 21, 22, 23, 24 },
        new[] { 16, 17, 21, 22, 23, 24 },
        new[] { 1, 2, 3, 4, 6, 7, 11, 12, 14, 15, 20, 25, 26 },
        new[] { 1, 2, 3, 4, 6, 7, 11, 12, 14, 15, 19, 25, 26 },
        new[] { 16, 17, 18, 22, 23, 24 },
        new[] { 16, 17, 18, 21, 23, 24 },
        new[] { 16, 17, 18, 21, 22, 24 },
        new[] { 16, 17, 18, 21, 22, 23 },
        new[] { 0, 1, 2, 3, 4, 5, 6, 7, 11, 12, 13, 14, 15, 19, 20, 26 },
        new[] { 0, 1, 2, 3, 4, 5, 6, 7, 11, 12, 13, 14, 15, 19, 20, 25 },
    };

    public bool Toggle(Player player, int buttonId)
    {
        int prayerIndex = (buttonId - 5) / 2;
        if (prayerIndex < 0 || prayerIndex >= PrayerConfig.Length)
            return false;

        if (player.SkillLvl[5] <= 0 || player.GetLevelForXP(5) < PrayerLevel[prayerIndex])
            return false;

        foreach (var index in ConflictingPrayers[prayerIndex])
        {
            if (!player.PrayOn[index])
                continue;

            player.PrayOn[index] = false;
            player.DrainRate -= DrainRate[index];
        }

        player.PrayOn[prayerIndex] = !player.PrayOn[prayerIndex];
        player.DrainRate += player.PrayOn[prayerIndex] ? DrainRate[prayerIndex] : -DrainRate[prayerIndex];
        player.DrainRate = Math.Max(player.DrainRate, 0);
        player.PrayerIcon = ResolveHeadIcon(player);
        player.AppearanceUpdateReq = true;
        player.UpdateReq = true;
        return true;
    }

    public void Reset(Player player)
    {
        Array.Clear(player.PrayOn);
        player.DrainRate = 0;
        player.PrayerIcon = -1;
        player.AppearanceUpdateReq = true;
        player.UpdateReq = true;
    }

    public int ResolveHeadIcon(Player player)
    {
        if (player.PrayOn[24])
            return 7;
        if (player.PrayOn[16])
            return 2;
        if (player.PrayOn[17])
            return 1;
        if (player.PrayOn[18])
            return 0;
        if (player.PrayOn[21])
            return 3;
        if (player.PrayOn[22])
            return 5;
        if (player.PrayOn[23])
            return 4;
        
        return -1;
    }
}
