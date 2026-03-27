using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AeroScape.Server.Data.Models;

/// <summary>
/// Persistent player state — replaces legacy flat-file saves (DavidScape/players/PlayerSave.java + FileManager.java).
/// Maps core fields from the legacy save format into a relational model.
/// </summary>
[Table("Players")]
public class DbPlayer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required, MaxLength(12)]
    public string Username { get; set; } = string.Empty;

    /// <summary>Hashed password. Legacy stored as encoded long — we store a proper hash now.</summary>
    [Required, MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>0 = Player, 1 = Moderator, 2 = Administrator.</summary>
    public int Rights { get; set; }

    // ── Position ────────────────────────────────────────────────────────────
    public int AbsX { get; set; } = 3222;
    public int AbsY { get; set; } = 3219;
    public int HeightLevel { get; set; }

    // ── Combat / Run state ──────────────────────────────────────────────────
    public int RunEnergy { get; set; } = 100;
    public int AttackStyle { get; set; }
    public int AutoRetaliate { get; set; }
    public int SpecialAmount { get; set; } = 100;
    public int SkulledDelay { get; set; }
    public int Gender { get; set; }

    // ── Slayer ──────────────────────────────────────────────────────────────
    public int SlayerTask { get; set; }
    public int SlayerAmount { get; set; }

    // ── Quests / Progression ────────────────────────────────────────────────
    public int DragonSlayer { get; set; }
    public int QuestPoints { get; set; }
    public int Rewards { get; set; }

    // ── Membership / Moderation ─────────────────────────────────────────────
    public int Member { get; set; }
    public int Muted { get; set; }
    public int Banned { get; set; }
    public int DoneCode { get; set; }
    public bool Starter { get; set; }

    // ── Minigame state ──────────────────────────────────────────────────────
    public int WaveId { get; set; } = 1;

    // ── Kill counts ─────────────────────────────────────────────────────────
    public int ZilyanakillCount { get; set; }
    public int BandosKillCount { get; set; }
    public int ArmadylKillCount { get; set; }
    public int SaradominKillCount { get; set; }

    // ── Construction / Housing ──────────────────────────────────────────────
    public int HouseDecor { get; set; }
    public int HouseHeight { get; set; }
    [MaxLength(2048)]
    public string ConstructionRoomsData { get; set; } = string.Empty;
    [MaxLength(4096)]
    public string ConstructionFurnitureData { get; set; } = string.Empty;

    // ── Summoning ───────────────────────────────────────────────────────────
    public int FamiliarType { get; set; }

    // ── Clan ────────────────────────────────────────────────────────────────
    [MaxLength(12)]
    public string OwnClanName { get; set; } = string.Empty;

    // ── Appearance (7 look slots, 5 colour slots) ───────────────────────────
    [MaxLength(128)]
    public string LookData { get; set; } = string.Empty;

    [MaxLength(128)]
    public string ColourData { get; set; } = string.Empty;

    // ── Timestamps ──────────────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLogin { get; set; } = DateTime.UtcNow;

    // ── Navigation properties ───────────────────────────────────────────────
    public ICollection<DbSkill> Skills { get; set; } = new List<DbSkill>();
    public ICollection<DbItem> Items { get; set; } = new List<DbItem>();
    public ICollection<DbBankItem> BankItems { get; set; } = new List<DbBankItem>();
    public ICollection<DbEquipment> Equipment { get; set; } = new List<DbEquipment>();
    public ICollection<DbFriend> Friends { get; set; } = new List<DbFriend>();
    public ICollection<DbGrandExchangeOffer> GrandExchangeOffers { get; set; } = new List<DbGrandExchangeOffer>();
}
