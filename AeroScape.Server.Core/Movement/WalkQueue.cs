using System;
using System.Threading.Tasks;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Frames;
using AeroScape.Server.Core.Messages;
using Microsoft.Extensions.Logging;

namespace AeroScape.Server.Core.Movement;

/// <summary>
/// Legacy 508 walking queue and movement tick processing.
/// Ported from DavidScape/io/packets/Walking.java and players/update/PlayerMovement.java.
/// </summary>
public sealed class WalkQueue
{
    private static readonly sbyte[] DirectionDeltaX = { -1, 0, 1, -1, 1, -1, 0, 1 };
    private static readonly sbyte[] DirectionDeltaY = { 1, 1, 1, 0, 0, -1, -1, -1 };

    private readonly GameFrames _frames;
    private readonly ILogger<WalkQueue>? _logger;

    public WalkQueue(GameFrames frames, ILogger<WalkQueue>? logger = null)
    {
        _frames = frames;
        _logger = logger;
    }

    public void HandleWalk(Player player, WalkMessage message)
    {
        _logger?.LogInformation("HandleWalk called for {Username}: first=({X},{Y}) running={Running} pathDeltas={PathCount}", 
            player.Username, message.FirstX, message.FirstY, message.IsRunning, 
            message.PathX?.Length ?? 0);
        
        Console.WriteLine($"[WALK] Before reset: ReadPtr={player.WQueueReadPtr}, WritePtr={player.WQueueWritePtr}");
        Console.WriteLine($"[WALK] Path deltas: PathX.Length={message.PathX?.Length ?? 0}, PathY.Length={message.PathY?.Length ?? 0}");
        
        ClearInteractions(player);
        
        // Restore interface like Java Walking.java:14 and 45-48
        Write(player, w => _frames.RemoveShownInterface(w, player));
        player.InterfaceId = -1;
        player.ChatboxInterfaceId = -1;
        Write(player, w => _frames.RestoreTabs(w, player));
        Write(player, w => _frames.RestoreInventory(w, player));
        Write(player, w => _frames.RemoveChatboxInterface(w, player));

        ResetWalkingQueue(player);
        player.AutoCasting = false;
        player.IsRunning = message.IsRunning;

        // FirstX/FirstY already have region base subtracted in WalkDecoder
        AddToWalkingQueue(player, message.FirstX, message.FirstY);
        
        // PathX/PathY are deltas from the previous position, so we need to accumulate
        // IMPORTANT: Check if there are any path deltas before looping!
        if (message.PathX != null && message.PathY != null && message.PathX.Length > 0)
        {
            int currentX = message.FirstX;
            int currentY = message.FirstY;
            
            // Ensure PathX and PathY arrays have the same length
            int pathLength = Math.Min(message.PathX.Length, message.PathY.Length);
            
            for (int i = 0; i < pathLength; i++)
            {
                currentX += message.PathX[i];
                currentY += message.PathY[i];
                AddToWalkingQueue(player, currentX, currentY);
            }
        }

        if (player.FaceToReq != 65535)
        {
            player.RequestFaceTo(65535);
        }

        if (player.FreezeDelay > 0)
        {
            player.LastTickMessage = "You cant move! Your frozen!";
        }
        
        Console.WriteLine($"[WALK] After adding steps: ReadPtr={player.WQueueReadPtr}, WritePtr={player.WQueueWritePtr}");
    }

    public void Process(Player player)
    {
        // Reset movement directions at the start of each tick
        player.MapRegionDidChange = false;
        player.DidTeleport = false;
        player.WalkDir = -1;
        player.RunDir = -1;
        
        // Debug: Log the queue state each tick if player is moving
        if (player.WQueueReadPtr != player.WQueueWritePtr)
        {
            Console.WriteLine($"[WALK TICK] {player.Username}: ReadPtr={player.WQueueReadPtr}, WritePtr={player.WQueueWritePtr}, Pos=({player.AbsX},{player.AbsY})");
        }

        if (player.TeleportToX != -1 && player.TeleportToY != -1)
        {
            player.MapRegionDidChange = true;
            if (player.MapRegionX != -1 && player.MapRegionY != -1)
            {
                int relX = player.TeleportToX - (player.MapRegionX - 6) * 8;
                int relY = player.TeleportToY - (player.MapRegionY - 6) * 8;
                if (relX >= 2 * 8 && relX < 11 * 8 && relY >= 2 * 8 && relY < 11 * 8)
                {
                    player.MapRegionDidChange = false;
                }
            }

            if (player.MapRegionDidChange)
            {
                player.MapRegionX = player.TeleportToX >> 3;
                player.MapRegionY = player.TeleportToY >> 3;
            }

            player.CurrentX = player.TeleportToX - 8 * (player.MapRegionX - 6);
            player.CurrentY = player.TeleportToY - 8 * (player.MapRegionY - 6);
            player.AbsX = player.TeleportToX;
            player.AbsY = player.TeleportToY;
            ResetWalkingQueue(player);
            player.TeleportToX = -1;
            player.TeleportToY = -1;
            player.DidTeleport = true;
            return;
        }

        if (player.FreezeDelay > 0)
        {
            return;
        }

        player.WalkDir = GetNextWalkingDirection(player);
        if (player.WalkDir == -1)
        {
            return;
        }

        if (player.IsRunning)
        {
            player.RunDir = GetNextWalkingDirection(player);
        }

        if (player.CurrentX < 2 * 8)
        {
            player.MapRegionDidChange = true;
        }
        else if (player.CurrentX >= 11 * 8)
        {
            player.MapRegionDidChange = true;
        }
        if (player.CurrentY < 2 * 8)
        {
            player.MapRegionDidChange = true;
        }
        else if (player.CurrentY >= 11 * 8)
        {
            player.MapRegionDidChange = true;
        }
        if (player.MapRegionDidChange)
        {
            player.TeleportToX = player.AbsX;
            player.TeleportToY = player.AbsY;
        }

        if (player.RunDir != -1)
        {
            if (player.RunEnergy > 0)
            {
                player.RunEnergyUpdateReq = true;
                player.RunEnergy--;
            }
            else
            {
                player.IsRunning = false;
                player.RunDir = -1;
            }
        }
    }

    public void ResetWalkingQueue(Player player)
    {
        player.WalkingQueueX[0] = player.CurrentX;
        player.WalkingQueueY[0] = player.CurrentY;
        player.WalkingQueue[0] = -1;
        player.WQueueReadPtr = 1;
        player.WQueueWritePtr = 1;
    }

    public void StopMovement(Player player)
    {
        ResetWalkingQueue(player);
        player.WalkDir = -1;
        player.RunDir = -1;
    }

    public void AddToWalkingQueue(Player player, int x, int y)
    {
        int diffX = x - player.WalkingQueueX[player.WQueueWritePtr - 1];
        int diffY = y - player.WalkingQueueY[player.WQueueWritePtr - 1];
        int max = Math.Max(Math.Abs(diffX), Math.Abs(diffY));

        for (int i = 0; i < max; i++)
        {
            if (diffX < 0)
            {
                diffX++;
            }
            else if (diffX > 0)
            {
                diffX--;
            }

            if (diffY < 0)
            {
                diffY++;
            }
            else if (diffY > 0)
            {
                diffY--;
            }

            AddStepToWalkingQueue(player, x - diffX, y - diffY);
        }
    }

    public void AddStepToWalkingQueue(Player player, int x, int y)
    {
        // Validate arrays are initialized
        if (player.WalkingQueueX == null || player.WalkingQueueY == null || player.WalkingQueue == null)
        {
            return;
        }

        // Validate bounds before any array access - ensure consistency with player.WalkingQueueSize
        if (player.WQueueWritePtr < 0 ||
            player.WQueueWritePtr >= player.WalkingQueueSize ||
            player.WQueueWritePtr >= player.WalkingQueueX.Length ||
            player.WQueueWritePtr >= player.WalkingQueueY.Length ||
            player.WQueueWritePtr >= player.WalkingQueue.Length)
        {
            return;
        }

        // Validate read position bounds
        if (player.WQueueWritePtr == 0 || 
            (player.WQueueWritePtr - 1) >= player.WalkingQueueSize ||
            (player.WQueueWritePtr - 1) >= player.WalkingQueueX.Length)
        {
            return;
        }

        int diffX = x - player.WalkingQueueX[player.WQueueWritePtr - 1];
        int diffY = y - player.WalkingQueueY[player.WQueueWritePtr - 1];
        int dir = Direction(diffX, diffY);

        if (dir != -1)
        {
            // Ensure we don't exceed the queue size
            if (player.WQueueWritePtr >= player.WalkingQueueSize - 1)
            {
                Console.WriteLine($"[WALK] Warning: Walking queue full for {player.Username}");
                return;
            }
            
            player.WalkingQueueX[player.WQueueWritePtr] = x;
            player.WalkingQueueY[player.WQueueWritePtr] = y;
            player.WalkingQueue[player.WQueueWritePtr++] = dir;
        }
    }

    private static int GetNextWalkingDirection(Player player)
    {
        if (player.WQueueReadPtr == player.WQueueWritePtr)
        {
            // No more steps in the queue - stop walking
            return -1;
        }

        int dir = player.WalkingQueue[player.WQueueReadPtr++];
        player.CurrentX += DirectionDeltaX[dir];
        player.CurrentY += DirectionDeltaY[dir];
        player.AbsX += DirectionDeltaX[dir];
        player.AbsY += DirectionDeltaY[dir];
        
        // Log when we reach the end of the queue
        if (player.WQueueReadPtr == player.WQueueWritePtr)
        {
            Console.WriteLine($"[WALK] {player.Username} reached destination at ({player.AbsX}, {player.AbsY})");
        }
        
        return dir;
    }

    private static int Direction(int dx, int dy)
    {
        if (dx < 0)
        {
            if (dy < 0)
            {
                return 5;
            }

            return dy > 0 ? 0 : 3;
        }

        if (dx > 0)
        {
            if (dy < 0)
            {
                return 7;
            }

            return dy > 0 ? 2 : 4;
        }

        if (dy < 0)
        {
            return 6;
        }

        return dy > 0 ? 1 : -1;
    }

    private static void ClearInteractions(Player player)
    {
        player.ItemPickup = false;
        player.PlayerOption1 = false;
        player.PlayerOption2 = false;
        player.PlayerOption3 = false;
        player.NpcOption1 = false;
        player.NpcOption2 = false;
        player.NpcOption3 = false;
        player.ObjectOption1 = false;
        player.ObjectOption2 = false;
        player.AttackingPlayer = false;
        player.AttackingNPC = false;
        player.AttackPlayer = 0;
        player.AttackNPC = 0;
        player.FollowingPlayer = false;
        player.FollowPlayerIndex = 0;
    }

    private void Write(Player player, Action<FrameWriter> build)
    {
        var session = player.Session;
        if (session is null)
            return;

        // Use fire-and-forget async to avoid blocking the game thread
        _ = Task.Run(async () =>
        {
            try
            {
                using var w = new FrameWriter(4096);
                build(w);
                await w.FlushToAsync(session.GetStream(), session.CancellationToken);
            }
            catch (Exception ex)
            {
                // Log network failures while preventing game thread crashes
                _logger?.LogWarning(ex, "Network write failed for player {PlayerId}", player.PlayerId);
            }
        });
    }
}
