using System;

namespace AeroScape.Server.Core.Entities;

/// <summary>
/// Runtime game-state for a spawned NPC.
/// Translated from DavidScape/npcs/NPC.java — network-layer independent.
/// </summary>
public class NPC
{
    // ── Identity / Slot ─────────────────────────────────────────────────────
    /// <summary>Position in the NPC array (Engine.npcs).</summary>
    public int NpcId { get; set; } = -1;

    /// <summary>NPC type id (e.g. 50 = King Black Dragon).</summary>
    public int NpcType { get; set; }

    /// <summary>Display name from NPC definitions.</summary>
    public string Name { get; set; } = string.Empty;

    // ── Position & Movement ─────────────────────────────────────────────────
    public int AbsX { get; set; }
    public int AbsY { get; set; }
    public int HeightLevel { get; set; }
    public int Direction { get; set; } = -1;
    public int MoveX { get; set; }
    public int MoveY { get; set; }

    /// <summary>Original spawn position (for respawning).</summary>
    public int MakeX { get; set; }
    public int MakeY { get; set; }

    /// <summary>Bounding box for random walk.</summary>
    public int MoveRangeX1 { get; set; }
    public int MoveRangeY1 { get; set; }
    public int MoveRangeX2 { get; set; }
    public int MoveRangeY2 { get; set; }

    public bool RandomWalk { get; set; } = true;

    // ── Combat Stats ────────────────────────────────────────────────────────
    public int CombatLevel { get; set; }
    public int MaxHP { get; set; } = 100;
    public int CurrentHP { get; set; } = 100;
    public int MaxHit { get; set; }
    /// <summary>0 = Melee, 1 = Range, 2 = Magic.</summary>
    public int AtkType { get; set; }
    /// <summary>Weakness type (same enum as AtkType).</summary>
    public int Weakness { get; set; } = 2;

    // ── Combat Emotes ───────────────────────────────────────────────────────
    public int AttackEmote { get; set; }
    public int DefendEmote { get; set; }
    public int DeathEmote { get; set; }
    public int AttackDelay { get; set; } = 5;
    public int CombatDelay { get; set; }

    // ── Combat State ────────────────────────────────────────────────────────
    public int AttackPlayer { get; set; }
    public bool AttackingPlayer { get; set; }
    public int FollowPlayer { get; set; }
    public int FollowCounter { get; set; }

    // ── Death / Respawn ─────────────────────────────────────────────────────
    public bool IsDead { get; set; }
    public bool DeadEmoteDone { get; set; } = true;
    public bool HiddenNPC { get; set; }
    public bool NeedsRespawn { get; set; }
    public int RespawnDelay { get; set; } = 1;

    // ── Loot ────────────────────────────────────────────────────────────────
    public bool NpcCanLoot { get; set; }

    // ── Prayer / Special ────────────────────────────────────────────────────
    /// <summary>0 = none, 1 = melee, 2 = range, 3 = magic, 4 = retribution.</summary>
    public int Praying { get; set; }
    public bool DoingRetribution { get; set; }

    // ── Summoning ───────────────────────────────────────────────────────────
    public bool IsSummoned { get; set; }
    public Player? Owner { get; set; }
    public bool IsPen { get; set; }

    // ── Update Flags ────────────────────────────────────────────────────────
    public bool UpdateReq { get; set; }

    public bool SpeakTextUpdateReq { get; set; }
    public string SpeakText { get; set; } = string.Empty;

    public bool Hit1UpdateReq { get; set; }
    public bool Hit2UpdateReq { get; set; }
    public int HitDiff1 { get; set; }
    public int PoisonHit1 { get; set; }
    public int HitDiff2 { get; set; }
    public int PoisonHit2 { get; set; }

    public bool AnimUpdateReq { get; set; }
    public int AnimRequest { get; set; } = 65535;
    public int AnimDelay { get; set; }

    public bool GfxUpdateReq { get; set; }
    public int GfxRequest { get; set; } = 65535;
    public int GfxDelay { get; set; }
    public int GfxHeight { get; set; }

    public bool FaceToUpdateReq { get; set; }
    public int FaceToRequest { get; set; } = -1;

    public bool FaceCoordsUpdateReq { get; set; }
    public int FaceCoordsX { get; set; } = -1;
    public int FaceCoordsY { get; set; } = -1;

    // ══════════════════════════════════════════════════════════════════════════
    //  Constructor
    // ══════════════════════════════════════════════════════════════════════════

    public NPC() { }

    public NPC(int type, int index)
    {
        NpcType = type;
        NpcId = index;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Methods (pure game-state logic, no network I/O)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Request an animation for this NPC.
    /// </summary>
    public void RequestAnim(int animId, int animD)
    {
        AnimRequest = animId;
        AnimDelay = animD;
        AnimUpdateReq = true;
        UpdateReq = true;
    }

    /// <summary>
    /// Request spoken text for this NPC.
    /// </summary>
    public void RequestText(string message)
    {
        SpeakText = message;
        AnimUpdateReq = true;
        SpeakTextUpdateReq = true;
    }

    /// <summary>
    /// Request a graphic effect for this NPC.
    /// </summary>
    public void RequestGfx(int gfxId, int gfxD)
    {
        if (gfxD >= 100)
            gfxD += 6553500;
        GfxRequest = gfxId;
        GfxDelay = gfxD;
        GfxUpdateReq = true;
        UpdateReq = true;
    }

    /// <summary>
    /// Request this NPC faces two coordinates.
    /// </summary>
    public void RequestFaceCoords(int x, int y)
    {
        FaceCoordsX = 2 * x + 1;
        FaceCoordsY = 2 * y + 1;
        FaceCoordsUpdateReq = true;
        UpdateReq = true;
    }

    /// <summary>
    /// Request this NPC faces another entity.
    /// </summary>
    public void RequestFaceTo(int faceId)
    {
        FaceToRequest = faceId;
        FaceToUpdateReq = true;
        UpdateReq = true;
    }

    /// <summary>
    /// Apply damage to this NPC.
    /// </summary>
    public void AppendHit(int damage, int poison)
    {
        if (damage > CurrentHP)
            damage = CurrentHP;

        CurrentHP -= damage;

        if (CurrentHP <= 0)
        {
            CurrentHP = 0;
            AttackingPlayer = false;
            IsDead = true;
        }

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
    /// Per-tick process: follow player, handle respawn countdown, delegate combat.
    /// Mirrors NPC.process() from Java.
    /// </summary>
    public void Process(Player?[] players)
    {
        if (FollowPlayer != 0)
        {
            var followTarget = Owner;
            if (followTarget is null && FollowPlayer > 0 && FollowPlayer < players.Length)
            {
                followTarget = players[FollowPlayer];
            }

            if (followTarget is null)
            {
                IsDead = true;
            }
            else if (!followTarget.Online || followTarget.IsDead)
            {
                IsDead = true;
            }
            else
            {
                AppendPlayerFollowing(followTarget);
            }
        }

        if (RespawnDelay > 0 && IsDead)
            RespawnDelay--;

        if (CombatDelay > 0)
            CombatDelay--;
    }

    private void AppendPlayerFollowing(Player player)
    {
        if (FollowCounter >= 3)
        {
            FollowPlayer = 0;
            AttackingPlayer = false;
            AttackPlayer = 0;
            FollowCounter = 0;
            RequestFaceCoords(3333, 3333);
            RequestFaceTo(-1);
            return;
        }

        if (AttackPlayer != player.PlayerId)
        {
            return;
        }

        if (!player.AttackingNPC && FollowCounter < 4 && Owner is null)
        {
            FollowCounter++;
        }
        else
        {
            FollowCounter = 0;
        }

        var playerX = player.AbsX;
        var playerY = player.AbsY;
        RequestFaceCoords(playerX, playerY);

        if (AbsX > playerX + 15 || AbsY > playerY + 15 || AbsX < playerX - 15 || AbsY < playerY - 15 || HeightLevel != player.HeightLevel)
        {
            AttackingPlayer = false;
            FollowPlayer = 0;
            RequestFaceCoords(3333, 3333);
            RequestFaceTo(-1);
            return;
        }

        HeightLevel = player.HeightLevel;
        MoveX = Math.Sign(playerX - AbsX);
        MoveY = Math.Sign(playerY - AbsY);
        if (MoveX != 0 || MoveY != 0)
        {
            AbsX += MoveX;
            AbsY += MoveY;
            UpdateReq = true;
        }
    }

    /// <summary>
    /// Reset all update masks (called after the update packet is sent).
    /// Mirrors NPCUpdate.clearUpdateMasks.
    /// </summary>
    public void ClearUpdateMasks()
    {
        UpdateReq = false;
        SpeakTextUpdateReq = false;
        Hit1UpdateReq = false;
        Hit2UpdateReq = false;
        AnimUpdateReq = false;
        GfxUpdateReq = false;
        FaceToUpdateReq = false;
        FaceCoordsUpdateReq = false;
    }
}
