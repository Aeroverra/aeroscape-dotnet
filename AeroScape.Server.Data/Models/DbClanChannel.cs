using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace AeroScape.Server.Data.Models;

/// <summary>
/// Persistent clan channel data - replaces Java legacy clan file saves.
/// Stores clan ownership, settings, ranks and member lists.
/// </summary>
[Table("ClanChannels")]
public class DbClanChannel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required, MaxLength(12)]
    public string Owner { get; set; } = string.Empty;

    [Required, MaxLength(12)]
    public string ClanName { get; set; } = string.Empty;

    public int JoinRequirement { get; set; }
    public int TalkRequirement { get; set; }
    public int KickRequirement { get; set; } = 7;
    public bool LootShareOn { get; set; }

    public virtual ICollection<DbClanRank> Ranks { get; set; } = new List<DbClanRank>();
}

[Table("ClanRanks")]
public class DbClanRank
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ClanChannelId { get; set; }

    [Required, MaxLength(12)]
    public string PlayerName { get; set; } = string.Empty;

    public int Rank { get; set; }

    [ForeignKey("ClanChannelId")]
    public virtual DbClanChannel ClanChannel { get; set; } = null!;
}