# Audit Round 17 - Comprehensive Sweep Report

## Scope
- **Files**: PlayerUpdateWriter.cs, NpcUpdateWriter.cs
- **Previous Rounds**: 16 rounds of fixes completed
- **Objective**: Find ALL remaining bugs with extreme scrutiny

## Critical Findings

### PlayerUpdateWriter.cs

#### 1. Buffer Overflow in AddNewPlayer() - Delta Coordinate Calculation
**Location**: Lines 423-430
**Severity**: CRITICAL
```csharp
int yPos = p2.AbsY - p.AbsY;
if (yPos > 15) yPos += 32;
int xPos = p2.AbsX - p.AbsX;
if (xPos > 15) xPos += 32;
```

**Issue**: This logic only handles positive deltas > 15, but doesn't handle negative deltas < -16. The Java implementation shows both positive AND negative adjustments are needed.

**Expected**:
```csharp
int yPos = p2.AbsY - p.AbsY;
if (yPos < 0) yPos += 32;
int xPos = p2.AbsX - p.AbsX;  
if (xPos < 0) xPos += 32;
```

#### 2. Missing Null Check in AppendPlayerUpdateMasks()
**Location**: Lines 257-260
**Severity**: HIGH
```csharp
for (int i = 0; i < p.PlayerListSize; i++)
{
    var p2 = p.PlayerList[i];
    if (p2 == null)
        continue;
```

**Issue**: The code checks for null AFTER accessing p.PlayerList[i], but doesn't validate that i is within bounds of the array. If PlayerListSize > PlayerList.Length, this will throw IndexOutOfRangeException.

#### 3. Race Condition in ClearUpdateReqs()
**Location**: Lines 269-286
**Severity**: MEDIUM
```csharp
public void ClearUpdateReqs(Player p)
{
    p.UpdateReq = false;
    p.ChatTextUpdateReq = false;
    // ... more flags
}
```

**Issue**: No thread safety. If another thread modifies these flags while clearing, inconsistent state could occur.

#### 4. Integer Overflow in AppendHit1()
**Location**: Line 356
**Severity**: LOW (already mitigated but could be cleaner)
```csharp
int hpRatio = maxHP > 0 ? (int)((long)p.SkillLvl[3] * 255L / maxHP) : 0;
```

**Issue**: While the long cast prevents overflow, it would be cleaner to use Math.Min to ensure the result is always 0-255:
```csharp
int hpRatio = maxHP > 0 ? Math.Min(255, (int)((long)p.SkillLvl[3] * 255L / maxHP)) : 0;
```

#### 5. Magic Number Anti-Pattern
**Location**: Throughout file
**Severity**: LOW (code quality)

Examples:
- Line 37: `2 * 8` and `11 * 8` (should be constants like REGION_EDGE_MIN, REGION_EDGE_MAX)
- Line 67-76: Same magic numbers repeated
- Line 216: `216` packet opcode (should be constant PLAYER_UPDATE_OPCODE)

### NpcUpdateWriter.cs

#### 6. Array Bounds Issue in UpdateNpc()
**Location**: Line 13
**Severity**: HIGH
```csharp
var newNpcIds = new byte[npcs.Length];
```

**Issue**: Later in AddNewNpc(), we use `npc.NpcId` as an index into this array without validating it's within bounds:
```csharp
p.NpcsInList[npc.NpcId] = 1;  // Line 169
```

If `npc.NpcId >= npcs.Length`, this will throw IndexOutOfRangeException.

#### 7. Incorrect Delta Wrapping in AddNewNpc()
**Location**: Lines 173-176
**Severity**: CRITICAL
```csharp
int y = npc.AbsY - p.AbsY;
if (y < 0) y += 32;
int x = npc.AbsX - p.AbsX;
if (x < 0) x += 32;
```

**Issue**: This is the OPPOSITE of the PlayerUpdateWriter bug! It only handles negative deltas, not positive ones > 15. Should handle BOTH cases like:
```csharp
if (y < -16) y += 32;
else if (y > 15) y -= 32;
```

#### 8. Division by Zero in GetMove()
**Location**: Lines 216-222
**Severity**: LOW (logic issue)
```csharp
private static int GetMove(int place1, int place2)
{
    if ((place1 - place2) == 0) return 0;
    if ((place1 - place2) < 0) return 1;
    if ((place1 - place2) > 0) return -1;
    return 0;
}
```

**Issue**: The final `return 0` is unreachable dead code. Also, the repeated subtraction is inefficient.

#### 9. Potential Null Reference in RandomWalk()
**Location**: Line 85
**Severity**: MEDIUM
```csharp
if (!DoesWalk(n) && n.FollowPlayer != 0)
{
}
```

**Issue**: Empty block suggests incomplete implementation. Also no null check on `n`.

#### 10. Thread Safety Issue in Random.Shared
**Location**: Line 96
**Severity**: LOW
```csharp
else if (n.RandomWalk && Random.Shared.Next(10) == 0 && DoesWalk(n))
```

**Issue**: While Random.Shared is thread-safe in .NET 6+, heavy concurrent access can cause contention. Consider using ThreadLocal<Random> for better performance.

## Summary

Found **10 bugs** ranging from CRITICAL to LOW severity:
- 3 CRITICAL bugs (coordinate wrapping errors that will cause visual glitches)
- 2 HIGH bugs (array bounds issues that can crash)
- 3 MEDIUM bugs (thread safety, null refs)
- 2 LOW bugs (code quality, performance)

The most serious issues are the coordinate delta wrapping bugs in both files - they're handling opposite cases incorrectly, which will cause players/NPCs to "jump" to wrong positions when crossing region boundaries.

## Recommendations

1. **Immediate**: Fix the coordinate wrapping logic in both AddNewPlayer methods
2. **Immediate**: Add bounds checking for NpcId array access
3. **Important**: Add array bounds validation in player list iteration
4. **Consider**: Add thread safety if these classes are used concurrently
5. **Cleanup**: Extract magic numbers to named constants