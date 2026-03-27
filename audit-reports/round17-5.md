# Audit Round 17-5 Report - DeathService.cs & NPC.cs

## Critical Bugs Found

### 1. DeathService.ProcessNpcDeath - Infinite Death Loop
**File**: DeathService.cs, Line 55-69
**Severity**: CRITICAL

The method never resets `npc.IsDead` to false after processing death. This creates an infinite loop where:
1. NPC dies (`IsDead = true`)
2. ProcessNpcDeath is called every tick
3. Death emote and loot dropping happen
4. `IsDead` remains true forever
5. NPC can never respawn properly

**Impact**: NPCs become permanently dead, never respawn, and continuously trigger death processing.

### 2. NPC.AppendPlayerFollowing - Follow Counter Logic Flaw
**File**: NPC.cs, Line 253-271
**Severity**: HIGH

The follow counter logic has multiple issues:
- Line 253: Checks `FollowCounter >= 4` but the method is named with threshold 3 in comment
- Lines 265-271: Complex branching logic that can cause NPCs to get stuck following
- The counter increment/decrement logic is convoluted and can lead to unexpected behavior
- Owned NPCs (`IsSummoned || Owner != null`) use `Math.Max(0, FollowCounter - 1)` which prevents negative values but doesn't properly reset

**Impact**: NPCs may not properly stop following players, or may abandon following too early.

### 3. DeathService.CleanupFamiliar - Race Condition
**File**: DeathService.cs, Line 303-313
**Severity**: MEDIUM

The method accesses `player.follower` multiple times without proper null checking:
```csharp
var follower = player.follower;
if (follower != null)
{
    follower.IsDead = true;
}
player.follower = null;
```

Between checking `follower != null` and setting `follower.IsDead = true`, another thread could null out `player.follower`, causing a NullReferenceException.

### 4. DeathService.ProcessPlayerDeath - Missing Null Check
**File**: DeathService.cs, Line 25-32
**Severity**: MEDIUM

The method assumes `player` is never null but doesn't validate:
```csharp
public void ProcessPlayerDeath(Player player)
{
    if (!player.IsDead)  // NullReferenceException if player is null
        return;
```

### 5. DeathService.DropNpcLoot - Path Traversal Vulnerability
**File**: DeathService.cs, Line 21
**Severity**: HIGH

The NPC drop path uses multiple ".." segments:
```csharp
_npcDropPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "legacy-java", "server508", "data", "npcs", "npcdrops.cfg");
```

This creates a brittle dependency on directory structure and could potentially access files outside the intended directory if the base directory changes.

### 6. NPC Constructor - Uninitialized State
**File**: NPC.cs, Line 119-124
**Severity**: LOW

The constructor with parameters doesn't initialize all required fields:
```csharp
public NPC(int type, int index)
{
    NpcType = type;
    NpcId = index;
}
```

Missing initialization of critical fields like `MaxHP`, `CurrentHP`, `DeadEmoteDone` which have defaults in the parameterless constructor but not here.

### 7. DeathService.MoveItemsToGravestone - Potential Object Duplication
**File**: DeathService.cs, Line 222-234
**Severity**: MEDIUM

The gravestone creation logic has a race condition:
```csharp
lock (engine.LoadedObjects)
{
    var gravestoneExists = engine.LoadedObjects.Exists(o => 
        o.ObjectId == 12719 && o.X == player.gsX && o.Y == player.gsY);
        
    if (!gravestoneExists)
    {
        engine.LoadedObjects.Add(new LoadedObject(12719, player.gsX, player.gsY, 0, 10));
    }
}
```

Multiple players dying at the exact same coordinates simultaneously could create multiple gravestones if the check and add aren't atomic.

### 8. NPC.Process - Null Reference Chain
**File**: NPC.cs, Line 206-229
**Severity**: HIGH

The method has multiple potential null reference issues:
- Line 215: `players[FollowPlayer]` could be null even if index is valid
- Line 218-222: Checks for null but then immediately uses `followTarget` properties without rechecking
- No validation that `players` array itself isn't null

### 9. DeathService.ApplyDead - Array Bounds Vulnerability
**File**: DeathService.cs, Line 96-110
**Severity**: MEDIUM

The retribution prayer effect iterates through all players without bounds checking:
```csharp
foreach (var other in GetEngine().Players)
{
    if (other == null || other.PlayerId == player.PlayerId)
    {
        continue;
    }
```

If `GetEngine().Players` returns null, this will throw. Additionally, no validation that the engine itself exists.

### 10. NPC.RequestGfx - Integer Overflow
**File**: NPC.cs, Line 170-179
**Severity**: LOW

The graphic delay calculation can overflow:
```csharp
if (gfxD >= 100)
    gfxD += 6553500;
```

For large values of `gfxD`, this addition could cause integer overflow, resulting in negative or wrapped values.

## Summary

Found 10 bugs ranging from CRITICAL to LOW severity. The most critical issues are:
1. NPCs never properly resetting their death state
2. Multiple race conditions in death processing
3. Null reference vulnerabilities throughout both files
4. Path traversal and integer overflow issues

These bugs could cause server crashes, NPCs becoming permanently unusable, and various exploits.