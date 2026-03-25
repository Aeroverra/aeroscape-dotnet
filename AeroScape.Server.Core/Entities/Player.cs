using System;
using System.Collections.Generic;

namespace AeroScape.Server.Core.Entities;

/// <summary>
/// Runtime game-state for a connected player.
/// Translated from DavidScape/players/Player.java — network-layer independent.
/// Persistent data lives in DbPlayer; this is the live, in-memory representation.
/// </summary>
public class Player
{
    // ── Identity / Slot ─────────────────────────────────────────────────────
    public int PlayerId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Rights { get; set; }
    public bool Online { get; set; }
    public bool[] Disconnected { get; set; } = new bool[2];

    // ── Position & Movement ─────────────────────────────────────────────────
    public int AbsX { get; set; }
    public int AbsY { get; set; }
    public int HeightLevel { get; set; }
    public int MapRegionX { get; set; }
    public int MapRegionY { get; set; }
    public int CurrentX { get; set; }
    public int CurrentY { get; set; }

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
    public const int BankSize = 500;
    public int[] BankItems { get; set; } = new int[BankSize];
    public int[] BankItemsN { get; set; } = new int[BankSize];
    public int[] TabStartSlot { get; set; } = new int[11];
    public int BankX { get; set; } = 50;
    public int ViewingBankTab { get; set; } = 10;
    public bool WithdrawNote { get; set; }
    public bool InsertMode { get; set; }

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
    public int PlayerListSize { get; set; }
    public bool RebuildNPCList { get; set; }
    public int NpcListSize { get; set; }

    // ── Interfaces ──────────────────────────────────────────────────────────
    public int InterfaceId { get; set; } = -1;
    public int ChatboxInterfaceId { get; set; } = -1;

    // ── Click state ─────────────────────────────────────────────────────────
    public int ClickX { get; set; }
    public int ClickY { get; set; }
    public int ClickId { get; set; }

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
