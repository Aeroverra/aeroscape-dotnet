using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Entities;

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

    /// <summary>NPC slots. Index 0 is unused.</summary>
    public NPC?[] Npcs { get; } = new NPC?[MaxNpcs];

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

    public GameEngine(ILogger<GameEngine> logger)
    {
        _logger = logger;
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
        if (Players[slot] != null)
            return false;

        player.PlayerId = slot;
        Players[slot] = player;
        return true;
    }

    /// <summary>
    /// Remove a player from the world (save should happen before calling this).
    /// Mirrors Engine.removePlayer(int).
    /// </summary>
    public void RemovePlayer(int id)
    {
        if (id <= 0 || id >= Players.Length || Players[id] == null)
            return;

        Players[id] = null;
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

        // TODO: Phase 7+ — apply NPC definition stats from config
        // (name, combatLevel, maxHP, maxHit, atkType, weakness, emotes, etc.)

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
        // ── 1. Global timers ────────────────────────────────────────────────
        ProcessGlobalTimers();

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

            // Per-player tick (timers, skills, combat delays, death, etc.)
            ProcessPlayerTick(p);

            // Movement will be handled by PlayerMovement service (Phase 6+)
        }

        // ── 3. Player update encoding (stub — Phase 6) ─────────────────────
        // PlayerUpdate.update(p) → will be called from network layer

        // ── 4. Clear player update masks ────────────────────────────────────
        for (int i = 1; i < Players.Length; i++)
        {
            var p = Players[i];
            if (p == null || !p.Online)
                continue;

            ClearPlayerUpdateReqs(p);
        }

        // ── 5. NPC processing ───────────────────────────────────────────────
        for (int i = 1; i < Npcs.Length; i++)
        {
            var n = Npcs[i];
            if (n == null)
                continue;

            // Clear masks first (matches Java ordering: clear → process)
            n.ClearUpdateMasks();
        }

        for (int i = 1; i < Npcs.Length; i++)
        {
            var n = Npcs[i];
            if (n == null)
                continue;

            n.Process();

            if (!n.IsDead)
            {
                // Random walk handled by NpcMovement service (Phase 6+)
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
            p.SaveTimer--;

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
        if (p.FreezeDelay > 0) p.FreezeDelay--;

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
            if (p.SkillLvl[5] <= 0)
            {
                // Reset prayers — will be expanded with prayer service
                Array.Clear(p.PrayOn);
                p.DrainRate = 0;
                p.PrayerIcon = -1;
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
        {
            p.DeathDelay--;
            if (p.DeathDelay <= 0)
            {
                // Death handling — simplified; full logic in Phase 7
                p.AfterDeathUpdateReq = true;
            }
        }

        // After-death restoration
        if (p.AfterDeathUpdateReq)
        {
            for (int i = 0; i < Player.SkillCount; i++)
                p.SkillLvl[i] = p.GetLevelForXP(i);

            Array.Clear(p.PrayOn);
            p.DrainRate = 0;
            p.PrayerIcon = -1;
            p.FreezeDelay = 0;
            p.SkulledDelay = 0;
            p.SpecialAmount = 100;
            p.RunEnergy = 100;
            p.DeathDelay = 7;
            p.SpecialAmountUpdateReq = true;
            p.RunEnergyUpdateReq = true;
            p.SkulledUpdateReq = true;
            p.IsDead = false;
            p.AfterDeathUpdateReq = false;
        }

        // Home teleport sequence
        if (p.HomeTeleDelay > 0) p.HomeTeleDelay--;
        if (p.YellTimer > 0) p.YellTimer--;
        if (p.SuggestionTimer > 0) p.SuggestionTimer--;

        // Skilling timers
        if (p.CookTimer > 0) p.CookTimer--;
        if (p.FishTimer > 0) p.FishTimer--;
        if (p.FletchTimer > 0) p.FletchTimer--;
        if (p.SmithingTimer > 0) p.SmithingTimer--;
        if (p.HerbloreTimer > 0) p.HerbloreTimer--;
        if (p.AgilityTimer > 0) p.AgilityTimer--;
        if (p.ActionTimer > 0) p.ActionTimer--;
        if (p.FireDelay > 0) p.FireDelay--;

        // Duel timer
        if (p.DuelTimer > 0) p.DuelTimer--;

        // Disconnection forwarding
        if (p.Disconnected[0])
            p.Disconnected[1] = true;

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

    /// <summary>
    /// Handle NPC death / respawn cycle.
    /// Mirrors the death logic in Engine.run().
    /// </summary>
    private void ProcessNpcDeath(NPC n, int index)
    {
        if (!n.DeadEmoteDone)
        {
            n.RequestAnim(n.DeathEmote, 0);
            n.DeadEmoteDone = true;
            n.CombatDelay = 3;
        }
        else if (n.DeadEmoteDone && !n.HiddenNPC && n.CombatDelay <= 0)
        {
            n.NpcCanLoot = true;
            // Drop table processing will be handled by ItemDropService (Phase 7+)
            n.HiddenNPC = true;
        }
        else if (n.HiddenNPC && n.RespawnDelay <= 0)
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
}
