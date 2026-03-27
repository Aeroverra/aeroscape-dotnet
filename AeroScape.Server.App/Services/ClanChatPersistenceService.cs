using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Services;
using AeroScape.Server.Data;
using AeroScape.Server.Data.Models;

namespace AeroScape.Server.App.Services;

public sealed class ClanChatPersistenceService : IClanChatPersistenceService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ClanChatPersistenceService> _logger;

    public ClanChatPersistenceService(IServiceScopeFactory scopeFactory, ILogger<ClanChatPersistenceService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task LoadAllChannelsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AeroScapeDbContext>();

        var channels = await db.ClanChannels
            .Include(c => c.Ranks)
            .AsNoTracking()
            .ToListAsync();

        var clanService = scope.ServiceProvider.GetRequiredService<ClanChatService>();

        foreach (var dbChannel in channels)
        {
            var channel = new ClanChannel(dbChannel.Owner, dbChannel.ClanName)
            {
                JoinRequirement = dbChannel.JoinRequirement,
                TalkRequirement = dbChannel.TalkRequirement,
                KickRequirement = dbChannel.KickRequirement,
                LootShareOn = dbChannel.LootShareOn
            };

            foreach (var dbRank in dbChannel.Ranks)
            {
                channel.Ranks[dbRank.PlayerName] = dbRank.Rank;
            }

            clanService.LoadChannel(channel);
        }

        _logger.LogInformation("Loaded {Count} clan channels", channels.Count);
    }

    public async Task SaveChannelAsync(string ownerName, ClanChannel channel)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AeroScapeDbContext>();

        var dbChannel = await db.ClanChannels
            .Include(c => c.Ranks)
            .FirstOrDefaultAsync(c => c.Owner == ownerName);

        if (dbChannel == null)
        {
            dbChannel = new DbClanChannel { Owner = ownerName };
            db.ClanChannels.Add(dbChannel);
        }

        dbChannel.ClanName = channel.ClanName;
        dbChannel.JoinRequirement = channel.JoinRequirement;
        dbChannel.TalkRequirement = channel.TalkRequirement;
        dbChannel.KickRequirement = channel.KickRequirement;
        dbChannel.LootShareOn = channel.LootShareOn;

        // Update ranks
        db.ClanRanks.RemoveRange(dbChannel.Ranks);
        dbChannel.Ranks.Clear();

        foreach (var (playerName, rank) in channel.Ranks)
        {
            dbChannel.Ranks.Add(new DbClanRank
            {
                PlayerName = playerName,
                Rank = rank
            });
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteChannelAsync(string ownerName)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AeroScapeDbContext>();

        var dbChannel = await db.ClanChannels
            .FirstOrDefaultAsync(c => c.Owner == ownerName);

        if (dbChannel != null)
        {
            db.ClanChannels.Remove(dbChannel);
            await db.SaveChangesAsync();
        }
    }
}