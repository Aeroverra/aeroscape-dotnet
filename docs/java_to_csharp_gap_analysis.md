# Java → C# Gap Analysis Report

**Generated:** 2026-03-26  
**Java Source:** `legacy-java/server508/` (DavidScape 508 — ~90 Java files, ~15,000+ lines)  
**C# Target:** `AeroScape.Server.*` (4 projects — ~110 C# files)  

---

## Executive Summary

The C# rewrite has successfully established the **architectural skeleton**: clean project separation (App, Core, Data, Network), async packet routing via DI, protocol dictionary from JSON, ISAAC cipher, entity models (Player/NPC), database models via EF Core, and the outgoing frame writer. **All 40+ incoming packet decoders** have been ported with correct byte-level parsing.

However, virtually **every message handler is a stub** — they log the packet and return `Task.CompletedTask`. The entire game logic layer (combat, skills, items, NPCs, trading, minigames, quests, dialogue, shops, map data, player update, NPC update, outgoing frames beyond login) is **missing**. The codebase can accept a login and send the initial interface setup, but cannot run a playable game.

**Coverage estimate: ~15-20% of functional parity with the Java source.**

---

## Table of Contents

1. [Architecture & Infrastructure](#1-architecture--infrastructure)
2. [Networking & Protocol Layer](#2-networking--protocol-layer)
3. [Outgoing Frames (Server → Client)](#3-outgoing-frames-server--client)
4. [Game Engine / Tick Loop](#4-game-engine--tick-loop)
5. [Player Entity & State](#5-player-entity--state)
6. [NPC Entity & State](#6-npc-entity--state)
7. [Player Update Protocol](#7-player-update-protocol)
8. [NPC Update Protocol](#8-npc-update-protocol)
9. [Movement & Pathfinding](#9-movement--pathfinding)
10. [Combat System](#10-combat-system)
11. [Magic System](#11-magic-system)
12. [Skills](#12-skills)
13. [Items & Inventory Management](#13-items--inventory-management)
14. [Banking](#14-banking)
15. [Equipment & Bonuses](#15-equipment--bonuses)
16. [Ground Items](#16-ground-items)
17. [Trading](#17-trading)
18. [Shops](#18-shops)
19. [NPC Interactions & Dialogues](#19-npc-interactions--dialogues)
20. [Commands](#20-commands)
21. [Friends & Ignores / Private Messaging](#21-friends--ignores--private-messaging)
22. [Clan Chat](#22-clan-chat)
23. [Prayer](#23-prayer)
24. [Quests](#24-quests)
25. [Minigames](#25-minigames)
26. [Construction (Player-Owned Houses)](#26-construction-player-owned-houses)
27. [Summoning](#27-summoning)
28. [Slayer](#28-slayer)
29. [Object Interactions](#29-object-interactions)
30. [Map Data & Region Loading](#30-map-data--region-loading)
31. [Player Save/Load (Persistence)](#31-player-saveload-persistence)
32. [File & Data Management](#32-file--data-management)
33. [Connection & Session Management](#33-connection--session-management)
34. [Miscellaneous Missing Systems](#34-miscellaneous-missing-systems)

---

## 1. Architecture & Infrastructure

| Aspect | Java | C# | Status |
|--------|------|-----|--------|
| Project structure | Monolithic single package | 4-project solution (App, Core, Data, Network) | ✅ **Improved** |
| Dependency injection | None (static singletons) | Full ASP.NET Core DI | ✅ **Improved** |
| Async I/O | Blocking socket reads | Async pipelines | ✅ **Improved** |
| Configuration | Hardcoded constants | JSON protocol dictionary | ✅ **Improved** |
| Database | Flat-file text save/load | EF Core with models | ✅ **Improved** |
| Game tick scheduling | `Engine.cycle()` with Thread.sleep(600) | `GameEngine` with `Task.Delay` | ⚠️ **Skeleton only** |

**Verdict:** Architecture is solid. The problem is all the logic that sits on top of it.

---

## 2. Networking & Protocol Layer

### Incoming Packets (Client → Server)

| Feature | Java | C# | Status |
|---------|------|-----|--------|
| Packet size table (256 entries) | `Packets.setPacketSizes()` hardcoded | `Protocol_508.json` + `ProtocolDictionary` | ✅ Done |
| ISAAC cipher decryption | In `PlayerSocket` | `IsaacCipher` + `PacketRouter` | ✅ Done |
| Packet framing & routing | `Packets.parseIncomingPackets()` → `PacketManager.parsePacket()` | `PacketRouter.ProcessBufferAsync()` | ✅ Done |
| Per-packet decoders | Inline reads in switch/case | 40+ `IPacketDecoder` implementations | ✅ Done |
| Click delay gate (blocking packets during delay) | `p.clickDelay > 0 && packetId != 222` | ❌ Not implemented | 🔴 **Missing** |
| 10-packet-per-cycle cap | `for (int i = 0; i < 10; i++)` | Same cap in `ParsePackets` | ✅ Done |

### Packet Decode Accuracy (opcode-by-opcode)

All 40+ decoders have been checked against the Java byte-reading order. Decoding is **correct** for:

- Walk (49/119/138), PublicChat (222), Command (107), ActionButtons (21/113/169/173/232/233)
- EquipItem (3), ItemOperate (186), DropItem (211), PickupItem (201)
- PlayerOption 1/2/3 (160/37/227), NPCAttack (123), NPCOption 1/2/3 (7/52/199)
- ObjectOption 1/2 (158/228), SwitchItems (167), SwitchItems2 (179)
- ItemOnItem (40), ItemSelect (220/134), ItemOption1 (203/152), ItemGive (131)
- MagicOnNPC (24), MagicOnPlayer (70), ItemOnObject (224), ItemOnNPC
- AddFriend (30), RemoveFriend (132), AddIgnore (61), RemoveIgnore (2)
- PrivateMessage (178), Idle (47), DialogueContinue (63), CloseInterface (108)
- ItemExamine (38), NpcExamine (88), ObjectExamine (84), TradeAccept (253)

**Missing packet decoders for inline-handled opcodes:**
| Opcode | Java Function | C# Status |
|--------|--------------|-----------|
| 42 | Clan chat join/leave (QWord name) | 🔴 No decoder |
| 127 | String input response | 🔴 No decoder |
| 189 | Long input (clan name setting) | 🔴 No decoder |
| 200 | Clan kick (QWord name) | 🔴 No decoder |
| 43 | User integer input | 🔴 No decoder |
| 190 | Construction build object option | 🔴 No decoder |
| 22 | Update request acknowledgment | 🔴 No decoder |
| 60/62 | Map region entered / object spawn trigger | 🔴 No decoder |
| 154 | Magic on item | 🔴 No decoder |
| 94 | Object select 2 (farming patches) | 🔴 No decoder |
| 59 | Mouse click (anticheat) | 🔴 No decoder |
| 99 | Unknown 4-byte packet | 🔴 No decoder |
| 115 | Ping (0-byte) | 🔴 No decoder (harmless) |
| 117/247/248 | Skill cape auto-trim trigger | 🔴 No decoder |
| 165 | Settings buttons (music volume etc.) | 🔴 No decoder |

---

## 3. Outgoing Frames (Server → Client)

### Login Sequence Frames

| Frame | Purpose | Java (`Frames.java`) | C# (`LoginFrames.cs`) | Status |
|-------|---------|---------------------|----------------------|--------|
| 239 | setWindowPane | ✅ | ✅ | ✅ Done |
| 93 | setInterface | ✅ | ✅ | ✅ Done |
| 179 | setString | ✅ | ✅ | ✅ Done |
| 115 | connectToFriendServer | ✅ | ✅ | ✅ Done |
| 142 | setMapRegion | ✅ | ⚠️ Sends zeros for map data | ⚠️ **Partial** |
| 100/161 | setConfig | ✅ | ✅ | ✅ Done |
| 217 | setSkillLvl | ✅ | ✅ | ✅ Done |
| 255 | setItems | ✅ | ✅ | ✅ Done |
| 252 | setPlayerOption | ✅ | ✅ | ✅ Done |
| 99 | setEnergy | ✅ | ✅ | ✅ Done |
| 218 | sendMessage | ✅ | ✅ | ✅ Done |

### In-Game Outgoing Frames (NOT in LoginFrames)

| Frame ID | Purpose | Java | C# | Status |
|----------|---------|------|----|--------|
| 216 | Player movement/teleport update | `Frames.updateMovement()` / `teleport()` / `noMovement()` | ❌ | 🔴 **Missing** |
| Player update packet | Full player list update (add/remove/mask) | `PlayerUpdate.java` (215 lines) | ❌ | 🔴 **CRITICAL** |
| NPC update packet | Full NPC list update | `NPCUpdate.java` (215 lines) | ❌ | 🔴 **CRITICAL** |
| 30 | createObject | `Frames.createObject()` | ❌ | 🔴 Missing |
| 25 | createGroundItem | `Frames.createGroundItem()` | ❌ | 🔴 Missing |
| 201 | removeGroundItem | `Frames.removeGroundItem()` | ❌ | 🔴 Missing |
| 177 | sendCoords (used by ground items, objects) | `Frames.sendCoords()` | ❌ | 🔴 Missing |
| 112 | createProjectile | `Frames.createProjectile()` | ❌ | 🔴 Missing |
| 104 | logout | `Frames.logout()` | ❌ | 🔴 Missing |
| 119 | playSound | `Frames.playSound()` | ❌ | 🔴 Missing |
| 6 | setNPCId (chatbox NPC head) | `Frames.setNPCId()` | ❌ | 🔴 Missing |
| 245 | animateInterfaceId | `Frames.animateInterfaceId()` | ❌ | 🔴 Missing |
| 59 | setInterfaceConfig (hide/show) | `Frames.setInterfaceConfig()` | ❌ | 🔴 Missing |
| 223 | setAccessMask (bank options) | `Frames.setAccessMask()` | ❌ | 🔴 Missing |
| 35 | itemOnInterface | `Frames.itemOnInterface()` | ❌ | 🔴 Missing |
| 89 | sendSentPrivateMessage | `Frames.sendSentPrivateMessage()` | ❌ | 🔴 Missing |
| 178 | sendReceivedPrivateMessage | `Frames.sendReceivedPrivateMessage()` | ❌ | 🔴 Missing |
| 154 | sendFriend (online status) | `Frames.sendFriend()` | ❌ | 🔴 Missing |
| 240 | sendIgnores | `Frames.sendIgnores()` | ❌ | 🔴 Missing |
| 229 | sendClanChat | `Frames.sendClanChat()` | ❌ | 🔴 Missing |
| 82 | resetList (clan) | `Frames.resetList()` | ❌ | 🔴 Missing |
| 135 | removeEquipment (item update) | `Frames.removeEquipment()` | ❌ | 🔴 Missing |
| 57 | teleportOnMapdata | `Frames.teleportOnMapdata()` | ❌ | 🔴 Missing |
| 173 | sendMapRegion2 (construction) | `Frames.sendMapRegion2()` | ❌ | 🔴 Missing |
| 246 | removeChatboxInterface | `Frames.removeChatboxInterface()` | ❌ | 🔴 Missing |
| 152 | runScript | `Frames.runScript()` | ❌ | 🔴 Missing |

**Summary: 2 of ~25+ outgoing frame types implemented (login only).**

---

## 4. Game Engine / Tick Loop

| Feature | Java (`Engine.java` — 734 lines) | C# (`GameEngine.cs`) | Status |
|---------|----------------------------------|---------------------|--------|
| 600ms tick cycle | `Thread.sleep(600)` | `Task.Delay(600)` | ⚠️ Skeleton |
| Player processing per tick | Player movement, combat delay, eat/drink timers, prayer drain, stat restore, death handling, poison, skull timer, freeze, special regen, teleport sequence, fire delay, run energy regen, home tele, agility, cooking/fishing/fletching/smithing/herblore timers, woodcutting/mining process, duel timer, overlay timer, idle timeout, save timer | ❌ None of this | 🔴 **CRITICAL** |
| NPC processing per tick | NPC movement (random walk), NPC combat (attack player), NPC death/respawn, NPC loot drops | ❌ | 🔴 **CRITICAL** |
| Player update cycle | Build and send player update packet | ❌ | 🔴 **CRITICAL** |
| NPC update cycle | Build and send NPC update packet | ❌ | 🔴 **CRITICAL** |
| Ground item timers | Item visibility, despawn timing | ❌ | 🔴 Missing |
| Global timers | Castle Wars timer, Fight Pits game timer, Clan Wars timer | ❌ | 🔴 Missing |
| NPC spawning | `Engine.newNPC()`, `newSummonNPC()` | ❌ | 🔴 Missing |
| Player disconnect handling | `Engine.cycle()` disconnect check + save | ❌ | 🔴 Missing |
| Stream flushing | Per-tick byte flush to all players | ❌ | 🔴 Missing |

---

## 5. Player Entity & State

| Feature | Java (`Player.java` — 5148 lines) | C# (`Player.cs`) | Status |
|---------|----------------------------------|------------------|--------|
| Core fields (position, skills, equipment, etc.) | ~200+ fields | ~200+ properties | ✅ Done |
| `getLevelForXP()` | ✅ | ✅ `GetLevelForXP()` | ✅ Done |
| `appendHit()` | ✅ | ✅ `AppendHit()` | ✅ Done |
| `requestAnim/Gfx/FaceTo` | ✅ | ✅ | ✅ Done |
| `addSkillXP()` with level-up detection | ✅ | ✅ | ✅ Done |
| `setCoords()` | ✅ | ✅ | ✅ Done |
| `updateHP()` | ✅ | ✅ | ✅ Done |
| `calculateEquipmentBonus()` | Iterates equipment, looks up item bonuses | `Array.Clear(EquipmentBonus)` — **no actual calculation** | 🔴 **Stub** |
| `teleportTo()` with anim/gfx sequence | Full 4-tick teleport sequence with start/end gfx | ❌ | 🔴 Missing |
| `stopMovement()` | Resets walk queue | ❌ | 🔴 Missing |
| `WalkingTo()` / `reqWalkQueue()` | Walking queue management | ❌ | 🔴 Missing |
| `process()` (per-tick) | Prayer drain, stat restore, poison, death sequence, timers | ❌ | 🔴 Missing |
| `objects()` | Spawns region-specific objects on map load | ❌ | 🔴 Missing |
| `friendsLoggedIn()` | Notifies friends of online status | ❌ | 🔴 Missing |
| `canAttackPlayer()` | Wilderness/multi combat/duel/minigame checks | ❌ | 🔴 Missing |
| `AtDuel()` / `AtPits()` / `AtCastleWars()` / `AtClanField()` / `bountyArea()` | Area detection methods | ❌ | 🔴 Missing |
| `combatLevel` calculation | Formula from skill levels | ❌ | 🔴 Missing |
| `setscores()` (high scores overlay) | ✅ | ❌ | 🔴 Missing |
| `gettotalz()` total level | ✅ | ✅ `GetTotalLevel()` | ✅ Done |

---

## 6. NPC Entity & State

| Feature | Java (`NPC.java` — 489 lines) | C# (`NPC.cs`) | Status |
|---------|-------------------------------|---------------|--------|
| Core fields | ✅ | ✅ | ✅ Done |
| `appendHit()` | ✅ | ✅ | ✅ Done |
| `RequestAnim/Gfx/FaceTo` | ✅ | ✅ | ✅ Done |
| `ClearUpdateMasks()` | ✅ | ✅ | ✅ Done |
| `process()` (per-tick logic) | Follow player, combat, random walk, respawn | Basic `RespawnDelay--` and `CombatDelay--` only | 🔴 **Mostly missing** |
| `appendPlayerFollowing()` | NPC follows a player | ❌ | 🔴 Missing |
| NPC drop table / loot | Extensive drop logic in `Engine.cycle()` | ❌ | 🔴 Missing |
| NPC speech / force chat | ✅ | ✅ `RequestText()` | ✅ Done |

---

## 7. Player Update Protocol

| Feature | Java (`PlayerUpdate.java`, `PlayerUpdateMasks.java`, `PlayerMovement.java`) | C# | Status |
|---------|---------------------------------------------------------------------------|-----|--------|
| Player list management (add/remove nearby) | Full 648-line implementation | ❌ | 🔴 **CRITICAL** |
| Bit-packed movement flags | ✅ | ❌ | 🔴 **CRITICAL** |
| Appearance mask (0x4) | Full appearance block encoding | ❌ | 🔴 **CRITICAL** |
| Animation mask (0x1) | ✅ | ❌ | 🔴 Missing |
| GFX mask (0x200) | ✅ | ❌ | 🔴 Missing |
| Force chat mask (0x20) | ✅ | ❌ | 🔴 Missing |
| Chat text mask (0x40) | ✅ | ❌ | 🔴 Missing |
| Face entity mask (0x2) | ✅ | ❌ | 🔴 Missing |
| Hit masks (0x100, 0x8) | ✅ | ❌ | 🔴 Missing |

**Without this, the client cannot see any player (including itself) in the game world.**

---

## 8. NPC Update Protocol

| Feature | Java (`NPCUpdate.java`, `NPCUpdateMasks.java`, `NPCMovement.java`) | C# | Status |
|---------|-------------------------------------------------------------------|-----|--------|
| NPC list build/update | Full implementation | ❌ | 🔴 **CRITICAL** |
| NPC movement encoding | ✅ | ❌ | 🔴 Missing |
| All NPC update masks | Animation, GFX, Hit, FaceTo, FaceCoords, ForceChat | ❌ | 🔴 Missing |
| `rebuildNPCList` flag | ✅ | Property exists but never used | 🔴 Missing |

---

## 9. Movement & Pathfinding

| Feature | Java | C# | Status |
|---------|------|----|--------|
| Walk queue (50-step ring buffer) | `Player.java` + `Walking.java` + `PlayerMovement.java` | Fields exist in `Player.cs`, no logic | 🔴 **Missing** |
| Running (double-step) | ✅ with energy drain | Properties exist, no logic | 🔴 Missing |
| Run energy regeneration | In `Engine.cycle()` | ❌ | 🔴 Missing |
| Player following | `PlayerFollow.java` (60 lines) | ❌ | 🔴 Missing |
| NPC following player | `NPC.appendPlayerFollowing()` | ❌ | 🔴 Missing |
| NPC random walk | `NPCMovement.java` (218 lines) | ❌ | 🔴 Missing |
| Map region change detection | In movement processing | ❌ | 🔴 Missing |

---

## 10. Combat System

### Player vs Player (PvP)

| Feature | Java (`PlayerCombat.java` — 488 lines) | C# | Status |
|---------|----------------------------------------|-----|--------|
| Melee hit formula | `maxMeleeHit()`: `floor(str * (bonus * 0.00175 + 0.1) + 2.05)` | ❌ | 🔴 Missing |
| Attack styles & XP distribution | Accurate/Strength/Defence/Controlled | ❌ | 🔴 Missing |
| Ranged combat | Bow detection, arrow consumption, projectile creation, arrow GFX | ❌ | 🔴 Missing |
| Special attacks (13 weapons) | AGS (1.6x), BGS, SGS, ZGS, Whip, Dragon claws (4-hit), DDS, D long, D scim, D mace, D halberd, Sara sword, Anger weapons | ❌ | 🔴 Missing |
| Wilderness level check | `wildLvl()` + `isInWildRange()` | ❌ | 🔴 Missing |
| Vengeance recoil | `p2.vengOn` → 75% recoil | ❌ | 🔴 Missing |
| Prayer protection | Melee/range prayer partially blocks hits via `Hitter` counter | ❌ | 🔴 Missing |
| Auto-retaliate | ✅ | Property exists, no logic | 🔴 Missing |
| Bounty Hunter area check | ❌ | ❌ | 🔴 Missing |
| Duel Arena restrictions | ❌ | ❌ | 🔴 Missing |
| Castle Wars team check | ❌ | ❌ | 🔴 Missing |
| Clan Wars team check | ❌ | ❌ | 🔴 Missing |

### Player vs NPC (PvE)

| Feature | Java (`PlayerNPCCombat.java` — 512 lines) | C# | Status |
|---------|------------------------------------------|-----|--------|
| Melee attack on NPC | Full special attack support (same 13 weapons) | ❌ | 🔴 Missing |
| Ranged attack on NPC | Arrow consumption, projectile, XP | ❌ | 🔴 Missing |
| Range hit formula | `maxRangeHit()` | ❌ | 🔴 Missing |
| Barrows brother kill tracking | `p.barrows[0-5]` + kill count message | ❌ | 🔴 Missing |
| GWD kill count (Zamorak, Sara, Bandos, Armadyl) | Per-boss faction kill counters | ❌ | 🔴 Missing |
| Slayer task kill tracking | Task type → NPC type matching, XP reward | ❌ | 🔴 Missing |
| Dragon Slayer quest kill (Elvarg) | NPC 742 death → quest progress | ❌ | 🔴 Missing |
| Auto-cast magic during melee | `p.magicNPC.autoCasting` check | ❌ | 🔴 Missing |

### NPC vs Player

| Feature | Java (`NPCPlayerCombat.java` — 89 lines) | C# | Status |
|---------|------------------------------------------|-----|--------|
| NPC attacks player | Melee/range/magic NPC attacks | ❌ | 🔴 Missing |
| NPC aggression | Attack nearby players | ❌ | 🔴 Missing |

---

## 11. Magic System

### Magic on NPC (`MagicNPC.java` — 538 lines)

| Feature | Java | C# | Status |
|---------|------|----|--------|
| 16 standard spellbook spells (Wind/Water/Earth/Fire × Strike/Bolt/Blast/Wave) | Full implementation with rune costs, level requirements, damage formulas, XP | ❌ | 🔴 Missing |
| Auto-casting | `autoCasting` toggle + `autoCastSpell` | ❌ | 🔴 Missing |
| Elemental staff rune removal | Checks weapon slot for staff, removes matching rune requirement | ❌ | 🔴 Missing |
| Spell GFX (caster + victim) | Per-spell graphic IDs | ❌ | 🔴 Missing |
| Bonus damage from magic equipment | `getBonusDamage()` formula | ❌ | 🔴 Missing |

### Magic on Player (`Magic.java` — extensive)

| Feature | Java | C# | Status |
|---------|------|----|--------|
| Teleport spells (Varrock, Lumby, Falador, Camelot, etc.) | Full rune costs + level checks | ❌ | 🔴 Missing |
| Ancient Magicks teleports | ✅ | ❌ | 🔴 Missing |
| Lunar spells | Vengeance, home teleport | ❌ | 🔴 Missing |
| High/Low alchemy | Rune consumption + GP generation | ❌ | 🔴 Missing |
| Bones to Bananas/Peaches | ✅ | ❌ | 🔴 Missing |
| Superheat Item | ✅ | ❌ | 🔴 Missing |
| Enchant spells (sapphire → onyx) | Bolt enchanting, jewellery enchanting | ❌ | 🔴 Missing |
| Magic on item (opcode 154) | `MagicOnItemHandle` | ❌ | 🔴 Missing |

---

## 12. Skills

### Woodcutting (`Woodcutting.java` — full implementation)

| Feature | Java | C# | Status |
|---------|------|----|--------|
| Tree types (8: Normal→Magic) | Object ID → tree ID mapping | ❌ | 🔴 Missing |
| Axe detection (inventory + equipment, 8 types) | Bronze through Dragon | ❌ | 🔴 Missing |
| Axe-specific animation + speed | Per-axe anim ID and speed | ❌ | 🔴 Missing |
| Level requirements per tree | 1 (Normal) through 75 (Magic) | ❌ | 🔴 Missing |
| XP per log type | 50 (Normal) through 500 (Magic) | ❌ | 🔴 Missing |
| Tick-based processing | `logTimer`, `secondtimer`, `process()` | Timer fields exist in Player, no processing | 🔴 Missing |
| Log item granting | Correct log item IDs | ❌ | 🔴 Missing |

### Mining (`Mining.java` — full implementation)

| Feature | Java | C# | Status |
|---------|------|----|--------|
| Rock types (12: Copper→Rune Essence) | Object ID → rock mapping | ❌ | 🔴 Missing |
| Pickaxe detection (6 types) | Bronze through Rune | ❌ | 🔴 Missing |
| Pickaxe-specific animation + speed | Per-pickaxe values | ❌ | 🔴 Missing |
| Ore level requirements | 1 (Copper) through 80 (Runite) | ❌ | 🔴 Missing |
| Ore XP values | 50 (Copper) through 600 (Runite) | ❌ | 🔴 Missing |
| Tick-based processing | `oreTimer`, `secondtimer`, `process()` | ❌ | 🔴 Missing |

### Fishing (`Fishing.java`)

| Feature | Java | C# | Status |
|---------|------|----|--------|
| 4 fishing methods (net/bait/lure/harpoon) | Different animations per method | ❌ | 🔴 Missing |
| Bait consumption | Fishing bait (313) consumed per catch | ❌ | 🔴 Missing |
| Fish type rewards | Via `FishMan` player field | ❌ | 🔴 Missing |
| XP formula | `FishXP * skillLvl[10] / 3` | ❌ | 🔴 Missing |
| Tick-based processing | `FishTimer` countdown | Timer field exists, no processing | 🔴 Missing |

### Smithing (`Smithing.java` — massive, ~1500+ lines)

| Feature | Java | C# | Status |
|---------|------|----|--------|
| 6 metal types (Bronze → Rune) | Full bar → item mappings | ❌ | 🔴 Missing |
| 28 item types per metal | Dagger, axe, mace, helm, bolts, sword, dart tips, nails, arrow tips, scimitar, crossbow limbs, longsword, throwing knives, full helm, sq shield, warhammer, battleaxe, chainbody, kite shield, claws, 2h sword, plateskirt, platelegs, platebody, pickaxe, etc. | ❌ | 🔴 Missing |
| Bar cost per item (1-5 bars) | Per-button mapping | ❌ | 🔴 Missing |
| Level requirements per metal tier | Bronze 1-18, Iron 15-33, Steel 30-48, Mithril 50-68, Adamant 70-88, Rune 85-99 | ❌ | 🔴 Missing |
| Smithing interface (300) | Items on interface, level-colored names, bar count display | ❌ | 🔴 Missing |
| XP formula | `AmoutOfBars * XpBar / 10 * xprate / 4` | ❌ | 🔴 Missing |

### Cooking (referenced in Engine.java)

| Feature | Java | C# | Status |
|---------|------|----|--------|
| Cooking timer processing | `p.CookTimer` countdown in Engine tick | Timer field exists, no processing | 🔴 Missing |
| Cooked item IDs | Various raw → cooked mappings | ❌ | 🔴 Missing |
| Cooking range/fire detection | Object interaction | ❌ | 🔴 Missing |

### Other Skills (referenced but minimal implementation in Java)

| Skill | Java Status | C# Status |
|-------|------------|-----------|
| Firemaking | Timer field, basic log→fire | 🔴 Missing |
| Fletching | Timer field in Engine tick | 🔴 Missing |
| Herblore | Timer field in Engine tick | 🔴 Missing |
| Crafting | Referenced in NPC dialogues | 🔴 Missing |
| Runecrafting | Referenced in NPC dialogues, altar teleport | 🔴 Missing |
| Agility | Timer/XP fields, course handling | 🔴 Missing |
| Thieving | Referenced in skill cape dialogues | 🔴 Missing |
| Farming | Seed planting via NPC dialogue, patch objects | 🔴 Missing |
| Hunter | Net item given via NPC dialogue | 🔴 Missing |

---

## 13. Items & Inventory Management

| Feature | Java (`PlayerItems.java` — 314 lines) | C# | Status |
|---------|---------------------------------------|-----|--------|
| `addItem()` — stackable + non-stackable | Handles noted items, stackable checks | ❌ | 🔴 Missing |
| `deleteItem()` | By slot or by search | ❌ | 🔴 Missing |
| `haveItem()` / `HasItemAmount()` | Quantity checks | ❌ | 🔴 Missing |
| `invItemCount()` | Count of specific item | ❌ | 🔴 Missing |
| `freeSlotCount()` | Empty slot counting | ❌ | 🔴 Missing |
| `getItemSlot()` | Find slot by item ID | ❌ | 🔴 Missing |
| Item stacking logic | Stackable item ID check | ❌ | 🔴 Missing |
| Inventory refresh frames | `setItems()` call after changes | ❌ | 🔴 Missing |

---

## 14. Banking

| Feature | Java (`PlayerBank.java` — 356 lines, `BankUtils.java`) | C# | Status |
|---------|-------------------------------------------------------|-----|--------|
| Open bank interface (762/763) | Full interface setup with tab support | ❌ | 🔴 Missing |
| Deposit items (single, 5, 10, all, X) | Multiple deposit modes | ❌ | 🔴 Missing |
| Withdraw items (single, 5, 10, all, X) | Multiple withdraw modes | ❌ | 🔴 Missing |
| Note withdrawal toggle | `withdrawNote` flag | Property exists, no logic | 🔴 Missing |
| Insert mode toggle | `insertMode` flag | Property exists, no logic | 🔴 Missing |
| Bank tabs (10 tabs) | Tab management, tab switching | ❌ | 🔴 Missing |
| Bank search | ❌ (not in legacy) | ❌ | N/A |
| Access masks for bank options | `setBankOptions()` frame | ❌ | 🔴 Missing |

---

## 15. Equipment & Bonuses

| Feature | Java (`PlayerWeapon.java` — 316 lines, equipment handling) | C# | Status |
|---------|----------------------------------------------------------|-----|--------|
| Weapon-specific attack tab interface | Maps weapon type → interface ID (e.g., whip=79, staff=82) | ❌ | 🔴 Missing |
| Attack speed per weapon | Weapon-specific attack delays | ❌ | 🔴 Missing |
| Attack/walk/stand/run emotes per weapon | Per-weapon animation sets | ❌ | 🔴 Missing |
| Equipment bonus calculation | Sum bonuses from all 14 equipment slots | Stub (clears array only) | 🔴 **Stub** |
| Two-handed weapon detection | Unequip shield when equipping 2h | ❌ | 🔴 Missing |
| Equipment requirements (level checks) | Skill cape level validation on login | ❌ | 🔴 Missing |
| Special attack bar toggle | `setInterfaceConfig` for special bar | ❌ | 🔴 Missing |

---

## 16. Ground Items

| Feature | Java (`Items.java`, `GroundItem.java`, `ItemList.java`) | C# | Status |
|---------|------------------------------------------------------|-----|--------|
| Ground item data structure | `GroundItem` class with position, owner, timer | ❌ | 🔴 Missing |
| Item spawning on death/drop | `createGroundItem()` | ❌ | 🔴 Missing |
| Item visibility rules | Owner-only → public after timer | ❌ | 🔴 Missing |
| Item despawn timer | Timed removal | ❌ | 🔴 Missing |
| Item pickup | Remove ground item, add to inventory | Handler is stub | 🔴 Missing |
| Item stacking on ground | Multiple drops at same tile stack | ❌ | 🔴 Missing |
| Item definitions (name, description, stack, note) | `ItemList.java` with file loading | ❌ | 🔴 Missing |

---

## 17. Trading

| Feature | Java (`PlayerTrade.java` — 264 lines, `PTrade.java` — 383 lines, `TButtons.java`, `TItem.java`) | C# | Status |
|---------|------------------------------------------------------------------------------------------------|-----|--------|
| Trade request | Player option 2 → trade initiation | Handler is stub | 🔴 Missing |
| Trade interface (335/334) | First screen with item add/remove | ❌ | 🔴 Missing |
| Second trade screen (confirmation) | Value display, accept/decline | ❌ | 🔴 Missing |
| Item transfer on accept | Swap trade items between players | ❌ | 🔴 Missing |
| Trade decline/cancel | Return items to original owner | ❌ | 🔴 Missing |
| Trade item add (1/5/10/all/X) | Multiple add modes via buttons | ❌ | 🔴 Missing |
| Trade item remove (1/5/10/all/X) | Multiple remove modes | ❌ | 🔴 Missing |

---

## 18. Shops

| Feature | Java (`ShopHandler.java` — 1062 lines) | C# | Status |
|---------|---------------------------------------|-----|--------|
| Shop data loading from file | `loadShops()` from data files | ❌ | 🔴 Missing |
| Shop interface display | Interface 620 with items and prices | ❌ | 🔴 Missing |
| Buy items | GP deduction, item addition | ❌ | 🔴 Missing |
| Sell items | Item removal, GP addition | ❌ | 🔴 Missing |
| Shop stock management | Default stock, restocking timers | ❌ | 🔴 Missing |
| Multiple shop definitions | Array of shop configs | ❌ | 🔴 Missing |

---

## 19. NPC Interactions & Dialogues

| Feature | Java (in `PacketManager.java`, `NPCOption1.java`, etc.) | C# | Status |
|---------|-------------------------------------------------------|-----|--------|
| NPC option 1 (Talk-to) | Massive switch on NPC type → dialogue chains | Handler is stub | 🔴 Missing |
| NPC option 2 (Trade/Bank) | Shop opening, banker dialogue | Handler is stub | 🔴 Missing |
| NPC option 3 | Various NPC interactions | Handler is stub | 🔴 Missing |
| Dialogue system | 55+ dialogue states (`p.Dialogue = 1..55`) with skill cape rewards, quest dialogues, item giving, NPC chatbox animations | ❌ | 🔴 **Missing** |
| Skill cape NPCs (24 NPCs) | Each skill's master NPC with cape/hood reward or training advice | ❌ | 🔴 Missing |
| Banker dialogue | Bank opening, "noob" rejection | ❌ | 🔴 Missing |
| Dialogue choice interfaces (458) | Multi-option dialogue with player choices | ❌ | 🔴 Missing |
| Slayer master task assignment | NPC 1599 (Duradel) task assignment | ❌ | 🔴 Missing |

---

## 20. Commands

| Feature | Java (`Commands.java` — 1089 lines) | C# (`CommandMessageHandler.cs` — 273 lines) | Status |
|---------|-------------------------------------|---------------------------------------------|--------|
| Command parsing | `string.split(" ")` in switch | String parsing with if/else chain | ⚠️ **Partial** |
| Player commands | `::home`, `::yell`, `::players`, `::train`, `::pvp`, `::empty`, `::savebackup`, `::loadbackup`, `::verifycode` and ~20 more | Mentions many in comments/structure, **most are stubs or log-only** | 🔴 Mostly stubs |
| Moderator commands | `::ban`, `::unban`, `::mute`, `::unmute`, `::kick`, `::ipban`, `::jail`, `::unjail`, `::teletome`, `::teleto` | ❌ | 🔴 Missing |
| Admin commands | `::item`, `::master`, `::npc`, `::object`, `::anim`, `::gfx`, `::coords`, `::update`, `::spec`, `::pnpc`, `::setlevel`, `::interface`, `::config`, `::givecape`, `::getkc`, and ~30 more | ❌ | 🔴 Missing |
| Teleport commands | `::gwd`, `::barrows`, `::duel`, `::pits`, `::clanfield`, `::castlewars`, `::cons`, `::party`, `::assault` | ❌ | 🔴 Missing |

---

## 21. Friends & Ignores / Private Messaging

| Feature | Java | C# | Status |
|---------|------|----|--------|
| Add friend | Stores in `friends` list, sends friend status frame | Adds to list, **no status frame sent** | ⚠️ Partial |
| Remove friend | Removes from list | Removes from list | ⚠️ Partial |
| Add ignore | Stores in `ignores` list | Adds to list | ⚠️ Partial |
| Remove ignore | Removes from list | Removes from list | ⚠️ Partial |
| Private message sending | Encrypts text, sends via frame 89 (sent) + 178 (received) | ❌ | 🔴 Missing |
| Online status updates | `sendFriend()` on login/logout | ❌ | 🔴 Missing |
| Ignore list sync on login | `sendIgnores()` | ❌ | 🔴 Missing |
| Friend server connection frame | Frame 115 | ✅ Sent in login sequence | ✅ Done |

---

## 22. Clan Chat

| Feature | Java (`ClanMain.java`, `ClanList.java`, `inChat.java`, `SaveChats.java`) | C# | Status |
|---------|------------------------------------------------------------------------|-----|--------|
| Join clan chat | Channel join by name | Handler is stub (log only) | 🔴 Missing |
| Leave clan chat | Reset list, config toggle | ❌ | 🔴 Missing |
| Send clan message | Frame 229 clan chat message | ❌ | 🔴 Missing |
| Clan name setting | Via input packet 189 | ❌ | 🔴 Missing |
| Kick from clan | Via packet 200 | ❌ | 🔴 Missing |
| Loot share toggle | Config 1083 | ❌ | 🔴 Missing |
| Clan data persistence | `SaveChats` file I/O | ❌ | 🔴 Missing |

---

## 23. Prayer

| Feature | Java (in `Player.java`, `ActionButtons.java`, `Prayer.java`) | C# | Status |
|---------|-------------------------------------------------------------|-----|--------|
| 27 prayer types | Full prayer array, config per prayer | `PrayOn[27]` array exists, no logic | 🔴 Missing |
| Prayer activation/deactivation | Toggle prayers via action buttons + configs | Handler is stub | 🔴 Missing |
| Prayer drain per tick | `prayerDrain` counter in Engine tick | ❌ | 🔴 Missing |
| Protection prayers | `prayerIcon` set per active prayer | ❌ | 🔴 Missing |
| Head icon updates | Skull/prayer icon update mask | ❌ | 🔴 Missing |
| Quick prayers | ❌ (not in legacy) | ❌ | N/A |

---

## 24. Quests

| Feature | Java (Dragon Slayer quest, dialogue states 100-111) | C# | Status |
|---------|---------------------------------------------------|-----|--------|
| Dragon Slayer quest | 5 quest states (0-5), NPC dialogues with Guildmaster/Oziach/Oracle, map item, boat ride, Elvarg kill, quest completion interface (277), quest point + XP rewards | ❌ | 🔴 **Missing** |
| Quest tab (274) | Quest list with teleport options | ❌ | 🔴 Missing |
| Quest points tracking | `p.QuestPoints` | Property exists, no logic | 🔴 Missing |

---

## 25. Minigames

### Barbarian Assault (`Assault.java` / `AssaultMessageHandler.cs`)

| Feature | Java | C# | Status |
|---------|------|----|--------|
| Wave spawning (5 waves) | NPC types per wave, counts, HP/max-hit | Constants defined, **logic is stub** | ⚠️ Constants only |
| Wave progression | Kill all → next wave | ❌ | 🔴 Missing |
| Reward system | ❌ (not fully in Java) | ❌ | N/A |

### Castle Wars

| Feature | Java (`CastleWarsFL.java`, Player area checks, Engine timer) | C# | Status |
|---------|-------------------------------------------------------------|-----|--------|
| Team assignment (Saradomin/Zamorak) | `p.CWTeam` | Property exists, no logic | 🔴 Missing |
| Flag capture mechanics | Flag equip/unequip on login | ❌ | 🔴 Missing |
| Game timer | In Engine tick | ❌ | 🔴 Missing |
| Cape/equipment management | Cape removal on login, flag handling | ❌ | 🔴 Missing |

### Fight Pits

| Feature | Java | C# | Status |
|---------|------|----|--------|
| Game start timer | `PitGame` countdown | Property exists, no logic | 🔴 Missing |
| Area detection | `AtPits()` | ❌ | 🔴 Missing |
| Random spawn on login | ✅ | ❌ | 🔴 Missing |

### Bounty Hunter

| Feature | Java | C# | Status |
|---------|------|----|--------|
| Target assignment | `bountyOpp` | `BountyOpponent` exists, no logic | 🔴 Missing |
| Area restriction | `bountyArea()` check | ❌ | 🔴 Missing |

### Duel Arena

| Feature | Java | C# | Status |
|---------|------|----|--------|
| Duel request/accept | Player option + partner matching | Properties exist, no logic | 🔴 Missing |
| Duel timer | `DuelTimer` in Engine tick | ❌ | 🔴 Missing |
| Area detection | `AtDuel()` | ❌ | 🔴 Missing |

### Clan Wars

| Feature | Java | C# | Status |
|---------|------|----|--------|
| Team opposition | `p.Opposing` | ❌ | 🔴 Missing |
| Clan timer | `ClanTimer` | ❌ | 🔴 Missing |

---

## 26. Construction (Player-Owned Houses)

| Feature | Java (`Construction.java` — extensive) | C# | Status |
|---------|---------------------------------------|-----|--------|
| 22 room types | Full room info array (coords, price, level) | ❌ | 🔴 Missing |
| Room building via doors | Direction detection (N/S/E/W) + room placement | ❌ | 🔴 Missing |
| Construction interface (402) | Room selection | ❌ | 🔴 Missing |
| Build mode toggle | `p.buildMode` | ❌ | 🔴 Missing |
| Custom map data (`sendMapRegion2`) | Dynamic palette-based region construction | ❌ | 🔴 Missing |
| Furniture building | Garden plants (8 types), center piece (5 types), etc. | ❌ | 🔴 Missing |
| Furniture removal | Remove and restore buildspot | ❌ | 🔴 Missing |
| Teleport to POH | `teleToPOH()` with delayed object spawning | ❌ | 🔴 Missing |
| Room data save/load | File-based room state persistence | ❌ | 🔴 Missing |
| Watering can mechanics | Can degradation on plant use | ❌ | 🔴 Missing |

---

## 27. Summoning

| Feature | Java (in Login, Engine, NPC spawn) | C# | Status |
|---------|-----------------------------------|-----|--------|
| Familiar types (6 NPCs: 6901, 7344, 6903, 6905, 6907, 6943) | Spawn on login if active | Properties exist (`FamiliarType`, `FamiliarId`), no logic | 🔴 Missing |
| Summoning tab interface (663) | Display familiar head | ❌ | 🔴 Missing |
| Familiar combat (Steel Titan joins fight) | Referenced but commented out | ❌ | 🔴 Missing |
| Summoning pouch items | Via shop/NPC | ❌ | 🔴 Missing |

---

## 28. Slayer

| Feature | Java | C# | Status |
|---------|------|----|--------|
| 5 task types | Task 0-4 mapping to NPC types | Properties exist (`SlayerTask`, `SlayerAmount`), no logic | 🔴 Missing |
| Task assignment (NPC 1599 Duradel) | Random task + amount | ❌ | 🔴 Missing |
| Kill tracking | Decrements `SlayerAm` on matching NPC death | ❌ | 🔴 Missing |
| Slayer XP formula | `150-500 * skillLvl[18]` | ❌ | 🔴 Missing |

---

## 29. Object Interactions

| Feature | Java (`ObjectOption1.java`, `ObjectOption2.java`, `objectLoader.java`) | C# | Status |
|---------|---------------------------------------------------------------------|-----|--------|
| Object option 1 | Doors, ladders, stairs, banks, altars, agility obstacles, cooking ranges, spinning wheels, thieving stalls, obelisks, levers, portals, mining rocks, trees, fishing spots, farming patches | Handler is stub | 🔴 Missing |
| Object option 2 | Bank (secondary option), furnace, anvil | Handler is stub | 🔴 Missing |
| Object spawning on region load | `p.objects()` — spawns custom objects per region | ❌ | 🔴 Missing |
| Construction object interactions (190) | Build/remove spots with multiple object IDs | ❌ | 🔴 Missing |

---

## 30. Map Data & Region Loading

| Feature | Java (`MapData.java`, `MapList.java`) | C# | Status |
|---------|--------------------------------------|-----|--------|
| XTEA map key loading | Loads 4-int key arrays per region from file | ❌ (sends zeros in login) | 🔴 **Missing** |
| Map region change detection | Sets `rebuildNPCList`, sends new keys | ❌ | 🔴 Missing |
| Region-specific object spawning | Custom objects per region ID | ❌ | 🔴 Missing |

---

## 31. Player Save/Load (Persistence)

| Feature | Java (`PlayerSave.java`, `FileManager.java`) | C# | Status |
|---------|---------------------------------------------|-----|--------|
| Character file loading | Text file parsing for all player data | DB-based via EF Core models, **but no actual loading code** | 🔴 **Missing** |
| Character file saving | Text file serialization | DB models exist, **no save code** | 🔴 **Missing** |
| Backup save/load | `savebackup` / `loadbackup` commands | ❌ | 🔴 Missing |
| Auto-save timer | Every 17 ticks in Engine | ❌ | 🔴 Missing |
| IP logging | Login/failure IP tracking | ❌ | 🔴 Missing |
| Ban file checks | IP ban + character ban file existence | ❌ | 🔴 Missing |
| Room data save/load (construction) | Separate room state files | ❌ | 🔴 Missing |

### Database Models (C# only — no Java equivalent)

The C# side has these EF Core models with no Java counterpart:
- `DbPlayer`, `DbSkill`, `DbItem`, `DbBankItem`, `DbEquipment`, `DbFriend`, `DbGrandExchangeOffer`
- `AeroScapeDbContext` with `SaveChangesAsync`

**These are forward-looking but currently unused** — no code reads from or writes to them.

---

## 32. File & Data Management

| Feature | Java (`FileManager.java`) | C# | Status |
|---------|--------------------------|-----|--------|
| Item definition loading | Item names, descriptions, bonuses, stackable flags from data files | ❌ | 🔴 Missing |
| NPC definition loading | NPC names, descriptions from data files | ❌ | 🔴 Missing |
| NPC spawn list loading | `LoadNPCLists.java` — spawns from text config | ❌ | 🔴 Missing |
| Shop data loading | From config files | ❌ | 🔴 Missing |
| Map data (XTEA keys) loading | From data files | ❌ | 🔴 Missing |

---

## 33. Connection & Session Management

| Feature | Java (`SocketListener.java`, `ConnectionManager.java`, `Monitor.java`, `Protect.java`) | C# | Status |
|---------|--------------------------------------------------------------------------------------|-----|--------|
| TCP listener | Java NIO-based | `TcpBackgroundService` | ✅ Done |
| Login handshake (3-stage) | Connection type check, ISAAC key exchange, credential validation | `LoginHandler.cs` | ⚠️ **Partial** (needs verification) |
| IP-based connection limiting | `Protect.checkPlayer()` — max 2 per IP | ❌ | 🔴 Missing |
| Connection throttling | `ConnectionManager` limits | ❌ | 🔴 Missing |
| Update server (cache serving) | Client version 508 cache key response | ❌ | 🔴 Missing |
| Player session array management | Fixed 2000-slot array | `PlayerSessionManager` | ⚠️ Exists, completeness unknown |
| HD/LD client detection | `p.usingHD` from login byte | ❌ (always defaults) | 🔴 Missing |

---

## 34. Miscellaneous Missing Systems

| System | Java | C# | Status |
|--------|------|----|--------|
| Skill cape auto-trimming | 24 cape IDs checked and replaced on login/packet 117 | ❌ | 🔴 Missing |
| Death mechanics | Death animation, item drop, respawn at home | ❌ | 🔴 Missing |
| Poison system | Poison damage ticks | ❌ | 🔴 Missing |
| Skull system | PK skull timer + icon | ❌ | 🔴 Missing |
| Wilderness level overlay | Wilderness level display via overlay | ❌ | 🔴 Missing |
| Destroy item confirmation | Interface 94 for untradeable items | ❌ | 🔴 Missing |
| Welcome screen | Login update notes (interface 132) | ❌ | 🔴 Missing |
| Emotes | Via action buttons → animations | ❌ | 🔴 Missing |
| Food eating | Eat delay timer, HP restoration values per food | ❌ | 🔴 Missing |
| Potion drinking | Stat boost values, dose decrement | ❌ | 🔴 Missing |
| Bone burying | Prayer XP per bone type, bury delay | ❌ | 🔴 Missing |
| Teleport sequences | 4-tick anim/gfx sequences with height changes | ❌ | 🔴 Missing |
| Idle disconnection | 5× idle packet → disconnect | ✅ `IdleMessageHandler` | ✅ Done |
| Chat text encryption/decryption | `Misc.encryptPlayerChat/decryptPlayerChat` | ❌ | 🔴 Missing |
| Boat ride (Dragon Slayer) | Interface 544 with timer | ❌ | 🔴 Missing |
| Party room | Referenced in commands | ❌ | 🔴 Missing |
| Verification code system | `donecode`, `verificationCode` fields | ❌ | 🔴 Missing |
| Agility courses | Object-based obstacle sequences | ❌ | 🔴 Missing |
| Item on item interactions | Fletching (knife+log), firemaking (tinderbox+log), herblore, etc. | Handler is stub | 🔴 Missing |
| Smelting (furnace) | Ore → bar conversion with level/material checks | ❌ | 🔴 Missing |
| Spinning wheel | Flax → bowstring, wool → ball of wool | ❌ | 🔴 Missing |
| Cannon placement | Dwarf multicannon via item-on-object | ❌ | 🔴 Missing |

---

## Priority Matrix

### Tier 1 — Game Won't Function Without These (CRITICAL)
1. **Player Update Protocol** — without this, clients see nothing
2. **NPC Update Protocol** — no NPCs visible
3. **Game Engine tick processing** — no game state advances
4. **Movement / Walk queue** — players can't move
5. **Map data (XTEA keys)** — client can't load terrain
6. **Outgoing frames** (beyond login) — can't send any game updates

### Tier 2 — Core Gameplay Loop
7. **Player items management** (add/delete/count)
8. **Equipment system** (equip/unequip/bonuses/weapon swap)
9. **Combat system** (melee formulas, hit application)
10. **NPC spawning & definitions**
11. **Player save/load** (DB integration)
12. **Ground items**
13. **Object interactions** (banks, doors, ladders)

### Tier 3 — Essential Features
14. **Banking**
15. **Trading**
16. **Shops**
17. **Skills** (Woodcutting, Mining, Fishing, Smithing, Cooking)
18. **Magic system** (spells, teleports, alchemy)
19. **Prayer system**
20. **Ranged combat**
21. **NPC dialogues**
22. **Commands** (at least player + admin essentials)
23. **Food/Potion consumption**
24. **Death mechanics**

### Tier 4 — Content Features
25. **Quests** (Dragon Slayer)
26. **Slayer**
27. **Minigames** (Castle Wars, Fight Pits, Duel Arena, Barbarian Assault, Bounty Hunter)
28. **Construction / POH**
29. **Summoning**
30. **Clan Chat**
31. **Private messaging**
32. **Skill capes & trimming**

---

## Summary Statistics

| Category | Total Systems | Fully Implemented | Partial/Stub | Missing |
|----------|--------------|-------------------|--------------|---------|
| Architecture | 6 | 6 | 0 | 0 |
| Protocol (incoming) | 55 opcodes | 42 | 0 | 13 |
| Protocol (outgoing) | ~25 frame types | 10 (login only) | 1 | 14 |
| Packet handlers | 42 | 3 (Idle, AddFriend, AddIgnore) | 39 | 0 |
| Game systems | 30+ | 0 | 0 | 30+ |
| **Overall functional parity** | | **~15-20%** | | |

The C# codebase has an excellent architectural foundation and complete protocol layer, but needs the entire game logic layer built on top of it.
