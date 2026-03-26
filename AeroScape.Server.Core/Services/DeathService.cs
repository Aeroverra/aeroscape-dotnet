using System;
using AeroScape.Server.Core.Entities;

namespace AeroScape.Server.Core.Services;

public sealed class DeathService
{
    private readonly InventoryService _inventory;
    private readonly PrayerService _prayer;

    public DeathService(InventoryService inventory, PrayerService prayer)
    {
        _inventory = inventory;
        _prayer = prayer;
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
            npc.HiddenNPC = true;
            return;
        }

        if (npc.HiddenNPC && npc.RespawnDelay > 0)
            npc.RespawnDelay--;
    }
}
