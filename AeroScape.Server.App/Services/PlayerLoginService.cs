using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Data;
using AeroScape.Server.Data.Models;
using AeroScape.Server.Network.Login;

namespace AeroScape.Server.App.Services;

/// <summary>
/// Loads or creates a player from the EF Core database during the login process.
/// Uses IServiceScopeFactory to create a scoped DbContext per login attempt.
/// </summary>
public sealed class PlayerLoginService : IPlayerLoginService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PlayerLoginService> _logger;

    public PlayerLoginService(IServiceScopeFactory scopeFactory, ILogger<PlayerLoginService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<(Player player, int returnCode)> LoadOrCreatePlayerAsync(string username, string password)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AeroScapeDbContext>();

        var dbPlayer = await db.Players
            .Include(p => p.Skills)
            .Include(p => p.Items)
            .Include(p => p.BankItems)
            .Include(p => p.Equipment)
            .Include(p => p.Friends)
            .FirstOrDefaultAsync(p => p.Username == username);

        if (dbPlayer != null)
        {
            // Existing player — verify password
            string hash = HashPassword(password);
            if (dbPlayer.PasswordHash != hash)
            {
                _logger.LogInformation("Wrong password for '{User}'", username);
                return (new Player { Username = username }, 3); // Wrong password
            }

            if (dbPlayer.Banned == 1)
            {
                return (new Player { Username = username }, 4); // Banned
            }

            // Load into runtime Player
            var player = MapToPlayer(dbPlayer);
            dbPlayer.LastLogin = DateTime.UtcNow;
            await db.SaveChangesAsync();

            _logger.LogInformation("Loaded existing player '{User}'", username);
            return (player, 2); // Success
        }
        else
        {
            // New player — auto-register
            var newDbPlayer = new DbPlayer
            {
                Username = username,
                PasswordHash = HashPassword(password),
                Rights = 2, // Admin by default for dev
                AbsX = 3222,
                AbsY = 3219,
                HeightLevel = 0,
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
            };

            db.Players.Add(newDbPlayer);
            await db.SaveChangesAsync();

            var player = MapToPlayer(newDbPlayer);

            _logger.LogInformation("Created new player '{User}' (id={Id})", username, newDbPlayer.Id);
            return (player, 2); // Success
        }
    }

    private static Player MapToPlayer(DbPlayer db)
    {
        var p = new Player
        {
            PersistentId = db.Id,
            Username = db.Username,
            Password = db.PasswordHash,
            PasswordHash = db.PasswordHash,
            Rights = db.Rights,
            AbsX = db.AbsX,
            AbsY = db.AbsY,
            HeightLevel = db.HeightLevel,
            RunEnergy = db.RunEnergy,
            AttackStyle = db.AttackStyle,
            AutoRetaliate = db.AutoRetaliate,
            SpecialAmount = db.SpecialAmount,
            SkulledDelay = db.SkulledDelay,
            Gender = db.Gender,
            SlayerTask = db.SlayerTask,
            SlayerAmount = db.SlayerAmount,
            DragonSlayer = db.DragonSlayer,
            QuestPoints = db.QuestPoints,
            Rewards = db.Rewards,
            Member = db.Member,
            Muted = db.Muted,
            Banned = db.Banned,
            DoneCode = db.DoneCode,
            Starter = db.Starter,
            WaveId = db.WaveId,
            ZilyanakillCount = db.ZilyanakillCount,
            BandosKillCount = db.BandosKillCount,
            ArmadylKillCount = db.ArmadylKillCount,
            SaradominKillCount = db.SaradominKillCount,
            HouseDecor = db.HouseDecor,
            HouseHeight = db.HouseHeight,
            ConstructionRoomsData = db.ConstructionRoomsData,
            ConstructionFurnitureData = db.ConstructionFurnitureData,
            FamiliarType = db.FamiliarType,
            OwnClanName = db.OwnClanName,
        };

        p.InitDefaults();

        // Parse look/colour data
        if (!string.IsNullOrEmpty(db.LookData))
        {
            var parts = db.LookData.Split(',');
            for (int i = 0; i < Math.Min(parts.Length, p.Look.Length); i++)
                if (int.TryParse(parts[i], out int v)) p.Look[i] = v;
        }
        if (!string.IsNullOrEmpty(db.ColourData))
        {
            var parts = db.ColourData.Split(',');
            for (int i = 0; i < Math.Min(parts.Length, p.Colour.Length); i++)
                if (int.TryParse(parts[i], out int v)) p.Colour[i] = v;
        }

        // Load skills
        foreach (var s in db.Skills)
        {
            if (s.SkillIndex >= 0 && s.SkillIndex < Player.SkillCount)
            {
                p.SkillLvl[s.SkillIndex] = s.Level;
                p.SkillXP[s.SkillIndex] = s.Experience;
            }
        }

        // Load inventory items
        foreach (var item in db.Items)
        {
            if (item.Slot >= 0 && item.Slot < p.Items.Length)
            {
                p.Items[item.Slot] = item.ItemId;
                p.ItemsN[item.Slot] = item.Amount;
            }
        }

        // Load equipment
        foreach (var eq in db.Equipment)
        {
            if (eq.Slot >= 0 && eq.Slot < p.Equipment.Length)
            {
                p.Equipment[eq.Slot] = eq.ItemId;
                p.EquipmentN[eq.Slot] = eq.Amount;
            }
        }

        // Load bank items
        foreach (var bi in db.BankItems)
        {
            if (bi.Slot >= 0 && bi.Slot < Player.BankSize)
            {
                p.BankItems[bi.Slot] = bi.ItemId;
                p.BankItemsN[bi.Slot] = bi.Amount;
            }
        }

        // Load friends
        foreach (var f in db.Friends)
        {
            p.Friends.Add(Core.Util.NameUtil.StringToLong(f.FriendName));
        }

        return p;
    }

    private static string HashPassword(string password)
    {
        // Simple SHA256 hash — good enough for a private RSPS
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
