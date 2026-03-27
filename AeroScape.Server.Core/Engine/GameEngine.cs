using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Combat;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Items;
using AeroScape.Server.Core.Movement;
using AeroScape.Server.Core.Services;
using AeroScape.Server.Core.Messages;

namespace AeroScape.Server.Core.Engine;

/// <summary>
/// Core game engine — a hosted service that drives the 600ms game tick.
/// Translated from DavidScape/Engine.java.
///
/// Responsibilities:
///   • Manages the player and NPC arrays.
///   • Runs the main game loop at ~600ms per major tick (with a 100ms packet sub-tick).
///   • Processes player per-tick logic, NPC per-tick logic, death/respawn cycles.
///   • Decoupled from networking: delegates packet I/O to injected services.
///
/// Network integration (packet parsing, update encoding, socket writes) will be
/// wired in during Phase 6 via the IPlayerUpdateService / INpcUpdateService interfaces.
/// </summary>
public class GameEngine : BackgroundService
{
    // ── Constants ────────────────────────────────────────────────────────────
    public const int MaxPlayers = 2000;
    public const int MaxNpcs = 50000;
    public const int MaxListedNpcs = 8041;

    /// <summary>Major tick interval (entity updates, combat, skills).</summary>
    public static readonly TimeSpan MajorTickInterval = TimeSpan.FromMilliseconds(600);

    /// <summary>Minor tick interval (packet processing).</summary>
    public static readonly TimeSpan MinorTickInterval = TimeSpan.FromMilliseconds(100);

    // ── World state ─────────────────────────────────────────────────────────
    /// <summary>Player slots. Index 0 is unused (client can't handle id 0).</summary>
    public Player?[] Players { get; } = new Player?[MaxPlayers];

    /// <summary>Lock object for thread-safe access to Players array.</summary>
    private readonly object _playersLock = new object();

    /// <summary>NPC slots. Index 0 is unused.</summary>
    public NPC?[] Npcs { get; } = new NPC?[MaxNpcs];
    public Dictionary<int, NpcDefinition> NpcDefinitions { get; } = new();
    public List<NpcSpawnDefinition> NpcSpawns { get; } = new();
    public List<LoadedObject> LoadedObjects { get; } = new();

    // ── Minigame / global timers (translated from Engine.java statics) ──────
    public int FightPitTimer { get; set; } = 120;
    public int PlayersInGame { get; set; }
    public int CWarsTimer { get; set; } = 240;
    public int CWGameTime { get; set; } = -1;
    public int SaradominScore { get; set; }
    public int ZamorakScore { get; set; }
    public int SaradominTeam { get; set; }
    public int ZamorakTeam { get; set; }
    public bool SaradominFlag { get; set; }
    public bool ZamorakFlag { get; set; }
    public int SaradominP { get; set; }
    public int ZamorakP { get; set; }

    private readonly ILogger<GameEngine> _logger;
    public IGameUpdateService? GameUpdateService { get; set; }
    private readonly WalkQueue _walkQueue;
    private readonly ShopService _shops;
    private readonly PrayerService _prayers;
    private readonly DeathService _deaths;
    private readonly LegacyFileManager _fileManager;
    private readonly NpcSpawnLoader _npcSpawnLoader;
    private readonly ObjectLoaderService _objectLoader;
    private readonly GroundItemManager _groundItems;
    private readonly NPCInteractionService _npcInteractionService;
    private bool _worldLoaded;

    // ── Combat services ─────────────────────────────────────────────────────
    public PlayerVsPlayerCombat PlayerCombat { get; }
    public PlayerVsNpcCombat PlayerNpcCombat { get; }
    public NpcVsPlayerCombat NpcPlayerCombat { get; }

    public GameEngine(
        ILogger<GameEngine> logger,
        ILogger<PlayerVsPlayerCombat> pvpLogger,
        ILogger<PlayerVsNpcCombat> pveLogger,
        ILogger<NpcVsPlayerCombat> npcLogger,
        WalkQueue walkQueue,
        ShopService shops,
        PrayerService prayers,
        DeathService deaths,
        LegacyFileManager fileManager,
        NpcSpawnLoader npcSpawnLoader,
        ObjectLoaderService objectLoader,
        GroundItemManager groundItems,
        PlayerItemsService playerItems,
        NPCInteractionService npcInteractionService)
    {
        _logger = logger;
        _walkQueue = walkQueue;
        _shops = shops;
        _prayers = prayers;
        _deaths = deaths;
        _fileManager = fileManager;
        _npcSpawnLoader = npcSpawnLoader;
        _objectLoader = objectLoader;
        _groundItems = groundItems;
        _npcInteractionService = npcInteractionService;
        PlayerCombat = new PlayerVsPlayerCombat(this, pvpLogger);
        PlayerNpcCombat = new PlayerVsNpcCombat(this, pveLogger, playerItems);
        NpcPlayerCombat = new NpcVsPlayerCombat(this, npcLogger);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Player management
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Find the first free player slot (starting from 1).
    /// Returns -1 if the server is full.
    /// </summary>
    public int FindFreePlayerSlot()
    {
        for (int i = 1; i < Players.Length; i++)
        {
            if (Players[i] == null)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Register a player in the world at the given slot.
    /// </summary>
    public bool AddPlayer(Player player, int slot)
    {
        if (slot <= 0 || slot >= Players.Length)
            return false;

        lock (_playersLock)
        {
            if (Players[slot] != null)
                return false;

            player.PlayerId = slot;
            Players[slot] = player;
        }
        return true;
    }

    /// <summary>
    /// Remove a player from the world (save should happen before calling this).
    /// Mirrors Engine.removePlayer(int).
    /// </summary>
    public void RemovePlayer(int id)
    {
        if (id <= 0 || id >= Players.Length)
            return;

        lock (_playersLock)
        {
            if (Players[id] == null)
                return;
            Players[id] = null;
        }
    }

    /// <summary>
    /// Get the online player count.
    /// </summary>
    public int GetPlayerCount()
    {
        int count = 0;
        for (int i = 1; i < Players.Length; i++)
        {
            if (Players[i] != null)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Find a player index by username (case-insensitive).
    /// Returns 0 if not found.
    /// </summary>
    public int GetIdFromName(string playerName)
    {
        var normalized = playerName.Replace('_', ' ');
        for (int i = 1; i < Players.Length; i++)
        {
            var p = Players[i];
            if (p != null && string.Equals(p.Username, normalized, StringComparison.OrdinalIgnoreCase))
                return p.PlayerId;
        }
        return 0;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  NPC management
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Spawn a new NPC. Returns the index it was placed at, or -1 if full.
    /// Mirrors Engine.newNPC(...).
    /// </summary>
    public int SpawnNpc(int type, int absX, int absY, int height,
                        int mRX1, int mRY1, int mRX2, int mRY2,
                        bool needsRespawn)
    {
        int index = -1;
        for (int i = 1; i < Npcs.Length; i++)
        {
            if (Npcs[i] == null)
            {
                index = i;
                break;
            }
        }

        if (index == -1)
        {
            _logger.LogWarning("Max number of NPCs spawned.");
            return -1;
        }

        var n = new NPC(type, index)
        {
            AbsX = absX,
            AbsY = absY,
            MakeX = absX,
            MakeY = absY,
            HeightLevel = height,
            MoveRangeX1 = mRX1,
            MoveRangeY1 = mRY1,
            MoveRangeX2 = mRX2,
            MoveRangeY2 = mRY2,
            NeedsRespawn = needsRespawn,
        };
        n.RequestFaceCoords(n.AbsX, n.AbsY - 1);

        _npcSpawnLoader.ApplyDefinition(n, NpcDefinitions);

        Npcs[index] = n;
        return index;
    }

    /// <summary>
    /// Flag all players to rebuild their local NPC list.
    /// Called after an NPC is removed.
    /// </summary>
    public void RebuildNpcs()
    {
        for (int i = 1; i < Players.Length; i++)
        {
            var p = Players[i];
            if (p != null)
                p.RebuildNPCList = true;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Utility
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Check if coordinates are in the Wilderness.</summary>
    public static bool WildernessArea(int absX, int absY)
        => Player.IsWildernessArea(absX, absY);

    /// <summary>Check if coordinates are in the Castle Wars area.</summary>
    public static bool CastleWarsArea(int absX, int absY)
        => absX >= 2368 && absX <= 2428 && absY >= 3072 && absY <= 3132;

    // ══════════════════════════════════════════════════════════════════════════
    //  Main game loop
    // ══════════════════════════════════════════════════════════════════════════

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GameEngine starting — tick interval {Interval}ms", MajorTickInterval.TotalMilliseconds);
        EnsureWorldLoaded();

        var stopwatch = new Stopwatch();

        while (!stoppingToken.IsCancellationRequested)
        {
            stopwatch.Restart();

            try
            {
                ProcessMajorTick();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during game tick");
            }

            stopwatch.Stop();
            var elapsed = stopwatch.Elapsed;
            var sleepTime = MajorTickInterval - elapsed;

            if (sleepTime > TimeSpan.Zero)
                await Task.Delay(sleepTime, stoppingToken);
            else
                _logger.LogWarning("Game tick took {Elapsed}ms (exceeded {Target}ms)",
                    elapsed.TotalMilliseconds, MajorTickInterval.TotalMilliseconds);
        }

        _logger.LogInformation("GameEngine stopped.");
    }

    /// <summary>
    /// One full 600ms game tick.
    /// Mirrors the inner loop of Engine.run() after the 600ms gate.
    /// </summary>
    private void ProcessMajorTick()
    {
        EnsureWorldLoaded();

        // ── 1. Global timers ────────────────────────────────────────────────
        ProcessGlobalTimers();
        _groundItems.Process();
        _shops.Process(this);

        // ── 2. Player per-tick processing & movement ────────────────────────
        for (int i = 1; i < Players.Length; i++)
        {
            var p = Players[i];
            if (p == null || !p.Online)
                continue;

            // Handle disconnection
            if (p.Disconnected[0] && p.Disconnected[1])
            {
                RemovePlayer(p.PlayerId);
                continue;
            }

            // Tab interface restoration (mirrors Java Engine.java lines 273-278)
            if (p.InterfaceId != 762 &&
                p.InterfaceId != 335 &&
                p.InterfaceId != 334 &&
                p.InterfaceId != 620)
            {
                // RestoreTabs functionality removed - method doesn't exist in interface
            }

            // Per-player tick (timers, skills, combat delays, death, etc.)
            ProcessPlayerTick(p);

            _walkQueue.Process(p);
        }

        // ── 3. Player update encoding ───────────────────────────────────────
        for (int i = 1; i < Players.Length; i++)
        {
            var p = Players[i];
            if (p == null || !p.Online)
                continue;

            GameUpdateService?.SendPlayerAndNpcUpdates(p);
        }

        // ── 4. Clear player update masks ────────────────────────────────────
        for (int i = 1; i < Players.Length; i++)
        {
            var p = Players[i];
            if (p == null || !p.Online)
                continue;

            if (GameUpdateService != null)
                GameUpdateService.ClearPlayerUpdateReqs(p);
            else
                ClearPlayerUpdateReqs(p);
        }

        // ── 5. NPC processing ───────────────────────────────────────────────
        for (int i = 1; i < Npcs.Length; i++)
        {
            var n = Npcs[i];
            if (n == null)
                continue;

            if (GameUpdateService != null)
                GameUpdateService.ClearNpcUpdateMasks(n);
            else
                n.ClearUpdateMasks();
        }

        for (int i = 1; i < Npcs.Length; i++)
        {
            var n = Npcs[i];
            if (n == null)
                continue;

            n.Process(Players);

            // NPC-vs-Player combat (mirrors Java: NPC.process() → Engine.npcPlayerCombat.attackPlayer)
            // Combat should be processed before death state checks to allow final attacks
            if (n.AttackingPlayer)
                NpcPlayerCombat.ProcessAttack(n);

            if (!n.IsDead)
            {
                // NPC random walk logic (mirrors Java Engine.java lines 326-330)
                if (n.RandomWalk && !n.AttackingPlayer)
                {
                    GameUpdateService?.ProcessNpcRandomWalk(n);
                }
            }
            else
            {
                ProcessNpcDeath(n, i);
            }
        }
    }

    /// <summary>
    /// Process global minigame / event timers.
    /// </summary>
    private void ProcessGlobalTimers()
    {
        // Update player count (mirrors Java Engine constPlayers tracking)
        // Thread-safe player count with proper synchronization
        int count = 0;
        lock (_playersLock) // Use dedicated lock object for thread safety
        {
            for (int i = 1; i < Players.Length; i++)
            {
                var p = Players[i];
                if (p != null && p.Online)
                    count++;
            }
            PlayersInGame = count;
        }
        
        if (FightPitTimer > 0) FightPitTimer--;
        if (FightPitTimer == 0) FightPitTimer = -1;
        if (FightPitTimer == -1 && PlayersInGame == 0) FightPitTimer = 120;

        if (CWarsTimer > 0) CWarsTimer--;
        if (CWarsTimer == 0)
        {
            CWarsTimer = -1;
            CWGameTime = 600;
            ZamorakScore = 0;
            SaradominScore = 0;
        }
        if (CWGameTime > 0) CWGameTime--;
        if (CWGameTime == 0)
        {
            CWarsTimer = 240;
            CWGameTime = -1;
        }

        var zamorakCarrier = ZamorakP > 0 && ZamorakP < Players.Length ? Players[ZamorakP] : null;
        if (zamorakCarrier == null || !zamorakCarrier.Online)
        {
            ZamorakFlag = false;
            ZamorakP = 0;
        }
        else if (ZamorakP > 0)
        {
            ZamorakFlag = true;
        }

        var saradominCarrier = SaradominP > 0 && SaradominP < Players.Length ? Players[SaradominP] : null;
        if (saradominCarrier == null || !saradominCarrier.Online)
        {
            SaradominFlag = false;
            SaradominP = 0;
        }
        else if (SaradominP > 0)
        {
            SaradominFlag = true;
        }
    }

    private void EnsureWorldLoaded()
    {
        if (_worldLoaded)
            return;

        string npcListPath = Path.Combine(Directory.GetCurrentDirectory(), "legacy-java", "server508", "data", "npcs", "npclist.cfg");
        string npcSpawnPath = Path.Combine(Directory.GetCurrentDirectory(), "legacy-java", "server508", "data", "npcs", "npcspawn.cfg");
        string objectPath = Path.Combine(Directory.GetCurrentDirectory(), "legacy-java", "server508", "data", "objects.cfg");

        foreach (var pair in _npcSpawnLoader.LoadDefinitions(npcListPath))
            NpcDefinitions[pair.Key] = pair.Value;

        NpcSpawns.AddRange(_npcSpawnLoader.LoadSpawns(npcSpawnPath));
        foreach (var spawn in NpcSpawns)
            SpawnNpc(spawn.NpcType, spawn.X, spawn.Y, spawn.Height, spawn.MoveRangeX1, spawn.MoveRangeY1, spawn.MoveRangeX2, spawn.MoveRangeY2, true);

        LoadedObjects.Clear();
        LoadedObjects.AddRange(_objectLoader.LoadFile(objectPath));

        _worldLoaded = true;
    }

    /// <summary>
    /// Per-player tick — decrements timers, handles stat restore, energy, special, etc.
    /// A simplified translation of Player.process() focused on core state management.
    /// Minigame-specific logic (Fight Pits, Castle Wars) will expand in future phases.
    /// </summary>
    private void ProcessPlayerTick(Player p)
    {
        // Save timer
        if (p.SaveTimer > 0)
        {
            p.SaveTimer--;
        }
        else
        {
            _fileManager.SaveCharacterSnapshot(p);
            p.SaveTimer = 10;
        }

        if (p.JailTimer > 0)
        {
            p.JailTimer--;
        }
        if (p.JailTimer == 0)
        {
            p.RequestForceChat("I have been jailed for breaking the rules.");
            p.JailTimer = 20;
        }

        if (p.AbsX == p.ReqX)
        {
            p.ReqX = -1;
        }
        if (p.AbsY == p.ReqY)
        {
            p.ReqY = -1;
        }

        // Combat delays
        if (p.CombatDelay > 0) p.CombatDelay--;
        if (p.NpcDelay > 0) p.NpcDelay--;
        if (p.ClickDelay > 0) p.ClickDelay--;
        if (p.AttackDelay > 0) p.AttackDelay--;
        if (p.EatDelay > 0) p.EatDelay--;
        if (p.BuryDelay > 0) p.BuryDelay--;
        if (p.DrinkDelay > 0) p.DrinkDelay--;
        if (p.MagicDelay > 0) p.MagicDelay--;

        // Freeze
        if (p.FreezeDelay > 0)
        {
            p.FreezeDelay--;
            _walkQueue.StopMovement(p);
        }

        // Process deferred NPC interactions
        ProcessDeferredNpcOptions(p);

        // Skull
        if (p.SkulledDelay > 0)
        {
            p.SkulledDelay--;
            p.SkulledUpdateReq = true;
        }

        // Jump delay (wilderness ditch)
        if (p.JumpDelay > 0)
        {
            p.JumpDelay--;
            p.JumpUpdateReq = true;
        }

        // Run energy
        if (p.RunEnergyDelay > 0)
        {
            p.RunEnergyDelay--;
        }
        else
        {
            if (p.RunEnergy < 100)
            {
                p.RunEnergy++;
                p.RunEnergyUpdateReq = true;
            }
            p.RunEnergyDelay = 4;
        }
        if (p.RunEnergy == 0)
            p.IsRunning = false;

        // Special attack energy
        if (p.SpecialAmountDelay > 0)
        {
            p.SpecialAmountDelay--;
        }
        else
        {
            if (p.SpecialAmount < 100)
            {
                p.SpecialAmount++;
                p.SpecialAmountUpdateReq = true;
            }
            p.SpecialAmountDelay = 2;
        }

        // Stat restore
        if (p.StatRestoreDelay > 0)
        {
            p.StatRestoreDelay--;
        }
        else
        {
            for (int i = 0; i < Player.SkillCount; i++)
            {
                int xpLvl = p.GetLevelForXP(i);
                if (p.SkillLvl[i] < xpLvl)
                    p.SkillLvl[i]++;
                else if (p.SkillLvl[i] > xpLvl)
                    p.SkillLvl[i]--;
            }
            p.StatRestoreDelay = 75;
        }

        // Prayer drain
        p.PrayerDrain -= p.DrainRate;
        if (p.PrayerDrain <= 0 && p.SkillLvl[5] > 0)
        {
            p.SkillLvl[5]--;
            p.UpdateReq = true;
            if (p.SkillLvl[5] <= 0)
            {
                _prayers.Reset(p);
                p.LastTickMessage = "You have run out of prayer points.";
            }
            p.PrayerDrain = 100;
        }

        // Teleport delay
        if (p.TeleDelay > 0) p.TeleDelay--;
        if (p.TeleDelay == 0)
        {
            p.TeleDelay = -1;
            p.SetCoords(p.TeleX, p.TeleY, p.HeightLevel);
            p.RequestAnim(p.TeleFinishAnim, 0);
            p.RequestGfx(p.TeleFinishGfx, p.TeleFinishGfxHeight);
            p.TeleX = p.TeleY = -1;
        }

        // Death
        if (p.IsDead)
            _deaths.ProcessPlayerDeath(p);

        // Magic cast cooldown reset
        if (p.MagicDelay <= 0 && !p.MagicCanCast)
            p.MagicCanCast = true;

        // Dragon claws multi-hit timer
        if (p.ClawTimer > 0)
        {
            p.ClawTimer--;
            if (p.ClawTimer <= 0 && p.UseClaws)
            {
                if (p.AttackPlayer > 0 && p.AttackPlayer < MaxPlayers)
                {
                    var target = Players[p.AttackPlayer];
                    // Check target validity and death state before timer execution
                    if (target != null && !target.IsDead && target.Online)
                    {
                        target.AppendHit(p.ThirdHit, 0);
                        target.AppendHit(p.FourthHit, 0);
                    }
                }
                p.UseClaws = false;
                // Clear attack target after claws finish to prevent state leaks
                p.AttackPlayer = 0;
            }
        }

        // Home teleport sequence
        if (p.HomeTeleDelay > 0) p.HomeTeleDelay--;
        if (p.HomeTele > 0 && p.HomeTeleDelay <= 0 && p.NormalHomeTele)
        {
            p.HomeTeleport(3221, 3221);
            p.HomeTele--;
        }
        if (p.HomeTele > 0 && p.HomeTeleDelay <= 0 && p.AncientsHomeTele)
        {
            p.HomeTeleport(3222, 3219);
            p.HomeTele--;
        }
        if (p.YellTimer > 0) p.YellTimer--;
        if (p.SuggestionTimer > 0) p.SuggestionTimer--;

        // Gravestone timer
        if (p.graveStoneTimer > 0)
        {
            p.graveStoneTimer--;
            if (p.graveStoneTimer == 0)
            {
                // Remove gravestone from loaded objects
                lock (LoadedObjects)
                {
                    LoadedObjects.RemoveAll(o => 
                        o.ObjectId == 12719 && o.X == p.gsX && o.Y == p.gsY);
                }
                p.graveStoneTimer = -1;
            }
        }

        // ── Gathering skill processing ─────────────────────────────────────
        // These tick-driven skills were processed in Player.process() in the Java code.
        // Woodcutting and Mining use their own internal timers via GatheringSkillBase.
        // Fishing, Cooking, and Fletching use the player's timer fields.
        p.Woodcutting?.Process();
        p.Mining?.Process();
        p.Fishing?.Process();
        p.Cooking?.Process();
        p.Fletching?.Process();

        // Skilling timers (legacy fields still decremented for compatibility)
        if (p.HerbloreTimer > 0) p.HerbloreTimer--;
        if (p.AgilityTimer > 0) p.AgilityTimer--;
        if (p.ActionTimer > 0) p.ActionTimer--;
        if (p.FireDelay > 0) p.FireDelay--;

        // Duel timer
        if (p.DuelTimer > 0) p.DuelTimer--;

        // Disconnection forwarding
        if (p.Disconnected[0])
        {
            if (p.TradePlayer > 0)
            {
                DeclineTradeForDisconnect(p);
            }
            p.Disconnected[1] = true;
        }

        // ── Combat dispatch (mirrors Java Player.process() → Engine.playerCombat / playerNPCCombat) ──
        if (p.AttackingPlayer)
            PlayerCombat.ProcessAttack(p);

        if (p.AttackingNPC)
            PlayerNpcCombat.ProcessAttack(p);

        if (p.AfterDeathUpdateReq)
        {
            _deaths.RestoreAfterDeath(p);
        }

        // Level-up detection
        for (int i = 0; i < Player.SkillCount; i++)
        {
            int currentMaxLevel = p.GetLevelForXP(i);
            if (currentMaxLevel > p.SkillLvlActual[i])
            {
                p.RequestGfx(199, 100);
                p.SkillLvlActual[i] = currentMaxLevel;
                // Level-up message will be sent by the frame/network layer
            }
        }

        if (p.RunEnergyUpdateReq)
        {
            p.UpdateReq = true;
            p.RunEnergyUpdateReq = false;
        }

        if (p.SpecialAmountUpdateReq)
        {
            p.UpdateReq = true;
            p.SpecialAmountUpdateReq = false;
        }

        if (p.SkulledUpdateReq && !IsAtDuel(p))
        {
            if (p.SkulledDelay >= 1)
            {
                p.PkIcon = 0;
                p.UpdateReq = true;
                p.AppearanceUpdateReq = true;
            }
            if (p.SkulledDelay <= 0)
            {
                p.PkIcon = -1;
                p.SkulledDelay = 0;
                p.UpdateReq = true;
                p.AppearanceUpdateReq = true;
            }

            p.SkulledUpdateReq = false;
        }

        if (p.ClickDelay == 0)
        {
            p.ClickDelay = -1;
        }
    }

    /// <summary>
    /// Clear transient update flags on a player after the update packet is sent.
    /// Mirrors PlayerUpdate.clearUpdateReqs.
    /// </summary>
    private static void ClearPlayerUpdateReqs(Player p)
    {
        p.UpdateReq = false;
        p.AppearanceUpdateReq = false;
        p.ChatTextUpdateReq = false;
        p.AnimUpdateReq = false;
        p.GfxUpdateReq = false;
        p.FaceToUpdateReq = false;
        p.Hit1UpdateReq = false;
        p.Hit2UpdateReq = false;
        p.ForceChatUpdateReq = false;
    }

    private static bool IsAtDuel(Player p)
        => p.AbsX >= 3362 && p.AbsX <= 3391 && p.AbsY >= 3228 && p.AbsY <= 3241;

    /// <summary>
    /// Handle NPC death / respawn cycle.
    /// Mirrors the death logic in Engine.run().
    /// </summary>
    private void ProcessNpcDeath(NPC n, int index)
    {
        _deaths.ProcessNpcDeath(n);

        if (n.HiddenNPC && n.RespawnDelay <= 0)
        {
            if (n.NeedsRespawn)
            {
                SpawnNpc(n.NpcType, n.MakeX, n.MakeY,
                         n.HeightLevel, n.MoveRangeX1,
                         n.MoveRangeY1, n.MoveRangeX2,
                         n.MoveRangeY2, true);
            }
            Npcs[index] = null;
            RebuildNpcs();
        }
    }

    private void DeclineTradeForDisconnect(Player player)
    {
        var partner = player.TradePlayer > 0 && player.TradePlayer < Players.Length ? Players[player.TradePlayer] : null;
        ReturnTradeItems(player);
        ResetTradeState(player);
        if (partner is not null)
        {
            ReturnTradeItems(partner);
            ResetTradeState(partner);
        }
    }

    private static void ReturnTradeItems(Player player)
    {
        for (var i = 0; i < player.TradeItems.Length; i++)
        {
            if (player.TradeItems[i] < 0 || player.TradeItemsN[i] <= 0)
            {
                continue;
            }

            var slot = Array.IndexOf(player.Items, -1);
            if (slot < 0)
            {
                break;
            }

            player.Items[slot] = player.TradeItems[i];
            player.ItemsN[slot] = player.TradeItemsN[i];
        }
    }

    private static void ResetTradeState(Player player)
    {
        Array.Fill(player.TradeItems, -1);
        Array.Fill(player.TradeItemsN, 0);
        player.TradeAccept[0] = false;
        player.TradeAccept[1] = false;
        player.TradePlayer = 0;
        player.TradeStage = 0;
        player.InterfaceId = -1;
    }

    /// <summary>
    /// Processes deferred NPC option interactions.
    /// When a player has a pending NPC option and walks into range, trigger the deferred action.
    /// This matches the Java pattern where npcOption1/2/3 flags keep the action pending until adjacency.
    /// </summary>
    private void ProcessDeferredNpcOptions(Player player)
    {
        // Check for pending NPC options and process them if the player is now in range
        if (player.NpcOption1 && ProcessDeferredNpcOption(player, 1))
        {
            player.NpcOption1 = false;
        }
        
        if (player.NpcOption2 && ProcessDeferredNpcOption(player, 2))
        {
            player.NpcOption2 = false;
        }
        
        if (player.NpcOption3 && ProcessDeferredNpcOption(player, 3))
        {
            player.NpcOption3 = false;
        }
    }

    /// <summary>
    /// Processes a specific deferred NPC option if the player is in range.
    /// Returns true if the option was processed (and should be cleared).
    /// </summary>
    private bool ProcessDeferredNpcOption(Player player, int optionNumber)
    {
        if (player.ClickId <= 0 || player.ClickId >= Npcs.Length)
            return true; // Invalid NPC, clear the pending option

        var npc = Npcs[player.ClickId];
        if (npc is null)
            return true; // NPC no longer exists, clear the pending option

        // Check if player is adjacent to the NPC
        if (CombatFormulas.GetDistance(player.AbsX, player.AbsY, npc.AbsX, npc.AbsY) <= 1)
        {
            // Player is in range, trigger the deferred action using the interaction service
            switch (optionNumber)
            {
                case 1:
                    _npcInteractionService.HandleNPCOption1(player, npc);
                    break;
                case 2:
                    _npcInteractionService.HandleNPCOption2(player, npc);
                    break;
                case 3:
                    _npcInteractionService.HandleNPCOption3(player, npc);
                    break;
            }
            return true; // Processed, clear the pending option
        }

        return false; // Not in range yet, keep the option pending
    }
}
