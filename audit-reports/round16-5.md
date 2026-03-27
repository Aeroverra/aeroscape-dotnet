# Audit Report - Round 16.5

**Date:** March 27, 2026  
**Scope:** DeathService.cs + NPC.cs  
**Previous Rounds:** 15 rounds of fixes completed  
**Focus:** Real bugs STILL PRESENT after previous fixes

## Critical Bug Found

### 🔴 NPC.cs - FollowCounter Logic Inconsistency

**Location:** `AppendPlayerFollowing()` method (lines ~256-290)

**Bug:** Inconsistent follow counter threshold logic creates unpredictable NPC behavior.

**Details:**
- Method starts with: `if (FollowCounter >= 3)` → abandons following
- Comment states: "If counter reaches or exceeds 4, NPC will abandon following"
- Code increments counter up to limit of 4: `if (FollowCounter < 4)`

**Impact:** 
- NPCs abandon following at count 3, not 4 as documented
- Creates confusion between code behavior and comments
- May cause NPCs to stop following players earlier than intended
- Inconsistency could lead to different behavior than original Java implementation

**Root Cause:** Mismatch between abandonment threshold (3) and increment limit (4)

**Recommended Fix:** 
Either change the check to `if (FollowCounter >= 4)` OR change the increment limit to `if (FollowCounter < 3)` and update the comment accordingly. The intended behavior should match the original Java implementation.

## Other Areas Examined (Clean)

### DeathService.cs - No remaining bugs found
- **Thread safety:** Proper locking in gravestone creation
- **Prayer retribution:** Correct area-of-effect logic for retribution damage
- **Resource cleanup:** Safe familiar cleanup with local references
- **File I/O:** Robust NPC drop parsing with validation
- **Memory management:** Proper item/equipment clearing

### NPC.cs - Only the follow counter issue above
- **Combat state:** Proper cleanup in `AppendHit()` when NPC dies  
- **Array bounds:** Safe bounds checking in `Process()` method
- **Update masks:** Clean reset in `ClearUpdateMasks()`
- **Movement logic:** Safe coordinate updates with proper validation

## Summary

After 15 rounds of fixes, only **1 critical logic bug** remains in the FollowCounter threshold logic. The rest of the code appears solid with good defensive programming practices.