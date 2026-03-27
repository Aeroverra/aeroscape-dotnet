# Game Engine Audit Round 7-4: Engine.cs vs Engine.java

## CRITICAL BUG: Missing NPC Combat Processing

**Location:** GameEngine.cs ProcessMajorTick() method, NPC processing section
**Severity:** CRITICAL - Game-breaking

### Issue
The C# GameEngine is missing a critical piece of NPC combat logic that exists in the Java Engine:

**Java Engine.java (lines 323-325):**
```java
if (n.attackingPlayer) {
    npcPlayerCombat.attackPlayer(n);
}
```

**C# GameEngine.cs (lines 350-352):**
```csharp
if (n.AttackingPlayer)
    NpcPlayerCombat.ProcessAttack(n);
```

The issue is that in the C# version, this combat check is inside a `!n.IsDead` conditional block, but in the Java version, the combat check happens **before** any death state checks.

### Root Cause
In Java Engine.java, the NPC processing flow is:
1. Call `n.process()` 
2. Check `if (n.attackingPlayer)` and process combat
3. **THEN** check death state and handle respawn

In C# GameEngine.cs, the flow is:
1. Call `n.Process(Players)`
2. Check `if (!n.IsDead)` first
3. **ONLY IF NOT DEAD:** Check `if (n.AttackingPlayer)` and process combat
4. Handle death state in else block

### Impact
NPCs that die while attacking a player will stop their attack immediately instead of completing their attack sequence. This breaks combat mechanics where NPCs should be able to land their final attack even as they're dying.

### Fix Required
Move the `if (n.AttackingPlayer)` combat check outside and before the `!n.IsDead` conditional block to match the Java logic flow.

## CRITICAL BUG: Incorrect NPC Movement Logic

**Location:** GameEngine.cs ProcessMajorTick() method, NPC processing section  
**Severity:** CRITICAL - Breaks NPC behavior

### Issue
The C# version is missing the NPC movement processing that exists in Java.

**Java Engine.java (lines 326-330):**
```java
if (!n.isDead) {
    if (n.randomWalk && !n.attackingPlayer) {
        npcMovement.randomWalk(n);
    }
}
```

**C# GameEngine.cs:**
The equivalent `GameUpdateService?.ProcessNpcMovement(n);` is inside the `!n.IsDead` block, but there's no equivalent to the `randomWalk` logic.

### Impact
NPCs will not perform random walking behavior, making the world feel static and lifeless.

## BUG: Missing Player Tab Restoration Logic

**Location:** GameEngine.cs ProcessPlayerTick() method
**Severity:** MEDIUM - UI behavior discrepancy

### Issue
The Java version has critical UI tab restoration logic that's missing in C#:

**Java Engine.java (lines 273-278):**
```java
if (p.interfaceId != 762 &&
        p.interfaceId != 335 &&
        p.interfaceId != 334 &&
        p.interfaceId != 620) {
    p.frames.restoreTabs(p);
}
```

This ensures that when players aren't in specific interfaces (trade, bank, etc.), their game tabs are properly restored.

### Impact
Players may get stuck in interface states or have missing/broken game tabs after closing certain interfaces.

## BUG: Missing Player Count Update

**Location:** GameEngine.cs - global processing section  
**Severity:** LOW - Missing feature

### Issue  
Java Engine.java tracks `constPlayers` but C# doesn't update any equivalent player count tracking in the main loop.

**Java Engine.java:**
The player count affects various minigame timers and server population tracking.

### Impact
Minigame logic that depends on player counts may not function correctly.

## TIMING BUG: Loop Structure Mismatch

**Location:** GameEngine.cs ExecuteAsync() vs Java Engine.run()
**Severity:** MEDIUM - Performance/timing issue

### Issue
The C# version uses a different timing approach than Java:

**Java:** Processes packets every 100ms, entities every 600ms
**C#:** Processes everything every 600ms

The Java version has this critical packet processing loop:
```java
connectAwaitingPlayers();
packets.parseIncomingPackets();
if (curTime - lastEntityUpdate >= 600) {
    // Entity processing
}
```

### Impact
The C# version may have slower packet response times since it doesn't process packets at 100ms intervals.

---

## Summary
**CRITICAL bugs found:** 2  
**MEDIUM bugs found:** 2  
**LOW bugs found:** 1

The most severe issues are the missing NPC combat processing and incorrect NPC movement logic, which would significantly break gameplay.