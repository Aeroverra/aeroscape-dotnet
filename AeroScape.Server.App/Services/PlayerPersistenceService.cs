using System;
using System.Threading;
using System.Threading.Tasks;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Services;
using AeroScape.Server.Core.Util;
using AeroScape.Server.Data;
using AeroScape.Server.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AeroScape.Server.App.Services;

public sealed class PlayerPersistenceService : BackgroundService, IPlayerPersistenceService
{
    private static readonly TimeSpan SaveInterval = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AeroScape.Server.Core.Engine.GameEngine _engine;
    private readonly ILogger<PlayerPersistenceService> _logger;

    public PlayerPersistenceService(
        IServiceScopeFactory scopeFactory,
        AeroScape.Server.Core.Engine.GameEngine engine,
        ILogger<PlayerPersistenceService> logger)
    {
        _scopeFactory = scopeFactory;
        _engine = engine;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            for (int i = 1; i < _engine.Players.Length; i++)
            {
                var player = _engine.Players[i];
                if (player is null || !player.Online)
                    continue;

                try
                {
                    await SavePlayerAsync(player, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Failed to persist player '{Player}'", player.Username);
                }
            }

            await Task.Delay(SaveInterval, stoppingToken);
        }
    }

    public async Task SavePlayerAsync(Player player, CancellationToken cancellationToken = default)
    {
        if (player.PersistentId <= 0)
            return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AeroScapeDbContext>();

        var dbPlayer = await db.Players
            .Include(p => p.Skills)
            .Include(p => p.Items)
            .Include(p => p.BankItems)
            .Include(p => p.Equipment)
            .Include(p => p.Friends)
            .FirstOrDefaultAsync(p => p.Id == player.PersistentId, cancellationToken);

        if (dbPlayer is null)
            return;

        dbPlayer.Rights = player.Rights;
        dbPlayer.AbsX = player.AbsX;
        dbPlayer.AbsY = player.AbsY;
        dbPlayer.HeightLevel = player.HeightLevel;
        dbPlayer.RunEnergy = player.RunEnergy;
        dbPlayer.AttackStyle = player.AttackStyle;
        dbPlayer.AutoRetaliate = player.AutoRetaliate;
        dbPlayer.SpecialAmount = player.SpecialAmount;
        dbPlayer.SkulledDelay = player.SkulledDelay;
        dbPlayer.Gender = player.Gender;
        dbPlayer.SlayerTask = player.SlayerTask;
        dbPlayer.SlayerAmount = player.SlayerAmount;
        dbPlayer.DragonSlayer = player.DragonSlayer;
        dbPlayer.QuestPoints = player.QuestPoints;
        dbPlayer.Rewards = player.Rewards;
        dbPlayer.Member = player.Member;
        dbPlayer.Muted = player.Muted;
        dbPlayer.Banned = player.Banned;
        dbPlayer.DoneCode = player.DoneCode;
        dbPlayer.Starter = player.Starter;
        dbPlayer.WaveId = player.WaveId;
        dbPlayer.ZilyanakillCount = player.ZilyanakillCount;
        dbPlayer.BandosKillCount = player.BandosKillCount;
        dbPlayer.ArmadylKillCount = player.ArmadylKillCount;
        dbPlayer.SaradominKillCount = player.SaradominKillCount;
        dbPlayer.HouseDecor = player.HouseDecor;
        dbPlayer.HouseHeight = player.HouseHeight;
        dbPlayer.ConstructionRoomsData = player.ConstructionRoomsData;
        dbPlayer.ConstructionFurnitureData = player.ConstructionFurnitureData;
        dbPlayer.FamiliarType = player.FamiliarType;
        dbPlayer.OwnClanName = player.OwnClanName;
        dbPlayer.PasswordHash = string.IsNullOrWhiteSpace(player.PasswordHash) ? dbPlayer.PasswordHash : player.PasswordHash;
        dbPlayer.LookData = string.Join(',', player.Look);
        dbPlayer.ColourData = string.Join(',', player.Colour);
        dbPlayer.LastLogin = DateTime.UtcNow;

        db.Skills.RemoveRange(dbPlayer.Skills);
        db.Items.RemoveRange(dbPlayer.Items);
        db.BankItems.RemoveRange(dbPlayer.BankItems);
        db.Equipment.RemoveRange(dbPlayer.Equipment);
        db.Friends.RemoveRange(dbPlayer.Friends);

        dbPlayer.Skills.Clear();
        dbPlayer.Items.Clear();
        dbPlayer.BankItems.Clear();
        dbPlayer.Equipment.Clear();
        dbPlayer.Friends.Clear();

        for (int i = 0; i < Player.SkillCount; i++)
        {
            dbPlayer.Skills.Add(new DbSkill
            {
                PlayerId = dbPlayer.Id,
                SkillIndex = i,
                Level = player.SkillLvl[i],
                Experience = player.SkillXP[i],
            });
        }

        for (int i = 0; i < player.Items.Length; i++)
        {
            if (player.Items[i] < 0)
                continue;

            dbPlayer.Items.Add(new DbItem
            {
                PlayerId = dbPlayer.Id,
                Slot = i,
                ItemId = player.Items[i],
                Amount = Math.Max(player.ItemsN[i], 1),
            });
        }

        for (int i = 0; i < Player.BankSize; i++)
        {
            if (player.BankItems[i] < 0)
                continue;

            dbPlayer.BankItems.Add(new DbBankItem
            {
                PlayerId = dbPlayer.Id,
                Slot = i,
                ItemId = player.BankItems[i],
                Amount = Math.Max(player.BankItemsN[i], 1),
            });
        }

        for (int i = 0; i < player.Equipment.Length; i++)
        {
            if (player.Equipment[i] < 0)
                continue;

            dbPlayer.Equipment.Add(new DbEquipment
            {
                PlayerId = dbPlayer.Id,
                Slot = i,
                ItemId = player.Equipment[i],
                Amount = Math.Max(player.EquipmentN[i], 1),
            });
        }

        foreach (var friend in player.Friends)
        {
            dbPlayer.Friends.Add(new DbFriend
            {
                PlayerId = dbPlayer.Id,
                FriendName = NameUtil.LongToString(friend),
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
