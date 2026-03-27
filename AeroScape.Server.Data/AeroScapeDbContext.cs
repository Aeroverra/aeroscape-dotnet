using Microsoft.EntityFrameworkCore;
using AeroScape.Server.Data.Models;

namespace AeroScape.Server.Data;

public class AeroScapeDbContext : DbContext
{
    public AeroScapeDbContext(DbContextOptions<AeroScapeDbContext> options)
        : base(options) { }

    public DbSet<DbPlayer> Players => Set<DbPlayer>();
    public DbSet<DbSkill> Skills => Set<DbSkill>();
    public DbSet<DbItem> Items => Set<DbItem>();
    public DbSet<DbBankItem> BankItems => Set<DbBankItem>();
    public DbSet<DbEquipment> Equipment => Set<DbEquipment>();
    public DbSet<DbFriend> Friends => Set<DbFriend>();
    public DbSet<DbGrandExchangeOffer> GrandExchangeOffers => Set<DbGrandExchangeOffer>();
    public DbSet<DbClanChannel> ClanChannels => Set<DbClanChannel>();
    public DbSet<DbClanRank> ClanRanks => Set<DbClanRank>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Player — unique username
        modelBuilder.Entity<DbPlayer>(entity =>
        {
            entity.HasIndex(p => p.Username).IsUnique();
        });

        // Skills — composite uniqueness (one row per player per skill)
        modelBuilder.Entity<DbSkill>(entity =>
        {
            entity.HasIndex(s => new { s.PlayerId, s.SkillIndex }).IsUnique();
            entity.HasOne(s => s.Player)
                  .WithMany(p => p.Skills)
                  .HasForeignKey(s => s.PlayerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Inventory items
        modelBuilder.Entity<DbItem>(entity =>
        {
            entity.HasIndex(i => new { i.PlayerId, i.Slot }).IsUnique();
            entity.HasOne(i => i.Player)
                  .WithMany(p => p.Items)
                  .HasForeignKey(i => i.PlayerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Bank items
        modelBuilder.Entity<DbBankItem>(entity =>
        {
            entity.HasIndex(b => new { b.PlayerId, b.Slot }).IsUnique();
            entity.HasOne(b => b.Player)
                  .WithMany(p => p.BankItems)
                  .HasForeignKey(b => b.PlayerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Equipment
        modelBuilder.Entity<DbEquipment>(entity =>
        {
            entity.HasIndex(e => new { e.PlayerId, e.Slot }).IsUnique();
            entity.HasOne(e => e.Player)
                  .WithMany(p => p.Equipment)
                  .HasForeignKey(e => e.PlayerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Friends
        modelBuilder.Entity<DbFriend>(entity =>
        {
            entity.HasIndex(f => new { f.PlayerId, f.FriendName }).IsUnique();
            entity.HasOne(f => f.Player)
                  .WithMany(p => p.Friends)
                  .HasForeignKey(f => f.PlayerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Grand Exchange offers
        modelBuilder.Entity<DbGrandExchangeOffer>(entity =>
        {
            entity.HasIndex(g => new { g.PlayerId, g.Slot }).IsUnique();
            entity.HasOne(g => g.Player)
                  .WithMany(p => p.GrandExchangeOffers)
                  .HasForeignKey(g => g.PlayerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Clan Channels — unique owner
        modelBuilder.Entity<DbClanChannel>(entity =>
        {
            entity.HasIndex(c => c.Owner).IsUnique();
        });

        // Clan Ranks — composite uniqueness (one rank per player per clan)
        modelBuilder.Entity<DbClanRank>(entity =>
        {
            entity.HasIndex(r => new { r.ClanChannelId, r.PlayerName }).IsUnique();
            entity.HasOne(r => r.ClanChannel)
                  .WithMany(c => c.Ranks)
                  .HasForeignKey(r => r.ClanChannelId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
