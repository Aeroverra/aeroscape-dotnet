# Audit Report - Round 12-2

**Date:** 2026-03-27  
**Scope:** GameEngine.cs, WalkQueue.cs, DeathService.cs, NPC.cs  
**Status:** After 11 rounds of fixes  

## Bugs Found

### 1. GameEngine.cs - Race Condition in Player Count
**Location:** ProcessGlobalTimers() method  
**Issue:** Thread safety violation when counting players
```csharp
// Current code:
lock (Players) // Lock the actual array to prevent race conditions
{
    for (int i = 1; i < Players.Length; i++)
    {
        var p = Players[i];
        if (p != null && p.Online)
            count++;
    }
}
```
**Problem:** The `Players` array is not declared as a locking object and this pattern is incorrect. The comment suggests awareness of threading issues but the implementation is flawed.

### 2. GameEngine.cs - Potential Memory Leak in Global Timers
**Location:** ProcessGlobalTimers() method  
**Issue:** Carrier references are manually nulled but this is unnecessary and indicates poor design
```csharp
var zamorakCarrier = ZamorakP > 0 && ZamorakP < Players.Length ? Players[ZamorakP] : null;
// ... later
// Clear any lingering reference to allow proper garbage collection
zamorakCarrier = null;
```
**Problem:** Local variables are automatically eligible for GC when they go out of scope. Manual nulling suggests misunderstanding of GC or indicates there might be a deeper reference issue.

### 3. WalkQueue.cs - Inadequate Array Bounds Validation
**Location:** AddStepToWalkingQueue() method  
**Issue:** Uses Player.WalkingQueueSize constant in comments but validates against actual array lengths
```csharp
// Validate bounds before any array access - use actual array lengths instead of WalkingQueueSize
if (player.WQueueWritePtr < 0 ||
    player.WQueueWritePtr >= player.WalkingQueueX.Length ||
    // ... more checks
```
**Problem:** Comment mentions `WalkingQueueSize` but code uses `.Length`. This inconsistency suggests the validation might not match the intended design.

### 4. WalkQueue.cs - Async Fire-and-Forget Without Error Handling Context
**Location:** Write() method  
**Issue:** Fire-and-forget async with generic error swallowing
```csharp
_ = Task.Run(async () =>
{
    try
    {
        // ... network code
    }
    catch
    {
        // Silently handle network failures to prevent game thread crashes
    }
});
```
**Problem:** While preventing crashes is good, completely silent failures can mask important issues like systematic network problems.

### 5. DeathService.cs - Race Condition in Gravestone Creation
**Location:** MoveItemsToGravestone() method  
**Issue:** Lock scope is too narrow for atomic operation
```csharp
// Use lock to prevent race condition in gravestone creation
lock (engine.LoadedObjects)
{
    if (!engine.LoadedObjects.Exists(o => o.ObjectId == 12719 && o.X == player.gsX && o.Y == player.gsY))
    {
        engine.LoadedObjects.Add(new LoadedObject(12719, player.gsX, player.gsY, 0, 10));
    }
}
```
**Problem:** The check-then-add pattern within a lock is correct, but there's a potential issue where multiple threads could create gravestones for the same player if this method is called concurrently for the same player.

### 6. NPC.cs - Logical Inconsistency in Follow Counter
**Location:** AppendPlayerFollowing() method  
**Issue:** Conflicting logic for follow counter reset
```csharp
if (!player.AttackingNPC && FollowCounter < 4)
{
    FollowCounter++;
}
else if (player.AttackingNPC)
{
    // Only reset counter when player is actively attacking, not for owned NPCs
    FollowCounter = 0;
}
// Don't reset counter for owned/summoned NPCs unless player is attacking
```
**Problem:** The comment suggests different behavior for owned NPCs but the code doesn't differentiate between owned and unowned NPCs. The `IsSummoned` property and `Owner` field exist but aren't used in this logic.

## Summary

Found **6 bugs** across the audited files. Most are subtle concurrency/design issues rather than obvious crashes. The codebase shows signs of defensive programming but some defensive measures indicate underlying design concerns that should be addressed.