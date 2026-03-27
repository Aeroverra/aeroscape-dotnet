# AUDIT ROUND 16 - Critical Banking Bug Found

**Date:** 2026-03-27  
**Round:** 16  
**Scope:** ALL items/equipment/banking/trading/ground items  
**Previous Rounds:** 15 rounds of fixes  

## CRITICAL BUG FOUND

### 🚨 BANKING DEPOSIT LOGIC BUG

**File:** `PlayerBankService.cs`  
**Method:** `Deposit()`  
**Lines:** 50-74  
**Severity:** HIGH

**Bug Description:**
The banking deposit logic has a critical flaw where items are removed from player inventory **before** verifying that the bank deposit operation will succeed when dealing with overflow scenarios.

**Vulnerable Code:**
```csharp
if (bankCount + amount < 0)
{
    amount = ItemDefinitionLoader.MaxItemAmount - bankCount;
    player.LastTickMessage = "Your bank is full";
}

if (bankCount == 0 && freeBankSlot == -1)
{
    player.LastTickMessage = "Not enough space in your bank.";
    return; // ⚠️ EARLY RETURN WITHOUT ROLLING BACK!
}

if (bankCount > 0)
{
    var bankSlot = GetBankItemSlot(player, itemId);
    player.BankItemsN[bankSlot] += amount; // ⚠️ NO OVERFLOW CHECK HERE!
}
else
{
    player.BankItems[freeBankSlot] = itemId;
    player.BankItemsN[freeBankSlot] = amount;
}

playerItems.DeleteItem(player, player.Items[inventorySlot], inventorySlot, amount); // ⚠️ ITEMS DELETED EVEN IF BANK OPERATION FAILS!
```

**Issue #1: Item Loss on Bank Full**
When `bankCount == 0 && freeBankSlot == -1`, the method returns early but **still calls `playerItems.DeleteItem()` on line 73**. This causes items to be permanently deleted from the player's inventory even though the banking operation failed.

**Issue #2: Missing Overflow Validation for Existing Items**
When depositing to an existing bank stack (`bankCount > 0`), there's no validation that `bankSlot` returned by `GetBankItemSlot()` is valid. If the item somehow doesn't exist in bank anymore (race condition), this will cause an invalid array access.

**Issue #3: Inconsistent Overflow Handling**
The overflow check `if (bankCount + amount < 0)` adjusts the amount but doesn't prevent the subsequent banking logic from executing, which could still fail and cause item loss.

**Exploitation Scenario:**
1. Player fills bank completely (no free slots)
2. Player attempts to deposit a new item type not already in bank
3. Code path: `bankCount == 0 && freeBankSlot == -1` 
4. Method returns early with error message
5. **Items are still deleted from inventory** due to code structure
6. **Result: Item duplication/loss exploit**

**Recommended Fix:**
```csharp
// Validate bank operation BEFORE deleting items from inventory
if (bankCount == 0 && freeBankSlot == -1)
{
    player.LastTickMessage = "Not enough space in your bank.";
    return; // Early return - don't modify anything
}

// Additional validation for existing bank slot
if (bankCount > 0)
{
    var bankSlot = GetBankItemSlot(player, itemId);
    if (bankSlot < 0) // Item no longer exists in bank
    {
        // Treat as new item deposit instead
        freeBankSlot = GetFreeBankSlot(player);
        if (freeBankSlot == -1)
        {
            player.LastTickMessage = "Not enough space in your bank.";
            return;
        }
        player.BankItems[freeBankSlot] = itemId;
        player.BankItemsN[freeBankSlot] = amount;
    }
    else
    {
        // Check for overflow before adding
        if (player.BankItemsN[bankSlot] + amount > ItemDefinitionLoader.MaxItemAmount)
        {
            amount = ItemDefinitionLoader.MaxItemAmount - player.BankItemsN[bankSlot];
            if (amount <= 0)
            {
                player.LastTickMessage = "Your bank is full for that item.";
                return;
            }
        }
        player.BankItemsN[bankSlot] += amount;
    }
}
else
{
    player.BankItems[freeBankSlot] = itemId;
    player.BankItemsN[freeBankSlot] = amount;
}

// Only delete from inventory AFTER successful bank operation
playerItems.DeleteItem(player, player.Items[inventorySlot], inventorySlot, amount);
```

## OTHER AREAS CHECKED - CLEAN

### ✅ PlayerItemsService.cs - CLEAN  
- All array bounds properly checked
- Stackable item logic handles overflow correctly with `Math.Min(ItemDefinitionLoader.MaxItemAmount, ...)`
- Item deletion logic is safe and atomic

### ✅ PlayerEquipmentService.cs - CLEAN
- Equipment slot bounds checking is comprehensive  
- Two-handed weapon logic properly validates inventory space before unequipping
- Ancient staff magic interface switching is properly implemented

### ✅ TradingService.cs - CLEAN
- `CompleteTrade()` has proper inventory space validation via `CanReceiveTradeItems()`
- Trade containers are cleared atomically to prevent duplication
- Partner validation includes bidirectional checking

### ✅ GroundItemManager.cs - CLEAN
- Array bounds checking for ground items array
- Proper null checking for ground item states
- Distance validation for pickup operations

## CONCLUSION

**Status:** ONE CRITICAL BUG FOUND  

While the previous 15 rounds of fixes have addressed most issues, a critical banking deposit bug remains that can cause item loss/duplication. This needs immediate attention before the system can be considered production-ready.

The bug affects the core banking functionality and could be exploited to duplicate or lose items, which would severely impact game economy and player trust.

## RECOMMENDATION

**PRIORITY:** IMMEDIATE FIX REQUIRED  
Fix the banking deposit logic to ensure atomic operations - validate bank space BEFORE removing items from inventory.