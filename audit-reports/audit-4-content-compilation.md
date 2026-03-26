# Audit Report 4: Content Systems & Compilation

Static audit against the legacy Java sources in `legacy-java/server508`. I could not run `dotnet build` in this environment because the `dotnet` CLI is not installed, so compilation findings below are based on source inspection, DI wiring, and handler/decoder reachability.

## Critical Bugs

- Shop interactions are effectively incomplete at runtime. `ShopService` implements `OpenShop`, `Buy`, and `Sell`, but the only observed call sites are `OpenShop(...)` from NPC handlers; there is no shop button handling in [AeroScape.Server.Core/Handlers/ActionButtonsMessageHandler.cs](../AeroScape.Server.Core/Handlers/ActionButtonsMessageHandler.cs) lines 35-106, and no other references to `ShopService.Buy` or `ShopService.Sell`. Result: shops can open, but buying and selling do not appear wired to any incoming packet path. This is a hard regression from `ShopHandler.java`, which contains the full buy/sell/open/restock flow.

- The general store / party shop logic from legacy `ShopHandler.java` is missing. `ShopService` only defines shops `2,3,4,5,6,7,8,9,10,11,12,13,14,16,18` in [AeroScape.Server.Core/Services/ShopService.cs](../AeroScape.Server.Core/Services/ShopService.cs) lines 17-33. Legacy logic relies on shop `1` (general store) and `17` (party shop) for dynamic stock, selling arbitrary items, slot clearing, and restocking. C# still sets `player.PartyShop = shopId == 17` on line 71, but shop `17` does not exist in the definitions, so that branch is dead and the feature is unported.

- `::changepass` is broken against the EF persistence model. The command only assigns `player.Password = args[0]` in [AeroScape.Server.Core/Services/CommandService.cs](../AeroScape.Server.Core/Services/CommandService.cs) lines 85-88. Login validates against `DbPlayer.PasswordHash` in [AeroScape.Server.App/Services/PlayerLoginService.cs](../AeroScape.Server.App/Services/PlayerLoginService.cs) lines 46-49, and `PlayerPersistenceService` never updates `PasswordHash` in [AeroScape.Server.App/Services/PlayerPersistenceService.cs](../AeroScape.Server.App/Services/PlayerPersistenceService.cs) lines 77-183. The command therefore changes only transient runtime state and will not survive relog.

- Clan chat is not functionally ported. The legacy system includes persistent chat ownership, join/talk/kick requirements, member lists, lootshare state, ranking, join/leave flows, and periodic save/load (`ClanMain.java`, `ClanList.java`, `SaveChats.java`). The C# `ClanChatService` in [AeroScape.Server.Core/Services/ClanChatService.cs](../AeroScape.Server.Core/Services/ClanChatService.cs) lines 11-132 only keeps in-memory channels, has no persistence, no channel list packet building, no kick flow, no friend-rank visibility semantics, and no broadcast path beyond setting `LastMessage`.

- Clan chat is also unreachable from the packet router. `Program.cs` registers `IMessageHandler<ClanChatMessage>` on line 123 in [AeroScape.Server.App/Program.cs](../AeroScape.Server.App/Program.cs), but [AeroScape.Server.Network/Protocol/PacketRouter.cs](../AeroScape.Server.Network/Protocol/PacketRouter.cs) lines 46-85 registers no clan-chat decoder or opcode. Legacy `PacketManager.java` handled clan join/name/rank/kick packets directly; the C# server has no equivalent ingress path.

- Construction state is non-persistent and mostly unreachable. `ConstructionService` only stores room/furniture state in an in-memory `ConcurrentDictionary<int, HouseState>` in [AeroScape.Server.Core/Services/ConstructionService.cs](../AeroScape.Server.Core/Services/ConstructionService.cs) lines 36 and 160-164. There is no save/load path for rooms or furniture in `PlayerPersistenceService`/`PlayerLoginService`, and no call sites to `AddRoom`, `AddFurniture`, or `RemoveFurniture`. Legacy `Construction.java` handled POH map generation, build spot UIs, room edits, local object spawning, and room-file persistence.

- Object loading is effectively missing from runtime. [AeroScape.Server.Core/Services/ObjectLoaderService.cs](../AeroScape.Server.Core/Services/ObjectLoaderService.cs) lines 9-36 parses the legacy file format, but it is not registered in DI and static reference scans found no usages. This leaves the `objectLoader.java` parity incomplete and makes `::loadobjects` impossible even before considering that `::loadobjects` is stubbed.

## Logic Errors

- Several NPC interaction mappings are wrong relative to legacy behavior. In [AeroScape.Server.Core/Handlers/NPCOption1MessageHandler.cs](../AeroScape.Server.Core/Handlers/NPCOption1MessageHandler.cs) lines 41-60, NPCs `494`, `495`, `2619`, and `2270` all open the bank interface, but in legacy `NPCOption1.java` bankers only showed dialogue on option 1, and `2270` is Martin Thwait’s thieving-tutor dialogue, not bank access. This changes user-facing behavior and bypasses the intended tutorial/skillcape flow.

- Additional NPC shop mappings are incorrect. `NPCOption1MessageHandler` and `NPCOption2MessageHandler` map `549 -> 13`, `548 -> 14`, and `521 -> 5` in [NPCOption1MessageHandler.cs](../AeroScape.Server.Core/Handlers/NPCOption1MessageHandler.cs) lines 47-60 and [NPCOption2MessageHandler.cs](../AeroScape.Server.Core/Handlers/NPCOption2MessageHandler.cs) lines 41-55. Legacy behavior differs: `549`/`548` option 1 are dialogues, `549` option 2 opens armor shop 5, `548` option 2 opens clothing shop 6, and `521` option 2 opens shop 2.

- Aubury’s third option teleports to the wrong location. [AeroScape.Server.Core/Handlers/NPCOption3MessageHandler.cs](../AeroScape.Server.Core/Handlers/NPCOption3MessageHandler.cs) lines 33-35 send players to `3253,3401,0`; legacy `NPCOption3.java` teleports Aubury’s rune teleport to `3504,3575,0`. This is a concrete coordinate regression.

- Dialogue flows were reduced to state changes/items and no longer drive the UI. [AeroScape.Server.Core/Services/DialogueService.cs](../AeroScape.Server.Core/Services/DialogueService.cs) lines 16-95 mostly set `player.Dialogue`, reward items, or quest state. Legacy `PacketManager.java` emitted chatbox interfaces, NPC heads, reward text, and choice interfaces for each branch. In the port, many NPC handlers only set `player.Dialogue` and never open the initial dialogue UI, so the player will not see the conversation start even when the state changes.

- Wise Old Man quest completion flow is incomplete at entry. `DialogueService` still has dialogue `111` reward logic on lines 88-91, but [NPCOption1MessageHandler.cs](../AeroScape.Server.Core/Handlers/NPCOption1MessageHandler.cs) has no case for NPC `2253` at all. Legacy `NPCOption1.java` explicitly handled the Wise Old Man and gated the reward on quest points. The completion reward is therefore orphaned.

- Object interactions lost the legacy coordinate and distance validation. [AeroScape.Server.Core/Services/ObjectInteractionService.cs](../AeroScape.Server.Core/Services/ObjectInteractionService.cs) lines 18-90 dispatch solely by `objectId`, whereas legacy `ObjectOption1.java` contained extensive per-coordinate checks to prevent spoofed interactions and to route different objects with the same ID differently. This is both a logic regression and a trust boundary regression.

- Stair handling in `ObjectInteractionService` is oversimplified. The shared `1738/1740` branch in [ObjectInteractionService.cs](../AeroScape.Server.Core/Services/ObjectInteractionService.cs) lines 63-66 toggles height in place, ignoring object coordinates and directional movement. Legacy object handlers moved players to specific target tiles per stair/object pair.

- `::male` and `::female` no longer restore the full look presets from Java. [CommandService.cs](../AeroScape.Server.Core/Services/CommandService.cs) lines 69-78 only flip `Gender` and update flags. Legacy `Commands.java` rewrote the entire `look[]` array for both presets. The port leaves the player model in a potentially invalid mixed-appearance state.

- Many commands return success without doing anything. Examples in [CommandService.cs](../AeroScape.Server.Core/Services/CommandService.cs) include `players`, `commands/help`, `yell`, `coords`, `loadobjects`, `rebuildnpclist`, `savebackup`, `loadbackup`, `newname`, `givemember`, `removemember`, `setskill`, `walk`, `object`, and more on lines 89-122 and 207-241. In legacy `Commands.java`, many of these had real gameplay/admin behavior.

- Castle Wars flag counters no longer update the player UI. [AeroScape.Server.Core/Services/CastleWarsService.cs](../AeroScape.Server.Core/Services/CastleWarsService.cs) lines 7-11 only increment `ZamFL`/`SaraFL`, while legacy `CastleWarsFL.java` also wrote the updated counters to the interface immediately.

## Missing Implementations

- `ShopHandler.java` parity is far from complete. Missing legacy behaviors include general store stock arrays, arbitrary-item selling to general/party shops, slot clearing, per-shop runtime `getAmount` synchronization, and the dynamic stock decay logic for general store inventory.

- NPC dialogue coverage is minimal compared with legacy `NPCOption1/2/3.java` plus `PacketManager.java`. The C# handlers only cover a handful of NPC IDs in [NPCOption1MessageHandler.cs](../AeroScape.Server.Core/Handlers/NPCOption1MessageHandler.cs), [NPCOption2MessageHandler.cs](../AeroScape.Server.Core/Handlers/NPCOption2MessageHandler.cs), and [NPCOption3MessageHandler.cs](../AeroScape.Server.Core/Handlers/NPCOption3MessageHandler.cs). Large portions of tutor, slayer, appearance, reward, familiar, and event NPC behavior are absent.

- Object interaction parity is also very limited. `ObjectInteractionService` covers a small fixed subset of banks, altars, spellbook switches, stairs, bounty entrance, POH portal UI, and wilderness ditch setup. Legacy `ObjectOption1.java` contains many more world objects, area-specific behaviors, and safeguards.

- Construction UI/map generation is missing. Legacy `Construction.java` includes build spot interface population, custom map palette generation, room object spawning, and POH teleport sequencing. None of that exists in the C# service layer.

- Clan chat persistence/loading is missing. There is no equivalent to legacy `ClanMain.loadChats()` on startup or `SaveChats` background persistence. All C# clan state is transient.

- Save/load coverage is incomplete for current runtime state. The EF model persists many core fields, but not ignores, jail state, house lock/height, Castle Wars counters, construction room/furniture state, clan ranks/requirements, or clan membership lists. Those systems either regress on relog or are not represented at all.

- `ObjectLoaderService` and `NpcSpawnLoader` are asymmetric. NPC definitions/spawns are actually applied by `GameEngine` via [AeroScape.Server.Core/Engine/GameEngine.cs](../AeroScape.Server.Core/Engine/GameEngine.cs) lines 410-425, but object loading has no equivalent application path.

## Compilation Issues (DI registration, type errors, duplicates)

- Several handlers are registered in DI but cannot ever run because `PacketRouter` does not register corresponding decoders/opcodes. Specifically, [Program.cs](../AeroScape.Server.App/Program.cs) lines 121-125 register handlers for `AssaultMessage`, `BountyHunterMessage`, `ClanChatMessage`, `PrayerMessage`, and `ItemOption2Message`, but [PacketRouter.cs](../AeroScape.Server.Network/Protocol/PacketRouter.cs) lines 46-85 registers none of those message types. `ItemOption2Decoder` does exist in [PacketDecoders.cs](../AeroScape.Server.Network/Protocol/PacketDecoders.cs) lines 543-556, which makes its omission from `PacketRouter` a concrete routing defect rather than a missing decoder implementation.

- `ObjectLoaderService` is neither registered in DI nor referenced by any runtime path. This is not a C# type error, but it is a solution-level integration defect for the scoped audit item "Object Interactions / objectLoader.java parity".

- Full compilation could not be executed in this environment because `dotnet` is unavailable, so unresolved compile-time symbol errors could not be mechanically ruled out. I did not find obvious namespace/type mismatches in the audited files themselves, and the registered services in [Program.cs](../AeroScape.Server.App/Program.cs) satisfy the constructors of the audited handlers/services that are actually wired.

- No duplicate hand-written class definitions were found in the source tree during static scans. The only duplicate basename surfaced was generated `.NETCoreApp,Version=v10.0.AssemblyAttributes.cs` under `obj`, which is expected and not a real source collision.

## Minor Issues

- `CommandService` keeps injected `_shops` and `_sessions` fields that are unused in [AeroScape.Server.Core/Services/CommandService.cs](../AeroScape.Server.Core/Services/CommandService.cs) lines 10-20. This is minor, but it suggests unfinished command porting.

- `NpcSpawnLoader` parses the legacy `Examine` field in [AeroScape.Server.Core/Services/NpcSpawnLoader.cs](../AeroScape.Server.Core/Services/NpcSpawnLoader.cs) lines 10-23 and 58-71, but `NPC` has no persisted/displayed examine field, so that piece of legacy data is currently dropped.

- `GameEngine` resolves NPC config files relative to `Directory.GetCurrentDirectory()` in [AeroScape.Server.Core/Engine/GameEngine.cs](../AeroScape.Server.Core/Engine/GameEngine.cs) lines 415-416. Launching the app from a different working directory will break NPC loading even though the files exist in the repository.

- New accounts are created with admin rights by default in [AeroScape.Server.App/Services/PlayerLoginService.cs](../AeroScape.Server.App/Services/PlayerLoginService.cs) line 74. This may be intentional for development, but it diverges sharply from legacy live-server behavior and will distort any content audit done through gameplay.
