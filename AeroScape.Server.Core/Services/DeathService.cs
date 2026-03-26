using System;
using System.IO;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Items;

namespace AeroScape.Server.Core.Services;

public sealed class DeathService
{
    private readonly InventoryService _inventory;
    private readonly PrayerService _prayer;
    private readonly GroundItemManager _groundItems;
    private readonly string _npcDropPath;

    public DeathService(InventoryService inventory, PrayerService prayer, GroundItemManager groundItems)
    {
        _inventory = inventory;
        _prayer = prayer;
        _groundItems = groundItems;
        _npcDropPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "legacy-java", "server508", "data", "npcs", "npcdrops.cfg");
    }

    public void ProcessPlayerDeath(Player player)
    {
        if (!player.IsDead)
            return;

        player.DeathDelay--;
        if (player.DeathDelay > 0)
            return;

        RestoreAfterDeath(player);
    }

    public void RestoreAfterDeath(Player player)
    {
        for (int i = 0; i < Player.SkillCount; i++)
            player.SkillLvl[i] = player.GetLevelForXP(i);

        _prayer.Reset(player);
        player.FreezeDelay = 0;
        player.SkulledDelay = 0;
        player.SpecialAmount = 100;
        player.RunEnergy = 100;
        player.IsDead = false;
        player.AfterDeathUpdateReq = false;
        player.DeathDelay = 7;
        player.TeleX = 3222;
        player.TeleY = 3219;
        player.SetCoords(3222, 3219, 0);
        player.SpecialAmountUpdateReq = true;
        player.RunEnergyUpdateReq = true;
        player.SkulledUpdateReq = true;
    }

    public void ProcessNpcDeath(NPC npc)
    {
        if (!npc.IsDead)
            return;

        if (!npc.DeadEmoteDone)
        {
            npc.RequestAnim(npc.DeathEmote, 0);
            npc.DeadEmoteDone = true;
            npc.CombatDelay = 3;
            return;
        }

        if (!npc.HiddenNPC && npc.CombatDelay <= 0)
        {
            npc.NpcCanLoot = true;
            DropNpcLoot(npc);
            npc.HiddenNPC = true;
            return;
        }

        if (npc.HiddenNPC && npc.RespawnDelay > 0)
            npc.RespawnDelay--;
    }

    private void DropNpcLoot(NPC npc)
    {
        if (!File.Exists(_npcDropPath))
        {
            return;
        }

        foreach (var rawLine in File.ReadLines(_npcDropPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("/", StringComparison.Ordinal) || !line.Contains('='))
            {
                continue;
            }

            var split = line.Split('=', 2);
            if (!int.TryParse(split[0], out var npcId) || npcId != npc.NpcType)
            {
                continue;
            }

            var drops = split[1].Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var drop in drops)
            {
                var parts = drop.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 3)
                {
                    continue;
                }

                if (!int.TryParse(parts[0], out var itemId))
                {
                    continue;
                }

                var minMax = parts[1].Split('-', StringSplitOptions.RemoveEmptyEntries);
                var chanceParts = parts[2].Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (minMax.Length != 2 || chanceParts.Length != 2 ||
                    !int.TryParse(minMax[0], out var min) ||
                    !int.TryParse(minMax[1], out var max) ||
                    !int.TryParse(chanceParts[0], out var chance) ||
                    !int.TryParse(chanceParts[1], out var outOf))
                {
                    continue;
                }

                var amount = Random.Shared.Next(min, max + 1);
                var roll = Random.Shared.Next(1, outOf + 1);
                if (roll <= chance)
                {
                    _groundItems.CreateGroundItem(itemId, amount, npc.AbsX, npc.AbsY, npc.HeightLevel, string.Empty);
                }
            }

            break;
        }
    }
}
