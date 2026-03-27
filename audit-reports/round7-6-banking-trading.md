# Audit Report - Round 7: Banking, Trading & Ground Items

**Date**: 2026-03-26  
**Scope**: PlayerBankService.cs, TradingService.cs, GroundItemManager.cs  
**Compared Against**: PlayerBank.java, PlayerTrade.java, PTrade.java, TButtons.java, Items.java  

## Critical Bugs Found

### 1. **TRADING: Item Duplication Vulnerability in Trade Completion** (CRITICAL)
**File**: `TradingService.cs:148-180` (`CompleteTrade` method)  
**Issue**: The current implementation has a race condition that can lead to item duplication.

**Problem**: The method performs inventory space validation BEFORE clearing trade containers, but the items remain in the trade containers during validation. If the validation fails after space checks but before container clearing, items could be restored AND remain in containers.

**Java Reference**: `PlayerTrade.java:24-37` and `PTrade.java:115-134` properly clear trade containers immediately when completing the trade.

**Fix Required**: Move `ClearTradeContainers()` call before inventory validation, not after.

### 2. **BANKING: Integer Overflow Protection Missing** (HIGH)
**File**: `PlayerBankService.cs:39-40` and `PlayerBankService.cs:83-84`  
**Issue**: The C# code uses `ItemDefinitionLoader.MaxItemAmount` for overflow checks, but doesn't validate this constant matches Java's `Integer.MAX_VALUE` (2,147,483,647).

**Java Reference**: `PlayerBank.java:35` and `PlayerBank.java:69` use `Integer.MAX_VALUE` directly.

**Fix Required**: Ensure `ItemDefinitionLoader.MaxItemAmount` equals `2147483647` or use the literal value for consistency.

### 3. **BANKING: Missing Inventory Frame Updates** (MEDIUM)
**File**: `PlayerBankService.cs:232-241` (`RefreshBankUi` method)  
**Issue**: The C# implementation only sends bank item frames but missing inventory update frames that Java sends.

**Java Reference**: `PlayerBank.java:72-76` sends both bank frames AND inventory frames:
```java
p.frames.setItems(p, -1, 64209, 93, p.items, p.itemsN);
p.frames.setItems(p, 149, 0, 93, p.items, p.itemsN);
```

**Fix Required**: Add inventory frame updates to `RefreshBankUi()` method.

### 4. **TRADING: Missing Bidirectional Trade Partner Validation** (MEDIUM)  
**File**: `TradingService.cs:297-304` (`GetPartner` method)  
**Issue**: The C# code only validates that `partner?.TradePlayer != player.PlayerId` but Java has additional validation.

**Java Reference**: `PlayerTrade.java:77-83` (`checkStage` method) performs bidirectional validation to ensure both players are trading with each other.

**Fix Required**: Add validation to ensure `partner.TradePlayer == player.PlayerId` AND `player.TradePlayer == partner.PlayerId`.

### 5. **GROUND ITEMS: Missing Frame Notifications** (MEDIUM)
**File**: `GroundItemManager.cs:28-43` (`Process` method)  
**Issue**: The C# implementation doesn't send proper frame notifications when items become global or are removed.

**Java Reference**: `Items.java:71-85` sends `removeGroundItem` and `createGlobalItem` frame notifications to affected players.

**Fix Required**: Add frame notification calls when ground items transition from private to global visibility and when they're removed.

## Architecture Differences (Not Bugs)

### Trade System Architecture
The C# implementation uses a more modern, service-oriented approach compared to the Java version's player-centric design. The Java code uses a separate `PTrade` class with `LinkedList<TItem>` while C# uses direct player arrays. Both approaches are valid.

### Ground Item Management  
C# uses a centralized `GroundItemManager` while Java integrates ground items into the main `Items` class. The C# approach provides better separation of concerns.

## Summary
- **5 bugs found** requiring fixes
- **1 critical** item duplication vulnerability
- **1 high** severity integer overflow issue  
- **3 medium** severity frame/validation issues
- No show-stopping architecture flaws identified

## Recommendations
1. **IMMEDIATE**: Fix the trade completion item duplication bug
2. **HIGH PRIORITY**: Verify integer overflow constants match Java values
3. **MEDIUM PRIORITY**: Add missing frame updates and validations
4. **TESTING**: Comprehensive testing of trade completion under various inventory conditions