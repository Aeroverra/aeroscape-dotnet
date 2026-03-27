# AUDIT ROUND 8: Items, Equipment & Banking Services

**Date:** 2026-03-26  
**Round:** 8 of 8 (after 7 rounds of fixes)  
**Scope:** PlayerItemsService.cs + PlayerEquipmentService.cs + PlayerBankService.cs vs Java equivalents

## Critical Bugs Still Present

### 1. **CRITICAL: PlayerItemsService Missing Key Java Functionality**

**HasItemAmount() Logic Completely Wrong**  
**File:** `PlayerItemsService.cs:9-10`  
**Java:** `PlayerItems.java:30-38`

**C# Implementation:**
```csharp
public bool HasItemAmount(Player player, int itemId, int amount) => InvItemCount(player, itemId) >= amount;
```

**Java Implementation:**
```java
public boolean HasItemAmount(Player p, int itemID, int itemAmount) {
    int playerItemAmountCount = 0;
    for (int i = 0; i < p.items.length; i++) {
        if (p.items[i] == itemID) {
            playerItemAmountCount = p.items[i];  // BUG: Should be p.itemsN[i]
        }
        if (playerItemAmountCount >= itemAmount) {
            return true;
        }
    }
    return false;
}
```

**Issue:** The Java code has a bug (assigns item ID instead of amount), but C# completely reimplemented it differently. While C# logic is correct, it's not faithful to the Java port requirement.

**Missing Method: getaxesid()**  
**Java:** `PlayerItems.java:50-59`  
**C#:** Missing entirely

Java has a specific method for checking axe ownership for woodcutting that C# doesn't implement.

### 2. **MAJOR: PlayerEquipmentService Frame Update Inconsistency**  

**Missing Inventory Frame Updates in Equip()**  
**File:** `PlayerEquipmentService.cs:112`  
**Java:** `Equipment.java:252-253`

**Java updates inventory frames after equipping:**
```java
if (p.animTimer <= 0 && canEquip) {
    p.frames.setItems(p, 387, 28, 94, p.equipment, p.equipmentN);
}
```

**C# only updates appearance but not inventory interface frames after equipment changes.**

### 3. **CRITICAL: PlayerBankService Wrong Free Slot Calculation**

**RefreshBankUi() Wrong Free Slot Count Logic**  
**File:** `PlayerBankService.cs:242-250`  
**Java:** `PlayerBank.java:72-76`

**C# Implementation:**
```csharp
int freeSlots = 0;
for (var i = 0; i < Size; i++)
{
    if (player.BankItems[i] == -1)
    {
        freeSlots++;
    }
}
player.BankFreeSlotCount = freeSlots;
```

**Java Implementation:**
```java
p.frames.setString(p, "" + getFreeBankSlot(p), 762, 97);
```

**Issue:** Java sends the INDEX of first free slot to UI, but C# calculates and stores the COUNT of free slots. This mismatch will break bank UI display showing wrong free slot information.

## Bugs Previously Reported But Appear Fixed ✅

### PlayerEquipmentService Fixed Issues:
1. ✅ **Ancient staff magic interface switching** - Now properly implemented at lines 126-136
2. ✅ **3rd age attack requirements** - Now properly validated in `GetAttackRequirementByName()`
3. ✅ **Missing frame updates in RefreshBankUi()** - Now properly implemented at lines 254-261

### PlayerBankService Fixed Issues:
1. ✅ **Tab switching logic** - Comment removed, stays on current tab (line 45-46)
2. ✅ **Frame updates in banking** - Now properly implemented in `RefreshBankUi()`

## Summary

**3 bugs still present** after 7 rounds of fixes:
- **2 Critical**: HasItemAmount wrong logic, Bank free slot index/count mismatch
- **1 Major**: Missing inventory frame updates in equipment
- **1 Missing Feature**: getaxesid() method for woodcutting

**Key fixes verified working:**
- Ancient staff interface switching ✅
- 3rd age item requirements ✅  
- Banking frame updates ✅
- Bank tab switching behavior ✅

## Priority Recommendations

1. **CRITICAL**: Fix bank free slot calculation to send slot INDEX not COUNT to match Java UI expectations
2. **CRITICAL**: Implement faithful HasItemAmount() port or document deviation
3. **HIGH**: Add missing inventory frame updates after equipment changes
4. **MEDIUM**: Consider implementing getaxesid() method for woodcutting feature parity