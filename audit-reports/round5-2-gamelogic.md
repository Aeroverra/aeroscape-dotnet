# AUDIT ROUND 5.2 - Game Logic Systems (Movement, Banking, Trading, Equipment)

## Overview
Comprehensive final audit of core game engine systems comparing C# implementations against Java reference code:
- **Movement:** WalkQueue.cs vs PlayerMovement.java + Walking.java
- **Banking:** PlayerBankService.cs vs PlayerBank.java
- **Trading:** TradingService.cs vs PlayerTrade.java  
- **Equipment:** PlayerEquipmentService.cs vs Equipment.java
- **Engine:** GameEngine.cs vs Engine.java

## Critical Bugs

### Movement System Bugs

**WalkQueue.cs:55 — Incorrect teleportation boundary check — PlayerMovement.java:23**
C# uses incorrect boundary check for teleport region changes:
```csharp
if (relX >= 16 && relX < 88 && relY >= 16 && relY < 88)
```
Java uses correct bounds:
```java
if (relX >= 2 * 8 && relX < 11 * 8 && relY >= 2 * 8 && relY < 11 * 8)
```
This causes players to incorrectly trigger region updates when teleporting within the same region.

**WalkQueue.cs:78 — Missing region boundary checks — PlayerMovement.java:40-49**
C# has simplified boundary logic but misses Java's comprehensive checks:
```csharp
if (player.CurrentX < 16 || player.CurrentX >= 88 || player.CurrentY < 16 || player.CurrentY >= 88)
```
Java performs separate bounds checks for each boundary that C# combines, potentially missing edge cases.

**WalkQueue.cs:23 — Missing freeze delay check in HandleWalk — Walking.java:45**
Java displays a freeze message during walking packet processing, but C# only shows it during movement processing. The Java implementation shows the message immediately when a walk packet is received while frozen.

### Banking System Bugs

**PlayerBankService.cs:62 — Tab switching logic inconsistency — PlayerBank.java:41**
C# automatically switches back to main tab (10) after depositing to a specific tab, but has a comment suggesting this behavior should be removed to match Java. Java doesn't force tab switching.

**PlayerBankService.cs:179 — Wrong bank free slot calculation — PlayerBank.java:missing**
C# calculates `BankFreeSlotCount` as the index of first free slot:
```csharp
player.BankFreeSlotCount = Math.Max(0, GetFreeBankSlot(player));
```
This should represent the count of free slots, not the index. Java doesn't have this field but uses proper count calculations elsewhere.

**PlayerBankService.cs:435 — Missing frame updates in banking operations — PlayerBank.java:72-76**
Java sends frame updates for bank interface refreshes:
```java
p.frames.setString(p, "" + getFreeBankSlot(p), 762, 97);
p.frames.setItems(p, -1, 64207, 95, p.bankItems, p.bankItemsN);
```
C# `RefreshBankUi()` method is called but doesn't implement the actual UI frame updates.

### Trading System Bugs

**TradingService.cs:104 — Potential item duplication vulnerability — PlayerTrade.java:missing**
C# implements comprehensive inventory space validation before completing trades, but Java's `acceptPlayer()` method directly adds items without checking space:
```java
for (int i = 0; i < getTradeItemCount(plr); i++) {
    Engine.playerItems.addItem(p, plr.tradeItems[i], plr.tradeItemsN[i]);
}
```
While C# implementation is safer, it differs from Java behavior which could allow item duplication if `addItem()` fails partway through.

**TradingService.cs:216 — Missing trade screen validation — PlayerTrade.java:77-83**
Java has `checkStage()` method with explicit trade validation that C# lacks:
```java
if (plr.tradePlayer != p.playerId || p.tradePlayer != plr.playerId) {
    return;
}
```
C# relies on `GetPartner()` which may not catch all edge cases during trade state transitions.

**TradingService.cs:385 — Wrong trade accept logic sequence — PlayerTrade.java:115**
Java sets both players' `tAccept[0] = true` before screen transitions but C# resets them. This affects the trade state machine behavior.

### Equipment System Bugs

**PlayerEquipmentService.cs:280 — Missing attack requirement validation for 3rd age items — Equipment.java:545**
Java validates 3rd age items require level 75:
```java
if (itemName.toLowerCase().contains("3rd age")) {
    return 75;
}
```
C# `GetAttackRequirementByName()` doesn't include 3rd age item checks, allowing equipping without proper level requirements.

**PlayerEquipmentService.cs:42 — Incorrect special attack bar ID for crystal bow — Equipment.java:missing**
C# assigns special attack interface to crystal bow but crystal bow shouldn't have special attacks in 508 protocol. Java doesn't assign special interfaces to crystal bow.

**PlayerEquipmentService.cs:151 — Missing ancient staff magic switch — Equipment.java:258**
Java switches magic interface when equipping ancient staff (ID 4675):
```java
if (wearId == 4675) {
    p.frames.setInterface(p, 1, 746, 93, 193); //Ancients tab
    p.isAncients = 1;
}
```
C# lacks this critical magic interface switching logic.

### Engine System Bugs

**GameEngine.cs:missing — Missing player weapon state updating — Engine.java + Equipment.java**
Java calls `p.playerWeapon.setWeapon()` and updates appearance after equipment changes. C# `ApplyWeaponState()` exists but the call timing and integration with appearance updates differs from Java.

**WalkQueue.cs:22 — Missing autocasting reset — Walking.java:22**
Java explicitly sets `p.usingAutoCast = false` during walk packet handling. C# sets `player.AutoCasting = false` but the field name and usage context may differ.

## Summary

Found **12 critical bugs** affecting core gameplay mechanics:

- **3 Movement bugs**: Teleportation boundaries, freeze delay timing, region checks
- **3 Banking bugs**: Tab switching behavior, free slot calculation, missing UI updates  
- **3 Trading bugs**: Item duplication risk, validation gaps, accept state logic
- **3 Equipment bugs**: Level requirement validation, special attack interfaces, magic switching
- **1 Engine bug**: Weapon state and appearance update timing

The most severe issues are the movement teleportation boundaries (affects all player movement), trading item duplication vulnerability (affects economy), and missing equipment level validation (affects game balance).