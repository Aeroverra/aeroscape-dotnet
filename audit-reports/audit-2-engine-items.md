# Audit Report 2: Engine, Movement, Items

## Critical Bugs

- `AeroScape.Server.Core/Engine/GameEngine.cs` does not preserve the legacy engine loop structure from `legacy-java/server508/src/main/java/DavidScape/Engine.java`. The Java engine runs a 100ms loop, calls `connectAwaitingPlayers()` and `packets.parseIncomingPackets()` every cycle, and only gates entity work behind a 600ms check (`Engine.java:269-319`, `488-507`). The C# port runs only a 600ms loop (`GameEngine.cs:259-287`) and has no equivalent connection/login pipeline or packet parse pass. That is a direct behavioral regression, not a refactor.

- `GameEngine.ProcessPlayerTick` only ports a small slice of `Player.process()`. The legacy tick handles save execution, map rereads, delayed pickup/object/player/NPC actions, fight pits, castle wars overlays, familiar UI, grave/boat/dragon timers, and many other counters (`Player.java:2866-4393`). The C# method only covers a subset (`GameEngine.cs:433-607`). The port is materially incomplete for the requested engine scope.

- Save timer behavior is broken. Java decrements `saveTimer`, then actually saves the character, refreshes objects, resets the timer to `10`, and reapplies the verification overlay when it expires (`Player.java:2881-2903`). C# only decrements `SaveTimer` and never performs the save/reset work (`GameEngine.cs:435-437`). In practice, auto-save from the game tick is missing.

- `Player.BankSize` is `500` in C# (`AeroScape.Server.Core/Entities/Player.cs:112-115`), while legacy `PlayerBank.SIZE` is `1000` (`PlayerBank.java:8`). That halves bank capacity and makes the port incompatible with legacy saves and bank logic.

- `GroundItemManager.Process()` drops the private-to-global transition entirely. Legacy `Items.process()` converts a privately owned tradable ground item into a global item when `itemGroundTime == 60`, then removes it at expiry (`Items.java:55-79`). C# only decrements `ItemGroundTime` and deletes the record at `<= 0` (`GroundItemManager.cs:13-34`). Private drops never become public.

- NPC death processing is incomplete. Legacy `Engine.run()` calls `n.npcDied(...)` before hiding the NPC, which is where loot/death-side effects are triggered (`Engine.java:408-418`). C# `DeathService.ProcessNpcDeath()` never calls an equivalent death/drop routine; it only flips `NpcCanLoot`, `HiddenNPC`, and respawn delay (`DeathService.cs:50-72`).

- `PlayerEquipmentService` is not a full port of legacy equipment behavior. The actual Java equip logic lives in `io/packets/Equipment.java`, not `PlayerWeapon.java`, and includes attack/defence/strength/magic/ranged/crafting requirements, quest locks, member/staff locks, gnomecopter restrictions, and appearance/body-mask handling (`Equipment.java:68-305`, `313-1453`). The C# service only moves items between inventory/equipment and recalculates emotes/bonuses (`PlayerEquipmentService.cs:35-122`). Large parts of equipment gameplay are missing.

- Equipment slot mapping is incomplete versus legacy. The C# keyword tables omit many Java entries from `Equipment.itemType()`: examples include `Feather headdress`, `tiara`/`Tiara`, `cav`, `boater`, `kiteshield (t)/(g)/(h)`, `Saradomin kite`, `Zamorak d'hide`, `Guthix d'hide`, `Saradomin d'hide`, `Magic butterfly net`, `Gnomecopter`, `Flowers`, `Invisibility`, `Swords`, `Longsword`, `dagger(p)/(+)/(s)`, `spear(p)/(+)/(s)/(kp)`, `javelin(p)`, `knife(p)`, and `Claws` (`Equipment.java:10-43`). Missing entries mean some legacy items will be rejected or assigned the wrong slot in C# (`PlayerEquipmentService.cs:7-33`, `124-139`).

- Trading only ports a minimal subset of the legacy system. The Java codebase has both `PlayerTrade.java` and the much richer `PTrade` / `TButtons` / `TItem` flow with full first-screen/second-screen refresh, offer/remove opcodes, acceptance text, inventory access masks, flashing removed slots, and staged confirmation (`PlayerTrade.java:8-264`, `PTrade.java:33-648`, `TButtons.java:23-87`). `TradingService.cs` supports only reciprocal open, basic offer/remove, and accept/decline (`TradingService.cs:8-321`). The full trading UX and much of the screen/state logic are missing.

## Logic Errors

- `WalkQueue.HandleWalk()` returns immediately when frozen (`WalkQueue.cs:17-20`). Legacy `Walking.handlePacket()` still resets the walking queue, cancels pending interactions (`itemPickup`, player/NPC/object options, attacks), restores interfaces/tabs, and resets `faceToReq`, while also warning the player (`Walking.java:17-64`). The C# behavior leaves stale targeting/UI state active whenever a frozen player clicks to move.

- `WalkQueue.HandleWalk()` never mirrors the legacy interface cleanup path. Java explicitly removes the shown interface twice, restores tabs/inventory, and removes the chatbox interface on every walking packet (`Walking.java:18`, `58-61`). There is no equivalent in the C# movement path (`WalkQueue.cs:15-50`).

- `GameEngine.ProcessMajorTick()` omits the legacy `restoreTabs` guard that runs before `p.process()` when the player is not in interfaces `762`, `335`, `334`, or `620` (`Engine.java:367-372`). No C# equivalent exists before `ProcessPlayerTick()` (`GameEngine.cs:307-324`).

- `ProcessGlobalTimers()` ports only Fight Pits and Castle Wars countdowns. It omits the drop-party timer `pdT` countdown/announcement/spawn logic (`Engine.java:273-315`) and the Zamorak/Saradomin flag carrier validity checks (`Engine.java:342-357`).

- `ProcessPlayerTick()` does not mirror the legacy jail behavior. Java decrements `jailTimer`, force-chats “I have been jailed for breaking the rules.” when it reaches `0`, then resets it to `20` (`Player.java:2867-2871`). C# only decrements `JailTimer` (`GameEngine.cs:438-439`).

- `ProcessPlayerTick()` does not reset `reqX` / `reqY`, does not rerun `readMaps()` every `MyCount2` interval, and does not handle the `dropCake()` random event (`Player.java:2872-2880`, `2926-2931`). Those tick-driven behaviors disappeared.

- `ProcessPlayerTick()` omits the delayed rune deletion for `taken > 0 && !inAssault` (`Player.java:2904-2914`).

- The C# claw timer logic does not match Java. Legacy has both `clawTimer2` / `UseClaws2` and a real `dClaw3(this)` callback when the timer expires (`Player.java:2916-2920`). C# uses `ClawTimer` / `UseClaws`, decrements it, then only comments that target resolution “would need to be resolved” (`GameEngine.cs:549-559`). The extra hits are not actually applied.

- Freeze handling is incomplete. Java decrements `freezeDelay` and actively calls `stopMovement(this)` each tick while frozen (`Player.java:4243-4245`). C# only decrements `FreezeDelay` (`GameEngine.cs:451-453`); it does not stop queued movement.

- Click delay behavior differs. Java resets `clickDelay` to `-1` when it reaches `0` (`Player.java:4389-4391`). C# decrements `ClickDelay` but never normalizes it back to `-1` (`GameEngine.cs:444`), so any code depending on the legacy sentinel value will diverge.

- Disconnect handling is incomplete. Java declines active trades before completing the disconnect path (`Player.java:4236-4241`). C# only forwards `Disconnected[0]` into `Disconnected[1]` (`GameEngine.cs:585-587`), so active trade state is left hanging.

- After-death restoration sequencing changed. Legacy uses `afterDeathUpdateReq` in `Player.process()` to restore skills, prayers, skull/freeze/special/run state, and reset `deathDelay` after the death flow (`Player.java:4337-4355`). C# bypasses that state machine and restores immediately inside `DeathService.ProcessPlayerDeath()` as soon as `DeathDelay` reaches `0` (`DeathService.cs:17-48`). That is not identical timing/flow.

- `ProcessPlayerTick()` does not port the legacy `itemPickup`, `playerOption1/2/3`, `npcOption1/2/3`, `objectOption1/2` deferred execution block (`Player.java:4270-4296`). The walking/combat tick no longer completes those queued interactions.

- `ProcessPlayerTick()` does not port the legacy `skulledUpdateReq` appearance logic that toggles `pkIcon` while skulled and clears it when the timer ends (`Player.java:4317-4328`). C# decrements `SkulledDelay` and sets `SkulledUpdateReq`, but never applies the icon transition (`GameEngine.cs:455-459`).

- `ProcessPlayerTick()` does not port the legacy `runEnergyUpdateReq` / `specialAmountUpdateReq` client updates (`Player.java:4329-4335`). If the networking layer does not compensate elsewhere, these state changes will be invisible to the client.

- Prayer drain is only partially ported. The numeric drain and reset exist (`GameEngine.cs:518-528`), but Java also sends a skill update every tick and emits “You have run out of prayer points.” when prayer reaches zero (`Player.java:4370-4380`). That user-facing behavior is absent in the engine path.

- `GameEngine.ProcessMajorTick()` no longer writes the per-player outbound buffer after clearing update masks. Java explicitly calls `Server.socketListener.writeBuffer(p)` (`Engine.java:384-391`). The C# tick stops at `GameUpdateService?.SendPlayerAndNpcUpdates(p)` and mask clearing (`GameEngine.cs:326-347`).

- NPC per-tick behavior is incomplete. Legacy `NPC.process()` follows `followPlayer` targets and kills the NPC if the followed player disappears (`NPC.java:362-369`). C# `NPC.Process()` only decrements respawn/combat delays (`NPC.cs:220-227`).

- Legacy NPC random walking is explicit in the engine loop when `n.randomWalk && !n.attackingPlayer` (`Engine.java:403-406`). The C# loop has no equivalent random-walk branch; it just calls `GameUpdateService?.ProcessNpcMovement(n)` (`GameEngine.cs:370-377`).

- `PlayerItemsService.AddItem()` diverges from legacy note handling. Java treats both stackable items and noted items as stackable (`PlayerItems.java:121-171`). C# relies solely on `IsStackable()` (`PlayerItemsService.cs:39-72`), which is fine only if note flags are loaded correctly; however the service dropped the explicit `noted()` / `notedAndStackable()` parity methods from `Items.java` and `PlayerItems.java`, so the behavior now depends entirely on loader correctness.

- `PlayerItemsService` is missing legacy helper methods `noted()`, `getaxesid()`, and `barrowschest()` (`PlayerItems.java:81-100`). Those are used as gameplay helpers in the Java codebase and were not ported.

- `PlayerBankService.Deposit()` does not mirror Java’s overflow handling. Legacy clamps `amt` when `bankItemCount + amt < 0` and sends “Your bank is full” (`PlayerBank.java:39-42`). C# has no equivalent overflow guard (`PlayerBankService.cs:35-63`).

- `PlayerBankService.Withdraw()` does not mirror Java’s inventory overflow guard. Legacy clamps `amt` when `invItemCount + amt < 0` and sends “You can't carry more of that item!” (`PlayerBank.java:73-78`). C# removed that check (`PlayerBankService.cs:73-109`).

- `PlayerBankService.Deposit()` and `Withdraw()` dropped all legacy bank UI refresh/config work: `setItems` on `64207`/`64209`/`149`, free-slot string `762:97`, and `sendTabConfig()` (`PlayerBank.java:52-57`, `102-112`, `277-315`). The state changes are server-side only in C#.

- `PlayerBankService` omitted `SendTabConfig()` and `SetBankX()` entirely (`PlayerBank.java:290-315`). Those are core parts of legacy bank tab/X-mode behavior.

- `GroundItemManager.GetPickupCandidate()` changes owner gating. The C# guard only blocks pickup when the item is tradable and still private (`GroundItemManager.cs:101-106`), which means a spoofed pickup request can target someone else’s private untradable drop. Legacy private drops never become global for untradables because they are only ever shown to the owner and are removed owner-side (`Items.java:61-79`, `130-160`, `197-223`).

- `GroundItemManager` has no equivalent to `removeGlobalItem()`, `createGlobalItem()`, or `itemPickedup()` (`Items.java:100-125`, `197-223`). It cannot reproduce the legacy visibility/removal semantics.

- `TradingService.HandleActionButton()` only supports remove-1 from the offered-items container (`buttonId == 30 && packetOpcode == 233`) (`TradingService.cs:158-170`). Legacy `PTrade.tradeOptions()` exposes Remove, Remove-5, Remove-10, Remove-All, and Remove-X via scripts and button handling (`PTrade.java:632-641`, `TButtons.java:686-700`).

- `TradingService.HandleActionButton()` supports offer `1/5/10/all` but drops legacy `Offer-X` (`packet 173`) entirely (`TradingService.cs:172-185`, `TButtons.java:717-734`).

- `TradingService.OpenFirstScreen()` does not populate any of the legacy first-screen strings, partner inventory information, access masks, or staged acceptance text (`PlayerTrade.java:71-95`, `PTrade.java:589-609`). It only sets `TradeStage`, accept flags, and `InterfaceId` (`TradingService.cs:189-201`).

- `TradingService` never opens or populates the legacy second confirmation screen contents (`PlayerTrade.java:50-68`, `PTrade.java:611-630`). Advancing to stage `2` only flips integers (`TradingService.cs:38-46`).

## Missing Implementations

- `GameEngine.cs` has no equivalents for `Engine.saveHouses()`, `Engine.addConnection()`, `Engine.connectAwaitingPlayers()`, `Engine.testPartyDrop()`, `Engine.getNPCDescription()`, or `Engine.newSummonNPC()` (`Engine.java:234-248`, `449-507`, `462-479`, `599-607`, `686-733`).

- The per-player minigame/event sections from `Player.process()` are absent: castle wars reward/scoreboard/team-room logic (`Player.java:3033-3155`), fight pits waiting/game-start/winner logic (`3157-3253`), clan field gate timer objects (`3270+`), and related overlay management.

- `PlayerEquipmentService` does not port the legacy `checkSpecials()` interface-config logic from `PlayerWeapon.java:283-315`. Weapon special bars are never enabled by the equipment service.

- `PlayerEquipmentService` also does not port legacy `isFullbody()`, `isFullhat()`, and `isFullmask()` behavior from `Equipment.java:1403-1452`. If the appearance encoder depends on those flags, equipped models will render incorrectly.

- `ItemDefinitionLoader` is missing `Items.notedAndStackable()` and `Items.getIdFromName()` parity (`Items.java:272-281`, `322-332`).

- `PlayerBankService` added `HandleBankSwitch()` and `MoveToBankTab()` but still omits the legacy config-sync methods required for the client to see tab state changes.

- `TradingService` does not port legacy `tradeOptions()`, `flashIcon()`, `getSecondString()`, `showFirst()`, `showSecond()`, or `refreshScreens()` from `PTrade.java`.

- `GroundItemManager` has no notion of broadcasting spawn/removal packets to the owner versus all nearby players, which was a core responsibility of `Items.java`.

## Minor Issues

- `PlayerItems.HasItemAmount()` is intentionally “fixed” in C# (`InvItemCount >= amount` in `PlayerItemsService.cs:7`) instead of copying the legacy bug in `PlayerItems.java:45-56` where it mistakenly assigns `playerItemAmountCount = p.items[i]`. That is an improvement, but it is not logic-identical.

- `GameEngine.GetPlayerCount()` starts from slot `1`, while Java loops all slots but index `0` is unused anyway (`GameEngine.cs:153-162`, `Engine.java:612-621`). Behavior is effectively equivalent.

- `PlayerBankService.Insert()` adds argument guards that legacy Java lacked (`PlayerBankService.cs:193-221`, `PlayerBank.java:200-217`). That is safer, but it means edge-case behavior no longer matches exactly.

- `ItemDefinitionLoader.FindNote()` / `FindUnnote()` use exact ordinal name comparison (`ItemDefinitionLoader.cs:78-90`). Legacy `BankUtils` also used exact `String.equals`, so this is compatible, but it means any loader-side normalization difference will change note matching globally.

- `WalkQueue.Process()` drains run energy during movement processing (`WalkQueue.cs:107-119`) instead of during movement packet encoding as legacy `Frames.updateMovement()` did (`Frames.java:1044-1054`). Net effect is similar, but the timing is not identical.
