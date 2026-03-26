using System;
using System.Collections.Generic;
using AeroScape.Server.Core.Skills;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Entities;

/// <summary>
/// Runtime game-state for a connected player.
/// Translated from DavidScape/players/Player.java — network-layer independent.
/// Persistent data lives in DbPlayer; this is the live, in-memory representation.
/// </summary>
public class Player
{
    // ── Identity / Slot ─────────────────────────────────────────────────────
    public int PersistentId { get; set; }
    public int PlayerId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int Rights { get; set; }
    public bool Online { get; set; }
    public bool[] Disconnected { get; set; } = new bool[2];
    public PlayerSession? Session { get; set; }

    // ── Position & Movement ─────────────────────────────────────────────────
    public int AbsX { get; set; }
    public int AbsY { get; set; }
    public int HeightLevel { get; set; }
    public int MapRegionX { get; set; } = -1;
    public int MapRegionY { get; set; } = -1;
    public int CurrentX { get; set; }
    public int CurrentY { get; set; }
    public bool UsingHd { get; set; }

    public int WalkDir { get; set; } = -1;
    public int RunDir { get; set; } = -1;
    public bool IsRunning { get; set; }
    public bool MapRegionDidChange { get; set; }
    public bool DidTeleport { get; set; }
    public int TeleportToX { get; set; } = -1;
    public int TeleportToY { get; set; } = -1;

    // Walking queue
    public int WalkingQueueSize { get; set; } = 50;
    public int WQueueReadPtr { get; set; }
    public int WQueueWritePtr { get; set; }
    public int[] WalkingQueueX { get; set; } = Array.Empty<int>();
    public int[] WalkingQueueY { get; set; } = Array.Empty<int>();
    public int[] WalkingQueue { get; set; } = Array.Empty<int>();

    // ── Appearance ──────────────────────────────────────────────────────────
    public int[] Look { get; set; } = new int[7]; // hair, beard, torso, arms, bracelets, legs, shoes
    public int[] Colour { get; set; } = new int[5];
    public int Gender { get; set; }
    public int NpcType { get; set; } = -1; // -1 = normal player, else NPC morph

    // ── Emotes ──────────────────────────────────────────────────────────────
    public int RunEmote { get; set; } = 0x338;
    public int WalkEmote { get; set; } = 0x333;
    public int StandEmote { get; set; } = 0x328;
    public int TurnEmote { get; set; } = 0x336;
    public int AttackEmote { get; set; } = 422;

    // ── Update flags ────────────────────────────────────────────────────────
    public bool UpdateReq { get; set; }
    public bool AppearanceUpdateReq { get; set; }
    public bool ChatTextUpdateReq { get; set; }
    public string ChatText { get; set; } = string.Empty;
    public int ChatTextEffects { get; set; }
    public bool AnimUpdateReq { get; set; }
    public int AnimReq { get; set; } = -1;
    public int AnimDelay { get; set; }
    public bool GfxUpdateReq { get; set; }
    public int GfxReq { get; set; } = -1;
    public int GfxDelay { get; set; }
    public bool FaceToUpdateReq { get; set; }
    public int FaceToReq { get; set; } = -1;
    public bool Hit1UpdateReq { get; set; }
    public bool Hit2UpdateReq { get; set; }
    public int HitDiff1 { get; set; }
    public int HitDiff2 { get; set; }
    public int PoisonHit1 { get; set; }
    public int PoisonHit2 { get; set; }
    public bool ForceChatUpdateReq { get; set; }
    public string ForceChat { get; set; } = string.Empty;

    // ── Skills ──────────────────────────────────────────────────────────────
    public const int SkillCount = 25;
    public int[] SkillLvl { get; set; } = new int[SkillCount];
    public int[] SkillXP { get; set; } = new int[SkillCount];
    public int[] SkillLvlActual { get; set; } = new int[SkillCount]; // tracks highest achieved for level-up msgs
    public int CombatLevel { get; set; }

    // ── Head icons ──────────────────────────────────────────────────────────
    public int PkIcon { get; set; } = -1;
    public int HeadIcon { get; set; } = -1;
    public int PrayerIcon { get; set; } = -1;

    // ── Prayer ──────────────────────────────────────────────────────────────
    public int PrayerDrain { get; set; } = 100;
    public int DrainRate { get; set; }
    public bool[] PrayOn { get; set; } = new bool[27];

    // ── Equipment / Inventory ───────────────────────────────────────────────
    public int[] Equipment { get; set; } = new int[14];
    public int[] EquipmentN { get; set; } = new int[14];
    public int[] EquipmentBonus { get; set; } = new int[12];
    public int[] Items { get; set; } = new int[28];
    public int[] ItemsN { get; set; } = new int[28];

    // ── Bank ────────────────────────────────────────────────────────────────
    public const int BankSize = 1000;
    public int[] BankItems { get; set; } = new int[BankSize];
    public int[] BankItemsN { get; set; } = new int[BankSize];
    public int[] TabStartSlot { get; set; } = new int[11];
    public int BankX { get; set; } = 50;
    public int ViewingBankTab { get; set; } = 10;
    public bool WithdrawNote { get; set; }
    public bool InsertMode { get; set; }
    public int BankTabConfig1 { get; set; }
    public int BankTabConfig2 { get; set; }
    public int BankTabConfig3 { get; set; } = -2013265920;
    public int BankFreeSlotCount { get; set; }

    // ── Energy / Special ────────────────────────────────────────────────────
    public int RunEnergy { get; set; } = 100;
    public int RunEnergyDelay { get; set; }
    public bool RunEnergyUpdateReq { get; set; }
    public int SpecialAmount { get; set; } = 100;
    public int SpecialAmountDelay { get; set; }
    public bool SpecialAmountUpdateReq { get; set; }
    public bool UsingSpecial { get; set; }

    // ── Combat state ────────────────────────────────────────────────────────
    public int AttackStyle { get; set; }
    public int AutoRetaliate { get; set; }
    public int SkulledDelay { get; set; }
    public bool SkulledUpdateReq { get; set; }
    public int EatDelay { get; set; }
    public int BuryDelay { get; set; }
    public int DrinkDelay { get; set; }
    public int MagicDelay { get; set; }
    public int FreezeDelay { get; set; }
    public int AttackDelay { get; set; } = 5;
    public int CombatDelay { get; set; }
    public int AttackPlayer { get; set; }
    public bool AttackingPlayer { get; set; }
    public int AttackNPC { get; set; }
    public bool AttackingNPC { get; set; }

    // ── Magic autocasting state ─────────────────────────────────────────
    /// <summary>Whether the player is autocasting spells.</summary>
    public bool AutoCasting { get; set; }
    /// <summary>Internal spell ID (1-16) for the spell being autocast.</summary>
    public int AutoCastSpellId { get; set; } = -1;
    /// <summary>Whether the magic system is ready to cast (resets after each cast).</summary>
    public bool MagicCanCast { get; set; } = true;

    // ── Vengeance state ─────────────────────────────────────────────────
    /// <summary>Whether vengeance is active on this player.</summary>
    public bool VengOn { get; set; }
    public long LastVengeanceTime { get; set; }
    public double ArenaSpellPower { get; set; } = 1.0;

    // ── Multi-hit tracking (Dragon Claws, etc.) ─────────────────────────
    public int SecondHit { get; set; }
    public int ThirdHit { get; set; }
    public int FourthHit { get; set; }
    public int ClawTimer { get; set; }
    public bool UseClaws { get; set; }

    // ── Prayer Hitter counter (for protection prayer bypass) ────────────
    public int Hitter { get; set; } = 5;
    public int ClickDelay { get; set; } = -1;
    public int NpcDelay { get; set; }
    public int DeathDelay { get; set; } = 7;
    public bool IsDead { get; set; }
    public bool AfterDeathUpdateReq { get; set; }

    // ── Teleport state ──────────────────────────────────────────────────────
    public int TeleX { get; set; } = -1;
    public int TeleY { get; set; } = -1;
    public int TeleDelay { get; set; } = -1;
    public int TeleFinishGfx { get; set; }
    public int TeleFinishGfxHeight { get; set; }
    public int TeleFinishAnim { get; set; }

    // ── Wilderness-ditch jump ───────────────────────────────────────────────
    public int JumpDelay { get; set; }
    public bool JumpUpdateReq { get; set; } = true;

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
    public bool Jailed { get; set; }
    public int JailTimer { get; set; }
    public bool HouseLocked { get; set; }
    public int VerificationCode { get; set; }

    // ── Kill counts ─────────────────────────────────────────────────────────
    public int ZilyanakillCount { get; set; }
    public int BandosKillCount { get; set; }
    public int ArmadylKillCount { get; set; }
    public int SaradominKillCount { get; set; }

    // ── Friends / Ignores ───────────────────────────────────────────────────
    public List<long> Friends { get; set; } = new(200);
    public List<long> Ignores { get; set; } = new(100);

    // ── Social options ──────────────────────────────────────────────────────
    public bool PlayerOption1 { get; set; }
    public bool PlayerOption2 { get; set; }
    public bool PlayerOption3 { get; set; }
    public bool NpcOption1 { get; set; }
    public bool NpcOption2 { get; set; }
    public bool NpcOption3 { get; set; }
    public bool ObjectOption1 { get; set; }
    public bool ObjectOption2 { get; set; }
    public bool ItemPickup { get; set; }

    // ── Player / NPC lists for update ───────────────────────────────────────
    public const int MaxPlayers = 2000;
    public const int MaxNpcs = 50000;
    public static int maxPlayers { get; set; } = MaxPlayers;
    public Player?[] PlayerList { get; set; } = new Player?[MaxPlayers];
    public byte[] PlayersInList { get; set; } = new byte[MaxPlayers];
    public int PlayerListSize { get; set; }
    public bool RebuildNPCList { get; set; }
    public NPC?[] NpcList { get; set; } = new NPC?[MaxNpcs];
    public byte[] NpcsInList { get; set; } = new byte[MaxNpcs];
    public int NpcListSize { get; set; }

    // ── Interfaces ──────────────────────────────────────────────────────────
    public int InterfaceId { get; set; } = -1;
    public int ChatboxInterfaceId { get; set; } = -1;
    public int Dialogue { get; set; }
    public int Choice { get; set; }
    public int DestroyItemId { get; set; }
    public int InputId { get; set; } = -1;

    // ── Click state ─────────────────────────────────────────────────────────
    public int ClickX { get; set; }
    public int ClickY { get; set; }
    public int ClickId { get; set; }
    public int LastObjectX { get; set; }
    public int LastObjectY { get; set; }
    public int ConstInterface { get; set; }
    public int[] NextRoom { get; set; } = new int[3];
    public int ReqX { get; set; } = -1;
    public int ReqY { get; set; } = -1;

    // ── Login ───────────────────────────────────────────────────────────────
    public int LoginStage { get; set; }
    public long LoginTimeout { get; set; }

    // ── Stat restore ────────────────────────────────────────────────────────
    public int StatRestoreDelay { get; set; } = 75;
    public int StatPotRestoreDelay { get; set; } = 5;

    // ── Following ───────────────────────────────────────────────────────────
    public int FollowPlayerIndex { get; set; }
    public bool FollowingPlayer { get; set; }

    // ── Trade ───────────────────────────────────────────────────────────────
    public int[] TradeItems { get; set; } = new int[28];
    public int[] TradeItemsN { get; set; } = new int[28];
    public bool[] TradeAccept { get; set; } = new bool[2];
    public int TradePlayer { get; set; }
    public int TradeStage { get; set; }
    public string TradeStatusText { get; set; } = string.Empty;
    public string TradePartnerText { get; set; } = string.Empty;
    public string TradeFreeSlotText { get; set; } = string.Empty;
    public string TradeConfirmTextSelf { get; set; } = string.Empty;
    public string TradeConfirmTextPartner { get; set; } = string.Empty;

    // ── Duel ────────────────────────────────────────────────────────────────
    public bool DuelReady { get; set; }
    public int DuelPartner { get; set; }
    public bool DuelCan { get; set; }
    public int DuelTimer { get; set; } = -1;
    public int DuelX { get; set; }
    public int DuelY { get; set; }

    // ── Construction / Housing ──────────────────────────────────────────────
    public int HouseDecor { get; set; } = 1585;
    public int HouseHeight { get; set; }
    public int HouseTele { get; set; } = -1;
    public bool InHouse { get; set; }
    public bool OwnHouse { get; set; }
    public int PersonHouse { get; set; }
    public bool BuildingMode { get; set; }
    public string ConstructionRoomsData { get; set; } = string.Empty;
    public string ConstructionFurnitureData { get; set; } = string.Empty;

    // ── Summoning ───────────────────────────────────────────────────────────
    public int FamiliarType { get; set; }
    public int FamiliarId { get; set; }

    // ── Clan ────────────────────────────────────────────────────────────────
    public int ClanChat { get; set; }
    public int ClanChannel { get; set; }
    public string ClanName { get; set; } = string.Empty;

    // ── Minigame timers (Fight Pits, Castle Wars, etc.) ─────────────────────
    public int WaveId { get; set; } = 1;
    public int PitGame { get; set; } = -1;
    public bool GameStarted { get; set; }
    public int CWTeam { get; set; }
    public int Overlay { get; set; }
    public int OverTimer { get; set; } = -1;
    public int FightEnemies { get; set; }

    // ── Bounty Hunter ───────────────────────────────────────────────────────
    public int BountyOpponent { get; set; }
    public int ZamFL { get; set; }
    public int SaraFL { get; set; }

    // ── Shops ───────────────────────────────────────────────────────────────
    public int ShopId { get; set; }
    public int[] ShopItems { get; set; } = new int[40];
    public int[] ShopItemsN { get; set; } = new int[40];
    public bool PartyShop { get; set; }

    // ── Skilling timers (Cooking, Fishing, Smithing, etc.) ──────────────────
    public int CookTimer { get; set; } = -1;
    public int CookAmount { get; set; }
    public int CookXP { get; set; }
    public int CookGet { get; set; }
    public int CookId { get; set; }

    public int FishTimer { get; set; } = -1;
    public int FishAmount { get; set; }
    public int FishXP { get; set; }
    public int FishGet { get; set; }
    public int FishEmote { get; set; }

    public int FletchTimer { get; set; } = -1;
    public int FletchAmount { get; set; }
    public int FletchXP { get; set; }
    public int FletchGet { get; set; }
    public int FletchId { get; set; }

    public int SmithingTimer { get; set; } = -1;
    public int SmithingAmount { get; set; }
    public int SmithingXP { get; set; }
    public int SmithingGet { get; set; }
    public int SmithingId { get; set; }

    public int HerbloreTimer { get; set; } = -1;
    public int HerbloreType { get; set; }
    public int HerbType { get; set; }

    // ── Agility timers ──────────────────────────────────────────────────────
    public int AgilityXP { get; set; }
    public int AgilityTimer { get; set; } = -1;

    // ── Gathering skill instances ───────────────────────────────────────────
    // These are created lazily / during init. Each skill encapsulates its own
    // state and tick processing, mirroring the Java pattern where Player had
    // `public Woodcutting wc;` and `public Mining mi;` fields.
    public WoodcuttingSkill Woodcutting { get; private set; } = null!;
    public MiningSkill Mining { get; private set; } = null!;
    public FishingSkill Fishing { get; private set; } = null!;
    public CookingSkill Cooking { get; private set; } = null!;
    public SmithingSkill Smithing { get; private set; } = null!;
    public FiremakingSkill Firemaking { get; private set; } = null!;
    public FletchingSkill Fletching { get; private set; } = null!;
    public CraftingSkill Crafting { get; private set; } = null!;
    public HerbloreSkill Herblore { get; private set; } = null!;
    public RunecraftingSkill Runecrafting { get; private set; } = null!;

    // ── Fishing state (used by FishingSkill's tick processing) ──────────────
    public bool IsFishing { get; set; }
    public int NetType { get; set; }
    public bool Bait { get; set; }
    public int FishMan { get; set; }

    // ── Action / idle ───────────────────────────────────────────────────────
    public int ActionTimer { get; set; }
    public int Idle { get; set; }
    public int SaveTimer { get; set; } = 17;
    public int FireDelay { get; set; } = -1;

    // ── Misc persistent state ───────────────────────────────────────────────
    public int HomeTele { get; set; }
    public int HomeTeleDelay { get; set; }
    public bool NormalHomeTele { get; set; }
    public bool AncientsHomeTele { get; set; }
    public int IsLunar { get; set; }
    public int IsAncients { get; set; }

    // ── Damage tracking (for kill attribution) ──────────────────────────────
    public int[] KilledBy { get; set; } = new int[MaxPlayers];

    // ── Barrows ─────────────────────────────────────────────────────────────
    public bool[] Barrows { get; set; } = new bool[6];

    // ── Home teleport sequence ──────────────────────────────────────────────
    public int YellTimer { get; set; }
    public int SuggestionTimer { get; set; }

    // ── Legacy Player.java compatibility fields ────────────────────────────
    public long lastVeng { get; set; }
    public bool graveStone { get; set; }
    public int gsH { get; set; }
    public int gsX { get; set; }
    public NPC? follower { get; set; }
    public int gsY { get; set; }
    public int constInterface { get; set; }
    public int lastObjectX { get; set; }
    public int lastObjectY { get; set; }
    public int taken { get; set; }
    public bool customMapdata { get; set; }
    public int graveStoneTimer { get; set; }
    public int actionButtonTimer { get; set; }
    public int objectTimer { get; set; }
    public int reqY { get; set; }
    public int reqX { get; set; }
    public int animTimer { get; set; }
    public bool correctClient { get; set; }
    public int iA { get; set; }
    public bool waiting { get; set; }
    public int currentSlot { get; set; }
    public int inputId { get; set; } = -1;
    public int inputItemId { get; set; }
    public int inputItemIndex { get; set; }
    public object? magicNPC { get; set; }
    public int modernDamageDelay { get; set; } = -1;
    public object? pTrade { get; set; }
    public int modernMagicDelay { get; set; }
    public int geoffers { get; set; }
    public int secHit2 { get; set; }
    public int CV2 { get; set; }
    public int smithType { get; set; }
    public bool hasCannon { get; set; }
    public int[] cannonC { get; set; } = new int[2];
    public int[] sitems { get; set; } = new int[40];
    public int[] sitemsN { get; set; } = new int[1000];
    public int thirdHit2 { get; set; }
    public int fourHit2 { get; set; }
    public int clawTimer2 { get; set; }
    public bool UseClaws2 { get; set; }
    public int[] offerItem { get; set; } = new int[6];
    public int[] offerAmount { get; set; } = new int[6];
    public int[] currentAmount { get; set; } = new int[6];
    public int[] offerType { get; set; } = new int[6];
    public int[] offerPrice { get; set; } = new int[6];
    public int secHit { get; set; }
    public int fourHit { get; set; }
    public int[] partyA { get; set; } = new int[20];
    public int[] partyAN { get; set; } = new int[20];
    public bool party { get; set; }
    public int hasCollect { get; set; }
    public string[][] pgeSO { get; set; } = Array.Empty<string[]>();
    public string[][] pgeBO { get; set; } = Array.Empty<string[]>();
    public int modernSpell { get; set; }
    public string[][] collects { get; set; } = Array.Empty<string[]>();
    public bool usingAutoCast { get; set; }
    public bool castAuto { get; set; }
    public int randomSkill { get; set; }
    public int chosenSkill { get; set; }
    public int randomInt { get; set; }
    public bool IsShopping { get; set; }
    public bool geb { get; set; }
    public int MyShopID { get; set; }
    public bool UpdateShop { get; set; }
    public object? NPCS { get; set; }
    public int Update { get; set; }
    public int LoadedBackup { get; set; }
    public int zkc { get; set; }
    public int bkc { get; set; }
    public int akc { get; set; }
    public int skc { get; set; }
    public int DestroyItem { get; set; }
    public int DestroyItemSlot { get; set; }
    public int TradeWithPerson { get; set; }
    public int DFStimer { get; set; }
    public int BoatTimer { get; set; }
    public int ThunderTimer { get; set; }
    public int DragonTimer { get; set; }
    public int CrashTimer { get; set; }
    public int FadeTimer { get; set; }
    public string password2 { get; set; } = string.Empty;
    public int HeadTimer { get; set; } = -1;
    public bool swapAsNote { get; set; }
    public bool noteItems { get; set; }
    public bool logmessage { get; set; }
    public bool ClanGame { get; set; }
    public int[] destroyItem { get; set; } = Array.Empty<int>();
    public int[] destroyItemAmt { get; set; } = Array.Empty<int>();
    public int destroyItemInt { get; set; }
    public int[] SaraPeople { get; set; } = Array.Empty<int>();
    public int[] ZammyPeople { get; set; } = Array.Empty<int>();
    public int FightEnemys { get; set; }
    public int[] FightPeople { get; set; } = Array.Empty<int>();
    public int GotThere { get; set; }
    public int ClanTele { get; set; }
    public int JadTele { get; set; }
    public int[] ClanMember { get; set; } = Array.Empty<int>();
    public int ClanTimer { get; set; }
    public bool ClanReady { get; set; }
    public int ClanPartner { get; set; }
    public int ClanSide { get; set; }
    public int clanheight { get; set; }
    public int Opposing { get; set; }
    public bool ClanBattle { get; set; }
    public int ClanCount { get; set; }
    public int[] HouseObjects { get; set; } = Array.Empty<int>();
    public int[] HouseX { get; set; } = Array.Empty<int>();
    public int[] HouseY { get; set; } = Array.Empty<int>();
    public int Room0 { get; set; }
    public int Room1 { get; set; }
    public int Room2 { get; set; }
    public int Room3 { get; set; }
    public int Room4 { get; set; }
    public bool KickPlayers { get; set; }
    public int Rooms { get; set; }
    public bool buildMode { get; set; }
    public int RoomDir { get; set; }
    public int Room0Type { get; set; }
    public int Room1Type { get; set; }
    public int Room2Type { get; set; }
    public int Room3Type { get; set; }
    public int Room4Type { get; set; }
    public bool TalkAgent { get; set; }
    public bool DecorChange { get; set; }
    public int TeleBackTimer { get; set; } = -1;
    public int HLastX { get; set; }
    public int HLastY { get; set; }
    public int lastHeight { get; set; }
    public int Garden { get; set; }
    public int Garden1 { get; set; }
    public int Garden2 { get; set; }
    public int Garden3 { get; set; }
    public int Garden4 { get; set; }
    public int HX { get; set; }
    public int HY { get; set; }
    public int XremoveSlot { get; set; }
    public int XinterfaceID { get; set; }
    public int XremoveID { get; set; }
    public string LocatedAt { get; set; } = string.Empty;
    public int SlayerAm { get; set; }
    public bool Farm { get; set; }
    public int SlayerCaveTimer { get; set; } = -1;
    public int NPC { get; set; }
    public int wallTimer1 { get; set; }
    public int wallTimer2 { get; set; }
    public int wallTimer3 { get; set; }
    public int wallTimer4 { get; set; }
    public int wallTimer5 { get; set; }
    public int SwingTimer1 { get; set; }
    public int SwingTimer2 { get; set; }
    public int SwingTimer3 { get; set; }
    public int LogTimer { get; set; }
    public int NetTimer { get; set; }
    public int[] shop2 { get; set; } = Array.Empty<int>();
    public int[] shop2n { get; set; } = Array.Empty<int>();
    public int[] shop2p { get; set; } = Array.Empty<int>();
    public int[] shop3 { get; set; } = Array.Empty<int>();
    public int[] shop3n { get; set; } = Array.Empty<int>();
    public int[] shop3p { get; set; } = Array.Empty<int>();
    public int[] shop4 { get; set; } = Array.Empty<int>();
    public int[] shop4n { get; set; } = Array.Empty<int>();
    public int[] shop4p { get; set; } = Array.Empty<int>();
    public int[] shop5 { get; set; } = Array.Empty<int>();
    public int[] shop5n { get; set; } = Array.Empty<int>();
    public int[] shop5p { get; set; } = Array.Empty<int>();
    public int[] shop6 { get; set; } = Array.Empty<int>();
    public int[] shop6n { get; set; } = Array.Empty<int>();
    public int[] shop6p { get; set; } = Array.Empty<int>();
    public int[] shop7 { get; set; } = Array.Empty<int>();
    public int[] shop7n { get; set; } = Array.Empty<int>();
    public int[] shop7p { get; set; } = Array.Empty<int>();
    public int[] shop8 { get; set; } = Array.Empty<int>();
    public int[] shop8n { get; set; } = Array.Empty<int>();
    public int[] shop8p { get; set; } = Array.Empty<int>();
    public int[] shop9 { get; set; } = Array.Empty<int>();
    public int[] shop9n { get; set; } = Array.Empty<int>();
    public int[] shop9p { get; set; } = Array.Empty<int>();
    public int[] shop10 { get; set; } = Array.Empty<int>();
    public int[] shop10n { get; set; } = Array.Empty<int>();
    public int[] shop10p { get; set; } = Array.Empty<int>();
    public int[] shop11 { get; set; } = Array.Empty<int>();
    public int[] shop11n { get; set; } = Array.Empty<int>();
    public int[] shop11p { get; set; } = Array.Empty<int>();
    public int[] shop12 { get; set; } = Array.Empty<int>();
    public int[] shop12n { get; set; } = Array.Empty<int>();
    public int[] shop12p { get; set; } = Array.Empty<int>();
    public int[] shop13 { get; set; } = Array.Empty<int>();
    public int[] shop13n { get; set; } = Array.Empty<int>();
    public int[] shop13p { get; set; } = Array.Empty<int>();
    public int[] shop14 { get; set; } = Array.Empty<int>();
    public int[] shop14n { get; set; } = Array.Empty<int>();
    public int[] shop14p { get; set; } = Array.Empty<int>();
    public int[] shop16 { get; set; } = Array.Empty<int>();
    public int[] shop16n { get; set; } = Array.Empty<int>();
    public int[] shop16p { get; set; } = Array.Empty<int>();
    public int viewings { get; set; }
    public int PkTimer { get; set; }
    public int NewEmote { get; set; }
    public int followPlayer { get; set; }
    public int bountyOpp { get; set; }
    public int SkillCapes { get; set; }
    public int Cost { get; set; }
    public int ItemName { get; set; }
    public bool[] leveledUp { get; set; } = Array.Empty<bool>();
    public int leveledUpSkill { get; set; }
    public int skillMenu { get; set; }
    public int trainingSkill { get; set; }
    public int cwTimer { get; set; }
    public int cwzamTimer { get; set; }
    public int tPartner { get; set; }
    public int[] ShopItemCost { get; set; } = Array.Empty<int>();
    public int[] ShopItemsA { get; set; } = Array.Empty<int>();
    public bool[] tAccept { get; set; } = Array.Empty<bool>();
    public object? socket { get; set; }
    public WoodcuttingSkill? wc { get; set; }
    public MiningSkill? mi { get; set; }
    public object? frames { get; set; }
    public int posionHit1 { get; set; }
    public int posionHit2 { get; set; }
    public int[] skillLvlA { get; set; } = Array.Empty<int>();
    public int defLow { get; set; }
    public int strLow { get; set; }
    public int atkLow { get; set; }
    public int rangeLow { get; set; }
    public int mageLow { get; set; }
    public int defMid { get; set; }
    public int strMid { get; set; }
    public int atkMid { get; set; }
    public int rapidRestore { get; set; }
    public int rapidHeal { get; set; }
    public int protItems { get; set; }
    public int rangeMid { get; set; }
    public int mageMid { get; set; }
    public int defHigh { get; set; }
    public int strHigh { get; set; }
    public int atkHigh { get; set; }
    public int prayMage { get; set; }
    public int prayRange { get; set; }
    public int prayMelee { get; set; }
    public int rangeHigh { get; set; }
    public int mageHigh { get; set; }
    public int retribution { get; set; }
    public int redepmtion { get; set; }
    public int smite { get; set; }
    public int praySummon { get; set; }
    public int chivalry { get; set; }
    public int piety { get; set; }
    public bool showAttackOption { get; set; }
    public int deathMessage { get; set; }
    public int messageCount { get; set; }
    public int memberCount { get; set; }
    public int objectX { get; set; }
    public int objectY { get; set; }
    public int objectHeight { get; set; }
    public int Members { get; set; }
    public int Counted { get; set; }
    public int MyCount { get; set; }
    public int MyCount2 { get; set; }
    public int totalXP { get; set; }
    public int totalz { get; set; }
    public int DragTimer { get; set; }
    public string s1 { get; set; } = string.Empty;
    public string s2 { get; set; } = string.Empty;
    public string s3 { get; set; } = string.Empty;
    public string LastTickMessage { get; set; } = string.Empty;
    public int SpecialBarInterface { get; set; } = -1;
    public int SpecialBarChild { get; set; } = -1;

    // ══════════════════════════════════════════════════════════════════════════
    //  Methods (pure game-state logic, no network I/O)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initialise arrays to default values (called after construction / login).
    /// Mirrors the Java constructor's default array init.
    /// </summary>
    public void InitDefaults()
    {
        WalkingQueueX = new int[WalkingQueueSize];
        WalkingQueueY = new int[WalkingQueueSize];
        WalkingQueue = new int[WalkingQueueSize];

        // Initialise gathering skill instances (mirrors Java: wc = new Woodcutting(this))
        Woodcutting = new WoodcuttingSkill(this);
        Mining = new MiningSkill(this);
        Fishing = new FishingSkill(this);
        Cooking = new CookingSkill(this);
        Smithing = new SmithingSkill(this);
        Firemaking = new FiremakingSkill(this);
        Fletching = new FletchingSkill(this);
        Crafting = new CraftingSkill(this);
        Herblore = new HerbloreSkill(this);
        Runecrafting = new RunecraftingSkill(this);
        wc = Woodcutting;
        mi = Mining;

        // Default look (male)
        Look[0] = 0;  // Hair
        Look[1] = 10; // Beard
        Look[2] = 18; // Torso
        Look[3] = 26; // Arms
        Look[4] = 33; // Bracelets
        Look[5] = 36; // Legs
        Look[6] = 42; // Shoes

        // Default skill levels (Hitpoints = 10, rest = 1)
        for (int i = 0; i < SkillCount; i++)
        {
            SkillLvl[i] = 1;
            SkillLvlActual[i] = 1;
        }
        SkillLvl[3] = 10;   // Hitpoints
        SkillXP[3] = 1154;

        // Empty inventory / equipment / bank
        Array.Fill(Items, -1);
        Array.Fill(Equipment, -1);
        Array.Fill(BankItems, -1);
        Array.Fill(TradeItems, -1);
        Array.Fill(ShopItems, -1);
    }

    /// <summary>
    /// Calculate the level for a given skill from its XP.
    /// Mirrors Player.getLevelForXP(int).
    /// </summary>
    public int GetLevelForXP(int skillId)
    {
        int exp = SkillXP[skillId];
        int points = 0;

        for (int lvl = 1; lvl < 250; lvl++)
        {
            points += (int)Math.Floor(lvl + 300.0 * Math.Pow(2.0, lvl / 7.0));
            int output = (int)Math.Floor(points / 4.0);
            if (output - 1 >= exp)
                return lvl;
        }
        return 99;
    }

    /// <summary>
    /// Sum of all skill levels (total level).
    /// </summary>
    public int GetTotalLevel()
    {
        int total = 0;
        for (int i = 0; i < 24; i++)
            total += GetLevelForXP(i);
        return total;
    }

    /// <summary>
    /// Request an animation for this player (flag for next update cycle).
    /// </summary>
    public void RequestAnim(int animId, int animD)
    {
        AnimReq = animId;
        AnimDelay = animD;
        AnimUpdateReq = true;
        UpdateReq = true;
    }

    /// <summary>
    /// Request a graphic effect for this player.
    /// </summary>
    public void RequestGfx(int gfxId, int gfxD)
    {
        if (gfxD >= 100)
            gfxD += 6553500;
        GfxReq = gfxId;
        GfxDelay = gfxD;
        GfxUpdateReq = true;
        UpdateReq = true;
    }

    /// <summary>
    /// Request this player to face another entity.
    /// </summary>
    public void RequestFaceTo(int faceId)
    {
        FaceToReq = faceId;
        FaceToUpdateReq = true;
        UpdateReq = true;
    }

    /// <summary>
    /// Append a hit (damage) to this player.
    /// </summary>
    public void AppendHit(int damage, int poison)
    {
        if (damage > SkillLvl[3])
            damage = SkillLvl[3];

        UpdateHP(damage, heal: false);

        if (!Hit1UpdateReq)
        {
            HitDiff1 = damage;
            PoisonHit1 = poison;
            Hit1UpdateReq = true;
        }
        else
        {
            HitDiff2 = damage;
            PoisonHit2 = poison;
            Hit2UpdateReq = true;
        }
        UpdateReq = true;
    }

    /// <summary>
    /// Modify current hitpoints.
    /// </summary>
    public void UpdateHP(int amount, bool heal)
    {
        if (heal)
        {
            SkillLvl[3] += amount;
            int max = GetLevelForXP(3);
            if (SkillLvl[3] > max)
                SkillLvl[3] = max;
        }
        else
        {
            SkillLvl[3] -= amount;
            if (SkillLvl[3] <= 0)
            {
                SkillLvl[3] = 0;
                IsDead = true;
            }
        }
    }

    /// <summary>
    /// Add experience to a skill (handles level-up detection).
    /// </summary>
    public void AddSkillXP(double amount, int skill)
    {
        if (skill < 0 || skill >= SkillXP.Length)
            return;

        int oldLevel = GetLevelForXP(skill);
        SkillXP[skill] += (int)amount;
        int newLevel = GetLevelForXP(skill);

        if (oldLevel < newLevel)
        {
            if (skill == 3)
                UpdateHP(newLevel - oldLevel, heal: true);
            else
                SkillLvl[skill] += (newLevel - oldLevel);

            AppearanceUpdateReq = true;
            UpdateReq = true;
        }
    }

    /// <summary>
    /// Force chat text for this player.
    /// </summary>
    public void RequestForceChat(string text)
    {
        ForceChat = text;
        ForceChatUpdateReq = true;
        UpdateReq = true;
    }

    public void HomeTeleport(int teleX, int teleY)
    {
        if (HomeTele == 15)
        {
            RequestAnim(1722, 0);
        }
        else if (HomeTele == 14)
        {
            RequestAnim(1723, 0);
            RequestGfx(800, 0);
        }
        else if (HomeTele == 13)
        {
            RequestAnim(1724, 0);
            RequestGfx(801, 0);
        }
        else if (HomeTele == 12)
        {
            RequestAnim(1725, 0);
            RequestGfx(802, 0);
        }
        else if (HomeTele == 11)
        {
            RequestAnim(2798, 0);
            RequestGfx(1703, 0);
        }
        else if (HomeTele == 10)
        {
            RequestAnim(2799, 0);
            RequestGfx(1704, 0);
        }
        else if (HomeTele == 9)
        {
            RequestAnim(2800, 0);
            RequestGfx(1705, 0);
        }
        else if (HomeTele == 8)
        {
            RequestAnim(4847, 0);
            RequestGfx(1706, 0);
        }
        else if (HomeTele == 7)
        {
            RequestAnim(4848, 0);
            RequestGfx(1707, 0);
        }
        else if (HomeTele == 6)
        {
            RequestAnim(4849, 0);
            RequestGfx(1708, 0);
        }
        else if (HomeTele == 5)
        {
            RequestAnim(4849, 0);
            RequestGfx(1709, 0);
        }
        else if (HomeTele == 4)
        {
            RequestAnim(4849, 0);
            RequestGfx(1710, 0);
        }
        else if (HomeTele == 3)
        {
            RequestAnim(4850, 0);
            RequestGfx(1711, 0);
        }
        else if (HomeTele == 2)
        {
            RequestAnim(4851, 0);
            RequestGfx(1712, 0);
        }
        else if (HomeTele == 1)
        {
            RequestAnim(4852, 0);
            RequestGfx(1713, 0);
            SetCoords(teleX, teleY, 0);
            NormalHomeTele = false;
            AncientsHomeTele = false;
            HomeTeleDelay = 3600;
            HomeTele = 15;
        }
    }

    /// <summary>
    /// Set the player's absolute coordinates (instant teleport).
    /// </summary>
    public void SetCoords(int x, int y, int height)
    {
        TeleportToX = x;
        TeleportToY = y;
        HeightLevel = height;
        DidTeleport = true;
    }

    /// <summary>
    /// Check if position is in the Wilderness.
    /// </summary>
    public static bool IsWildernessArea(int absX, int absY)
    {
        return (absX >= 2363 && absY >= 3071 && absX <= 2432 && absY <= 3135)
            || (absX >= 2370 && absY >= 5128 && absX <= 2426 && absY <= 5167)
            || (absX >= 2940 && absX <= 3395 && absY >= 3524 && absY <= 4000)
            || (absX >= 3362 && absY >= 3228 && absX <= 3391 && absY <= 3241);
    }

    /// <summary>
    /// Calculate equipment bonuses from equipped items.
    /// Item bonus lookup will be delegated to an IItemDefinitionProvider in future phases.
    /// </summary>
    public void CalculateEquipmentBonus()
    {
        Array.Clear(EquipmentBonus);
    }

    /// <summary>
    /// Determine the player who did the most damage for kill attribution.
    /// </summary>
    public int GetPlayerKiller()
    {
        int killer = 0;
        int count = 0;
        for (int i = 1; i < KilledBy.Length; i++)
        {
            if (killer == 0)
            {
                killer = i;
                count = 1;
            }
            else if (KilledBy[i] > KilledBy[killer])
            {
                killer = i;
                count = 1;
            }
            else if (KilledBy[i] == KilledBy[killer])
            {
                count++;
            }
        }
        if (count > 1)
            killer = PlayerId;
        return killer;
    }

    /// <summary>
    /// Reset all skilling timers.
    /// </summary>
    public void ResetSkillTimers()
    {
        CookTimer = -1; CookAmount = 0; CookXP = 0; CookGet = 0; CookId = 0;
        FishTimer = -1; FishAmount = 0; FishXP = 0; FishGet = 0; FishEmote = 0;
        FletchTimer = -1; FletchAmount = 0; FletchXP = 0; FletchGet = 0; FletchId = 0;
        SmithingTimer = -1; SmithingAmount = 0; SmithingXP = 0; SmithingGet = 0; SmithingId = 0;
    }

    /// <summary>
    /// Reset duel state.
    /// </summary>
    public void ResetDuel()
    {
        DuelReady = false;
        DuelPartner = 0;
        DuelCan = false;
        DuelTimer = -1;
        SkulledDelay = -1;
    }
}
