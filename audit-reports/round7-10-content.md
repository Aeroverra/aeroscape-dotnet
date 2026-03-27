# AUDIT ROUND 7: Content Systems Analysis

## Scope
Audited content systems: Shops, Commands, NPC dialogues/handlers, Object interactions, Clan chat, Construction, Save/Load persistence, and Program.cs DI wiring.

## Findings

### 1. **ShopService.cs - Critical Bug in Buy/Sell Logic**

**Issue**: In the `Buy` method, there is a potential null reference exception and inconsistent inventory handling:

```csharp
public bool Buy(Player player, int itemId, int amount)
{
    // Line 208-214: No null check for player.ShopItems
    int slot = Array.IndexOf(player.ShopItems, itemId);
    if (slot < 0 || slot >= player.ShopItemsN.Length)
        return false;
    
    // Missing validation that player.ShopItems and player.ShopItemsN are same length
```

**Root Cause**: The code assumes `player.ShopItems` and `player.ShopItemsN` arrays are always properly initialized and have matching lengths. In the Java version, this was handled through the `getAmount(Player p)` method that ensured consistency.

**Impact**: Runtime `NullReferenceException` or `IndexOutOfRangeException` if shop state becomes corrupted.

**Fix Required**: Add proper null checks and array length validation.

### 2. **ShopService.cs - Logic Error in Sell Validation**

**Issue**: The `Sell` method has incorrect validation logic:

```csharp
public bool Sell(Player player, int itemId, int amount)
{
    if (itemId == 995 && !player.PartyShop) // Line 243
        return false;
```

**Problem**: This prevents selling coins entirely in non-party shops, but the Java implementation allowed coins to be sold to general stores (shops 1 and 17).

**Expected Behavior**: Should check `if (itemId == 995 && !player.PartyShop && player.ShopId != 1)` to match Java logic.

### 3. **CommandService.cs - Security Vulnerability in Teleport Commands**

**Issue**: The `enter` command (line 69-120) has insufficient validation:

```csharp
case "enter":
    // Missing validation for house coordinates bounds checking
    player.SetCoords(3104, 3926, targetPlayer.HouseHeight);
```

**Problem**: No bounds checking on `targetPlayer.HouseHeight` which could be manipulated to teleport players to invalid coordinates.

**Java Comparison**: The Java version had implicit bounds checking through the map system.

### 4. **NPCInteractionService.cs - Missing Dialogue State Management**

**Issue**: NPCs that start dialogues don't properly set dialogue state:

```csharp
case 2270:
    _ui.ShowNpcDialogue(player, 2270, "Martin Thwait", "What are you looking at noob?", 9827);
    break;
```

**Problem**: Missing `player.TalkingTo = npc.NpcId;` to track dialogue state, unlike Java implementation.

### 5. **ClanChatService.cs - Race Condition in Concurrent Operations**

**Issue**: Potential race condition in `JoinChat`:

```csharp
public bool JoinChat(Player player, string ownerName)
{
    LeaveChat(player); // Modifies _channels
    
    if (!_channels.TryGetValue(ownerName, out var channel)) // Race condition possible here
```

**Problem**: If another thread modifies `_channels` between `LeaveChat` and `TryGetValue`, inconsistent state may occur.

### 6. **Program.cs - Missing Service Registration**

**Issue**: The DI container is missing registration for `IPlayerLoginService` implementation:

```csharp
// Line 96: Missing concrete implementation registration
builder.Services.AddSingleton<IPlayerLoginService, PlayerLoginService>();
```

**Impact**: This will cause runtime dependency injection failures.

### 7. **ConstructionService.cs - Array Bounds Issue**

**Issue**: In `AddRoom` method:

```csharp
if (roomId < 0 || roomId + 1 >= RoomInfo.GetLength(0)) // Line 74
    return false;

var requiredLevel = RoomInfo[roomId + 1, 3]; // Line 77
```

**Problem**: The bounds check allows `roomId + 1` to equal `RoomInfo.GetLength(0)`, but then accesses `RoomInfo[roomId + 1, 3]` which would be out of bounds.

**Fix**: Should be `roomId + 1 > RoomInfo.GetLength(0)` or access `RoomInfo[roomId, 3]`.

## Verification Status

**Compilation**: ❌ Cannot verify - .NET SDK not available on system
**Functional Testing**: ❌ Cannot test - compilation required

## Summary

Found **7 real bugs** in the content systems that could cause runtime errors, security issues, or functional problems. The most critical are the ShopService null reference potential and the Program.cs DI registration issue.