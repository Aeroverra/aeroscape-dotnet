using System;
using System.IO;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Engine;
using AeroScape.Server.Core.Items;
using Microsoft.Extensions.DependencyInjection;

namespace AeroScape.Server.Core.Services;

public sealed class DeathService
{
    private readonly PrayerService _prayer;
    private readonly GroundItemManager _groundItems;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _npcDropPath;

    public DeathService(PrayerService prayer, GroundItemManager groundItems, IServiceProvider serviceProvider)
    {
        _prayer = prayer;
        _groundItems = groundItems;
        _serviceProvider = serviceProvider;
        _npcDropPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "legacy-java", "server508", "data", "npcs", "npcdrops.cfg");
    }

    public void ProcessPlayerDeath(Player player)
    {
        if (!player.IsDead)
            return;

        player.DeathDelay--;
        ApplyDead(player);
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
        }
    }

    private void ApplyDead(Player player)
    {
        player.LastTickMessage = "Oh dear, you are dead.";
        if (player.DeathDelay >= 7 && player.FaceToReq != 65535)
        {
            player.RequestFaceTo(65535);
        }

        if (player.follower != null)
        {
            player.follower.AttackPlayer = 0;
            player.follower.AttackingPlayer = false;
            player.follower.FollowPlayer = player.PlayerId;
        }

        player.RequestAnim(7197, 0);
        if (player.DeathDelay < 0)
            return;

        if (player.PrayerIcon == 3 && player.DeathDelay == 1)
        {
            foreach (var other in GetEngine().Players)
            {
                if (other == null || other.PlayerId == player.PlayerId)
                {
                    continue;
                }

                if (other.AbsX >= player.AbsX - 5 && other.AbsX <= player.AbsX + 5 &&
                    other.AbsY >= player.AbsY - 5 && other.AbsY <= player.AbsY + 5)
                {
                    other.RequestGfx(437, 0);
                    other.AppendHit(10 + Random.Shared.Next(16), 0);
                }
            }
        }

        var inDuel = IsAtDuel(player);
        var inClanField = IsAtClanField(player);
        var inClanLobby = IsAtClanLobby(player);
        var inPits = IsAtPits(player);
        var inWilderness = Player.IsWildernessArea(player.AbsX, player.AbsY);

        if (!inDuel && !inClanField && !inClanLobby && !inPits && inWilderness)
        {
            if (player.Rights < 2)
            {
                DropAllItems(player);
                player.LastTickMessage = "Your items were dropped!";
            }

            CleanupFamiliar(player);
        }

        if (!inDuel && !inClanField && !inClanLobby && !inPits && !inWilderness)
        {
            MoveItemsToGravestone(player);
            CleanupFamiliar(player);
        }

        if (IsBountyArea(player))
        {
            player.BountyOpponent = 0;
        }

        player.AfterDeathUpdateReq = true;
        player.FollowingPlayer = false;
        player.followPlayer = 0;
        player.Overlay = 0;

        if (IsAtCastleWars(player))
        {
            if (player.CWTeam == 0)
            {
                player.SetCoords(2427, 3077, 1);
            }
            else
            {
                player.SetCoords(2372, 3130, 1);
            }

            if (player.Equipment[3] == 4037 && player.CWTeam == 1)
            {
                player.Equipment[3] = -1;
                player.EquipmentN[3] = 0;
                player.AppearanceUpdateReq = true;
                player.UpdateReq = true;
            }

            if (player.Equipment[3] == 4039 && player.CWTeam == 0)
            {
                player.Equipment[3] = -1;
                player.EquipmentN[3] = 0;
                player.AppearanceUpdateReq = true;
                player.UpdateReq = true;
            }

            return;
        }

        if (inPits)
        {
            player.GameStarted = false;
            player.SetCoords(2399, 5172, 0);
            player.LastTickMessage = "You lost.";
            return;
        }

        if (inClanField)
        {
            if (player.ClanSide == 1)
            {
                player.SetCoords(3320, 3781, player.clanheight);
            }
            else
            {
                player.SetCoords(3320, 3770, player.clanheight);
            }

            return;
        }

        if (player.DuelPartner != 0)
        {
            player.SkulledDelay = 0;
            player.SetCoords(player.DuelX, player.DuelY, 0);
            player.ResetDuel();
            return;
        }

        player.SetCoords(3222, 3219, 0);
    }

    private void DropAllItems(Player player)
    {
        _groundItems.CreateGroundItem(526, 1, player.AbsX, player.AbsY, player.HeightLevel, string.Empty);

        for (var i = 0; i < player.Items.Length; i++)
        {
            if (player.Items[i] < 0 || player.ItemsN[i] <= 0)
                continue;

            _groundItems.CreateGroundItem(player.Items[i], player.ItemsN[i], player.AbsX, player.AbsY, player.HeightLevel, string.Empty);
            player.Items[i] = -1;
            player.ItemsN[i] = 0;
        }

        for (var i = 0; i < player.Equipment.Length; i++)
        {
            if (player.Equipment[i] < 0 || player.EquipmentN[i] <= 0)
                continue;

            _groundItems.CreateGroundItem(player.Equipment[i], player.EquipmentN[i], player.AbsX, player.AbsY, player.HeightLevel, string.Empty);
            player.Equipment[i] = -1;
            player.EquipmentN[i] = 0;
        }

        player.AppearanceUpdateReq = true;
        player.UpdateReq = true;
    }

    private void MoveItemsToGravestone(Player player)
    {
        player.gsItems.Clear();
        player.gsItemsN.Clear();
        player.gsEquip.Clear();
        player.gsEquipN.Clear();

        for (var i = 0; i < player.Items.Length; i++)
        {
            if (player.Items[i] < 0 || player.ItemsN[i] <= 0)
                continue;

            player.gsItems.Add(player.Items[i]);
            player.gsItemsN.Add(player.ItemsN[i]);
            player.Items[i] = -1;
            player.ItemsN[i] = 0;
        }

        for (var i = 0; i < player.Equipment.Length; i++)
        {
            if (player.Equipment[i] < 0 || player.EquipmentN[i] <= 0)
                continue;

            player.gsEquip.Add(player.Equipment[i]);
            player.gsEquipN.Add(player.EquipmentN[i]);
            player.Equipment[i] = -1;
            player.EquipmentN[i] = 0;
        }

        player.gsX = player.AbsX;
        player.gsY = player.AbsY;
        player.gsH = player.HeightLevel;
        player.graveStone = true;
        player.graveStoneTimer = 200;
        var engine = GetEngine();
        if (!engine.LoadedObjects.Exists(o => o.ObjectId == 12719 && o.X == player.gsX && o.Y == player.gsY))
        {
            engine.LoadedObjects.Add(new LoadedObject(12719, player.gsX, player.gsY, 0, 10));
        }
        player.AppearanceUpdateReq = true;
        player.UpdateReq = true;
    }

    private GameEngine GetEngine() => _serviceProvider.GetRequiredService<GameEngine>();

    private static void CleanupFamiliar(Player player)
    {
        if (player.follower != null)
        {
            player.follower.IsDead = true;
        }

        player.FamiliarType = 0;
        player.FamiliarId = 0;
        player.follower = null;
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

    private static bool IsAtDuel(Player player)
        => player.AbsX >= 3362 && player.AbsX <= 3391 && player.AbsY >= 3228 && player.AbsY <= 3241;

    private static bool IsAtPits(Player player)
        => player.AbsX >= 2370 && player.AbsX <= 2426 && player.AbsY >= 5128 && player.AbsY <= 5167;

    private static bool IsAtClanLobby(Player player)
        => player.AbsX >= 3264 && player.AbsX <= 3279 && player.AbsY >= 3672 && player.AbsY <= 3695;

    private static bool IsAtClanField(Player player)
        => player.AbsX >= 3263 && player.AbsX <= 3329 && player.AbsY >= 3713 && player.AbsY <= 3841;

    private static bool IsAtCastleWars(Player player)
        => player.AbsX >= 2363 && player.AbsX <= 2432 && player.AbsY >= 3071 && player.AbsY <= 3135;

    private static bool IsBountyArea(Player player)
        => player.AbsX >= 3085 && player.AbsX <= 3185 && player.AbsY >= 3662 && player.AbsY <= 3765;
}
