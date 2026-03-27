# Audit Report - Round 17-4
**Date**: 2026-03-27
**Auditor**: Azula (Subagent)
**Scope**: GameEngine.cs + WalkQueue.cs
**Status**: CRITICAL BUGS FOUND

## Summary
After extreme scrutiny of every line in both files, I found **5 critical bugs** that have survived 16 rounds of fixes.

## GameEngine.cs Bugs

### 1. Thread-Safety Bug: ProcessGlobalTimers() Race Condition
**Location**: Lines 435-453
**Severity**: CRITICAL

```csharp
// BUG: Reading carrier objects outside the lock can cause null reference exceptions
var zamorakCarrier = ZamorakP > 0 && ZamorakP < Players.Length ? Players[ZamorakP] : null;
if (zamorakCarrier == null || !zamorakCarrier.Online)
{
    ZamorakFlag = false;
    ZamorakP = 0;
}
```

**Issue**: The code reads `Players[ZamorakP]` without holding `_playersLock`. Between checking bounds and accessing the array, another thread could call `RemovePlayer()` and set that slot to null.

**Fix Required**: Move carrier access inside the lock:
```csharp
Player? zamorakCarrier = null;
lock (_playersLock)
{
    zamorakCarrier = ZamorakP > 0 && ZamorakP < Players.Length ? Players[ZamorakP] : null;
}
```

### 2. Combat State Leak: Dragon Claws Attack Target Not Cleared
**Location**: Line 666
**Severity**: HIGH

```csharp
// Clear attack target after claws finish to prevent state leaks
p.AttackPlayer = 0;
```

**Issue**: The comment says to clear the attack target but this only happens inside the `if (p.ClawTimer <= 0 && p.UseClaws)` block. If `UseClaws` is false when timer expires, `AttackPlayer` is never cleared, causing the player to remain "locked on" to a target.

**Fix Required**: Move the clear outside the UseClaws check:
```csharp
if (p.ClawTimer <= 0)
{
    if (p.UseClaws)
    {
        // ... existing claw logic ...
        p.UseClaws = false;
    }
    p.AttackPlayer = 0; // Always clear when timer expires
}
```

### 3. Object Removal Race Condition: Gravestone Timer
**Location**: Lines 697-706
**Severity**: MEDIUM

```csharp
lock (LoadedObjects)
{
    LoadedObjects.RemoveAll(o => 
        o.ObjectId == 12719 && o.X == p.gsX && o.Y == p.gsY);
}
```

**Issue**: While the removal is locked, other threads reading `LoadedObjects` without locks will see inconsistent state during enumeration. The collection is not thread-safe for readers during modification.

**Fix Required**: Make LoadedObjects a thread-safe collection or ensure all reads also use locks.

### 4. Deferred NPC Option Processing: Invalid NPC Access
**Location**: Lines 872-874
**Severity**: HIGH

```csharp
if (player.ClickId <= 0 || player.ClickId >= Npcs.Length)
    return true; // Invalid NPC, clear the pending option
```

**Issue**: The bounds check is correct, but immediately after, the code accesses `Npcs[player.ClickId]` without rechecking if another thread removed that NPC. Between the check and the access, `ProcessNpcDeath()` could have set `Npcs[index] = null`.

**Fix Required**: Combine the null check with bounds check:
```csharp
var npc = player.ClickId > 0 && player.ClickId < Npcs.Length ? Npcs[player.ClickId] : null;
if (npc is null)
    return true;
```

## WalkQueue.cs Bugs

### 5. Async Fire-and-Forget Network Write Bug
**Location**: Lines 253-276
**Severity**: CRITICAL

```csharp
Task.Run(async () =>
{
    try
    {
        using var w = new FrameWriter(4096);
        build(w);
        await w.FlushToAsync(session.GetStream(), session.CancellationToken);
    }
    catch (Exception ex)
    {
        _logger?.LogWarning(ex, "Network write failed for player {PlayerId}", player.PlayerId);
    }
}).ContinueWith(task =>
{
    if (task.IsFaulted && task.Exception != null)
    {
        _logger?.LogError(task.Exception, "Unhandled exception in Write task for player {PlayerId}", player.PlayerId);
    }
}, TaskContinuationOptions.OnlyOnFaulted);
```

**Issue**: Fire-and-forget async operations can cause critical problems:
1. If the player disconnects before the write completes, `session.GetStream()` may throw
2. The FrameWriter is disposed while network writes may still be pending
3. No backpressure - if writes are slow, unbounded tasks accumulate
4. Player state may change between task creation and execution

**Fix Required**: Either make writes synchronous on the game thread or implement proper async coordination with the network layer.

## Detailed Analysis Notes

### Thread Safety Patterns
- GameEngine uses `_playersLock` inconsistently - some reads happen outside locks
- NPC array has NO locking at all, yet is accessed by multiple threads
- Collections like LoadedObjects are not thread-safe but accessed concurrently

### State Management Issues  
- Combat state transitions have gaps where invalid states can persist
- Deferred actions don't revalidate their targets after delays
- Timer-based state changes don't atomically update related fields

### Network Integration Weaknesses
- Fire-and-forget async writes lose critical game state updates
- No flow control between game tick speed and network throughput  
- Session lifecycle not properly coordinated with game state

## Recommendations

1. **Immediate**: Fix the thread-safety bugs in ProcessGlobalTimers and deferred NPC options
2. **High Priority**: Rework the network write pattern to be synchronous or properly coordinated
3. **Medium Priority**: Add comprehensive locking to NPC array access
4. **Long Term**: Consider using concurrent collections or actor model for game state

## Verification Steps

1. Run with ThreadSanitizer to catch race conditions
2. Stress test with rapid player connect/disconnect  
3. Test dragon claws with target disconnecting mid-animation
4. Verify gravestone removal under concurrent object enumeration
5. Test network backpressure with slow connections

---
*End of Report - 5 Critical Bugs Found*