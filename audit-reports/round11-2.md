# Audit Report Round 11-2

**Date:** 2026-03-27  
**Scope:** GameEngine.cs + WalkQueue.cs + DeathService.cs + NPC.cs  
**Approach:** Static code analysis for real bugs still present after 10 rounds of fixes

## Real Bugs Found

### 1. **Race Condition in ProcessGlobalTimers() - GameEngine.cs**
**Lines 342-353**
```csharp
// Thread-safe player count with proper synchronization
int count = 0;
var playersSnapshot = Players; // Get local reference
lock (playersSnapshot) // Synchronize access to prevent race conditions
{
    for (int i = 1; i < playersSnapshot.Length; i++)
    {
        var p = playersSnapshot[i];
        if (p != null && p.Online)
            count++;
    }
}
PlayersInGame = count;
```

**Issue:** The `lock(playersSnapshot)` is ineffective because `playersSnapshot` just references the same array object as `Players`. The array itself is never locked elsewhere in the codebase, so this provides no actual synchronization. Other threads can still modify `Players` while this code runs, causing potential race conditions.

**Fix:** Either use a proper locking mechanism across all array access, or use atomic operations/thread-safe collections.

### 2. **Potential Null Reference in ProcessDeferredNpcOption() - GameEngine.cs**
**Lines 692-696**
```csharp
// Check if player is adjacent to the NPC
if (CombatFormulas.GetDistance(player.AbsX, player.AbsY, player.ClickX, player.ClickY) <= 1)
{
    // Player is in range, trigger the deferred action using the interaction service
    switch (optionNumber)
```

**Issue:** The code uses `player.ClickX` and `player.ClickY` for distance calculation, but these may not represent the NPC's position. The comment says "Check if player is adjacent to the NPC" but it's checking distance to click coordinates, not NPC coordinates.

**Fix:** Use `npc.AbsX` and `npc.AbsY` instead of `player.ClickX` and `player.ClickY`.

### 3. **Potential Index Out of Range in AddStepToWalkingQueue() - WalkQueue.cs**
**Lines 123-135**
```csharp
// Validate bounds before any array access
if (player.WQueueWritePtr >= player.WalkingQueueSize || 
    player.WQueueWritePtr < 0 ||
    player.WQueueWritePtr >= player.WalkingQueueX.Length ||
    player.WQueueWritePtr >= player.WalkingQueueY.Length ||
    player.WQueueWritePtr >= player.WalkingQueue.Length)
{
    return;
}

// Validate read position bounds
if (player.WQueueWritePtr == 0 || (player.WQueueWritePtr - 1) >= player.WalkingQueueX.Length)
{
    return;
}
```

**Issue:** The validation checks `player.WQueueWritePtr >= player.WalkingQueueSize` but `WalkingQueueSize` is not a standard property of arrays. This should be checking against the actual array length. If `WalkingQueueSize` doesn't exist or has wrong value, this validation fails.

**Fix:** Remove the `WalkingQueueSize` check or ensure it's properly defined and maintained.

### 4. **Resource Leak in Write() Method - WalkQueue.cs**
**Lines 216-231**
```csharp
private static void Write(Player player, Action<FrameWriter> build)
{
    var session = player.Session;
    if (session is null)
        return;

    using var w = new FrameWriter(4096);
    build(w);
    // Use fire-and-forget async to avoid blocking the game thread
    _ = Task.Run(async () =>
    {
        try
        {
            await w.FlushToAsync(session.GetStream(), session.CancellationToken);
        }
        catch
        {
            // Silently handle network failures to prevent game thread crashes
        }
    });
}
```

**Issue:** The `FrameWriter` is disposed (`using var w`) before the async task starts. This means the async task will operate on a disposed object, leading to `ObjectDisposedException`.

**Fix:** Move the `using` statement inside the Task.Run or use a different approach for async disposal.

## Summary
- **4 real bugs found**
- Issues range from concurrency problems to resource management
- Most critical: Race condition and resource leak that could cause runtime exceptions