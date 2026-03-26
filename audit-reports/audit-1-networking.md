# Audit Report 1: Networking & Protocol

Audit scope:
- `AeroScape.Server.Network/Update/PlayerUpdateWriter.cs`
- `AeroScape.Server.Network/Update/NpcUpdateWriter.cs`
- `AeroScape.Server.Network/Login/LoginHandler.cs`
- `AeroScape.Server.Network/Protocol/PacketDecoders.cs`
- `AeroScape.Server.Network/Protocol/PacketRouter.cs`
- `AeroScape.Server.Network/Frames/LoginFrames.cs`
- `AeroScape.Server.Network/Frames/GameFrames.cs`
- `AeroScape.Server.Core/Crypto/IsaacCipher.cs`

Method:
- Read the full C# files listed above.
- Read the full legacy Java counterparts, including the packet handler classes under `legacy-java/server508/src/main/java/DavidScape/io/packets/`, because `Packets.java` only defines framing and sizes.
- Compared field order, byte transforms, mask bits, frame ids, and missing behavior.

Limits:
- `dotnet` is not installed in this environment, so I could not run a clean build. I checked compile-surface issues from source only.
- I could not locate a legacy Java ISAAC source file in `legacy-java`. The ISAAC findings below are based on comparison to the canonical RS/Palidino ISAAC algorithm and the way the C# code is used elsewhere.

## Critical Bugs (will crash/break the client)

### `AeroScape.Server.Core/Crypto/IsaacCipher.cs`
- `IsaacCipher.cs:49-50`, `55-56`, `61-62`, `67-68`, `76-77`, `82-83`, `88-89`, `94-95` use `(x & 0xFF)` and `((y >> 8) & 0xFF)` style indexing via `Mask = 255` and `SizeLog = 8`.
- Canonical ISAAC indexes the 256-word state array with word-aligned indices (`(x & 0x3FC) >> 2`, `(y >> 8 & 0x3FC) >> 2` or equivalent). The current code indexes by byte, not by word.
- Impact: the generated keystream does not match the RS 508 ISAAC stream. If this class is used for inbound or outbound opcode encryption, packet ids will decrypt incorrectly and the protocol will desync immediately.

### `AeroScape.Server.Network/Protocol/PacketDecoders.cs`
- Multiple decoders do not match the legacy byte order / transformation logic:
  - `WalkDecoder` at `PacketDecoders.cs:115-117` returns raw `firstX/firstY`. Legacy `Walking.java:30-32` subtracts `(mapRegionX - 6) * 8` and `(mapRegionY - 6) * 8` before queueing. This changes the actual walk destination.
  - `PublicChatDecoder` at `PacketDecoders.cs:137-151` does not call RS chat decompression. Legacy `PublicChat.java:21-24` uses `Misc.decryptPlayerChat(...)`. Current C# will treat compressed chat bytes as ASCII garbage.
  - `EquipItemDecoder` at `PacketDecoders.cs:202-207` decodes the 8-byte equip packet as `DWord + Word + Word`. Legacy `Equipment.java:76-103` reads `DWord_v2`, then `UnsignedWordBigEndian` item id, then two single bytes. The C# field layout is wrong.
  - `ItemOperateDecoder` at `PacketDecoders.cs:218-223` treats the payload as packed interface/slot/item. Legacy `ItemOperate.java:22-24` reads `DWord`, `UnsignedWordA`, `UnsignedWordBigEndianA`. The transforms are wrong.
  - `DropItemDecoder` at `PacketDecoders.cs:234-239` reads plain words. Legacy `DropItem.java:21-23` uses `UnsignedWordBigEndianA` for slot and plain `UnsignedWord` for item id.
  - `PlayerOption1Decoder` at `PacketDecoders.cs:263-266` reads `UnsignedWord()`. Legacy `PlayerOption1.java:31-33` reads `UnsignedWordBigEndian()`.
  - `NPCOption1Decoder` at `PacketDecoders.cs:311-315` reads `UnsignedWord()`. Legacy `NPCOption1.java:53` reads `UnsignedWordA()`.
  - `NPCOption3Decoder` at `PacketDecoders.cs:335-338` reads `UnsignedWord()`. Legacy `NPCOption3.java:18` reads `UnsignedWordBigEndian()`.
  - `MagicOnPlayerDecoder` at `PacketDecoders.cs:503-508` reads four plain words. Legacy `MagicOnPlayer.java:21-24` reads `SignedWordA`, `SignedWordBigEndian`, `UnsignedWord`, `UnsignedWord`.
  - `ItemSelectDecoder` at `PacketDecoders.cs:438-445` assumes a packed dword interface hash. Legacy `ItemSelect.java:59-64` reads `byte`, `UnsignedWord`, `byte`, `UnsignedWordBigEndian`, `UnsignedWordA`.
  - `ItemGiveDecoder` at `PacketDecoders.cs:472-476` inserts an extra skipped byte and then reads `SignedWordBigEndian`. Legacy `ItemGive.java:25-26` reads only `SignedWordA` then `SignedWordBigEndian`; the C# decode is misaligned.
  - `ItemOnNPCDecoder` at `PacketDecoders.cs:533-539` reads three plain words, but legacy `ItemOnNPC.java:18-21` uses `UnsignedWordA()` four times.
- Impact: basic interaction packets will target the wrong item ids, slots, NPC ids, player ids, or coordinates. Equip/use/drop/click/chat are not protocol-compatible.

### `AeroScape.Server.Network/Frames/LoginFrames.cs`
- The HD tab layout is wrong. `LoginFrames.cs:90-112` uses the same tab slot ids as the low-detail pane, but legacy HD layout uses different parent slots (`Frames.java` via `setInterfaces`, mirrored correctly in `GameFrames.cs:707-728`).
- The music tab child id is wrong in both branches. `LoginFrames.cs:85` and `112` send child `239`; legacy uses child `187` (`Frames.java` non-HD `setTab(..., 86, 187)`, HD `setInterface(..., 100, 187)`).
- Impact: HD clients will attach tabs to the wrong components on login; the music tab is also wrong for both layouts. This is the kind of mismatch that leaves the client in a broken or partially unusable state immediately after login.

## Logic Errors (wrong behavior)

### `AeroScape.Server.Network/Login/LoginHandler.cs`
- `LoginHandler.cs:211` reads `serverKeyEcho` and never validates it against the server session key. Legacy login expects the echoed key to match the server-issued session key.
- `LoginHandler.cs:61-253` only parses the handshake and returns `LoginResult`. Legacy `Login.java:153-479` also performs duplicate-login handling, password verification, ban checks, account load, rights assignment, login response selection, initial coord correction, initial region send, and the first post-login frame sequence.
- `LoginHandler.cs:269-286` has the correct 9-byte login response shape, but `HandleLoginAsync` never calls it. In Java, the handshake code writes the response in the same flow before continuing into the in-game init.
- `LoginHandler.cs:145-149` accepts any packet size up to 500, but does not re-check the expected encrypted block layout the way Java does with `loginEncryptPacketSize`.
- Impact: this file is not a full port of `Login.java`; it is only a partial parser. The missing response/account-state logic changes observable login behavior even when the raw handshake parses.

### `AeroScape.Server.Network/Protocol/PacketRouter.cs`
- `PacketRouter.cs:133` indexes `ProtocolDictionary.Incoming[opcode]` directly. Java `Packets.java:49-89` treats invalid ids / sizes as a soft failure and stops reading. A bad opcode in C# can throw and kill the session loop.
- `PacketRouter.cs:146-149` treats `UnknownSize` packets as "consume the rest of the current buffer". That mirrors Java's crude `avail` behavior, but combined with pipeline buffering it is riskier here because a single read can contain multiple packets.
- `PacketRouter.cs:121-170` does not preserve Java's `packetSize >= 500` guard from `Packets.java:82-88`.
- Impact: malformed input and ISAAC desync are handled less defensively than in the Java server.

### `AeroScape.Server.Network/Protocol/PacketDecoders.cs`
- `ActionButtonsDecoder` at `PacketDecoders.cs:177-191` never normalizes `65535` to `0` for the third button field. Legacy `ActionButtons.java:55-60` does.
- `PrivateMessageDecoder` at `PacketDecoders.cs:618-631` does not decrypt compressed RS private chat text. Legacy `PacketManager.java:264-277` uses `Misc.decryptPlayerChat(...)`.
- `WalkDecoder` at `PacketDecoders.cs:112-126` decodes movement but does not mirror the side effects in `Walking.java:18-64` such as queue reset, interface teardown, and option flag resets. If that logic is not recreated elsewhere, behavior diverges from legacy even when the bytes decode.

### `AeroScape.Server.Network/Frames/LoginFrames.cs`
- `LoginFrames.cs:181-235` writes zero XTEA keys when region data is missing. Legacy `Frames.setMapRegion` teleports the player to Varrock and aborts the region send when map data is missing (`Frames.java:1127-1131`).
- `LoginFrames.cs:24-151` sends only a small subset of the legacy post-login sequence. Missing Java behavior includes `setWelcome`, `connecttofserver`, `setInterfaces`, the quest tab strings, and several side-effect frames from `Login.java:287-359`.
- Impact: even if the client gets past the loading screen, the initial interface state is not faithful to the legacy login flow.

### `AeroScape.Server.Network/Update/PlayerUpdateWriter.cs`
- `PlayerUpdateWriter.cs:268-311` also zero-fills missing map keys. Legacy `Frames.setMapRegion` teleports to a safe region on missing map data (`Frames.java:1127-1131`).
- `PlayerUpdateWriter.cs:107-138` only handles player updates. Legacy `PlayerUpdate.java:170-176` performs player update and NPC update in the same update pass. If no higher-level caller pairs this with `NpcUpdateWriter.UpdateNpc`, the port is behaviorally incomplete.
- `PlayerUpdateWriter.cs:416-422` correctly re-encodes chat, but the incoming side does not decode it. End-to-end chat therefore does not round-trip the legacy protocol.

### `AeroScape.Server.Network/Frames/GameFrames.cs`
- Most packet writing is close to the legacy source, but this file and `LoginFrames.cs` disagree on the login-time tab/interface setup. `GameFrames.SetInterfaces` uses the legacy HD layout (`GameFrames.cs:706-728`), while `LoginFrames` does not (`LoginFrames.cs:90-112`).
- This creates a split-brain login sequence: initial login frames and later frame helpers are not encoding the same UI contract.

### `AeroScape.Server.Network/Update/NpcUpdateWriter.cs`
- Core NPC update masks and movement are mostly faithful to the Java source. I did not find a protocol-byte mismatch here beyond the Java-origin duplicated speak-text append, which the C# port preserves exactly (`NpcUpdateWriter.cs:176-177`, same as `NPCUpdate.java:114-119`).
- The file is therefore comparatively safe, but it inherits the same missing higher-level orchestration concern noted above: it must be invoked alongside player update generation every tick.

## Missing Implementations (stubs that need filling)

### `AeroScape.Server.Network/Protocol/PacketRouter.cs`
- Missing registrations for Java packet behavior that exists in `PacketManager.java` / `Packets.java`, including at least:
  - `42` clan join
  - `43` user input
  - `62` object spawn refresh
  - `99` unknown 4-byte packet
  - `117`, `247`, `248` login-success follow-up packets
  - `127` string input
  - `189` qword input
  - `190` construction/build packet
  - `200` clan kick
  - `ItemOnNPC` packet path is present in C# as a decoder type but is not registered at all
- Missing these means the router is not a functional replacement for the Java packet layer.

### `AeroScape.Server.Network/Protocol/PacketDecoders.cs`
- `PublicChatDecoder` and `PrivateMessageDecoder` both contain explicit placeholder behavior instead of real RS text decompression (`PacketDecoders.cs:141-150`, `622-631`).
- `DialogueContinueDecoder` discards the legacy payload entirely. Java comments say the 6-byte payload is consumed but not used; if later game logic depends on component ids, this decoder has no path to expose them.

### `AeroScape.Server.Network/Login/LoginHandler.cs`
- This is not a full replacement for `Login.java`. Missing implementation areas include:
  - duplicate-login eviction / return code 5
  - password verification / return code 3
  - banned IP / banned character handling / return code 4
  - character load / rights init / skill cap checks
  - the immediate post-login setup done after the response bytes are written

### `AeroScape.Server.Network/Frames/LoginFrames.cs`
- This file is only a reduced login sequence. It does not reproduce the full legacy initialization performed by `Login.java` plus `Frames.java`.

## Minor Issues (style, naming, non-functional)

### `AeroScape.Server.Network/Update/PlayerUpdateWriter.cs`
- `LegacyAppearanceData.ContainsAny` at `PlayerUpdateWriter.cs:627-641` is case-sensitive. Java item-name checks use mixed-case string tables and `contains(...)` on the original item name. This can cause subtle appearance mismatches for equipment names whose case does not match exactly.

### `AeroScape.Server.Network/Update/NpcUpdateWriter.cs`
- `newNpcIds` at `NpcUpdateWriter.cs:13` is written but never used.

### `AeroScape.Server.Network/Frames/GameFrames.cs`
- `ChatEncodeValues` / `ChatEncodeSizes` duplicate the same tables also embedded in `PlayerUpdateWriter` chat support. This is not a protocol bug, but it is easy for the two copies to drift.

### `AeroScape.Server.Network/Login/LoginHandler.cs`
- `usingHD` is assigned twice (`LoginHandler.cs:165`, `190`) with the first assignment unused until the HD marker is processed.

### `AeroScape.Server.Network/Protocol/PacketRouter.cs`
- Decoder registration is hand-maintained and already diverged from the Java packet surface. With this many opcodes, a generated table from `Protocol_508.json` plus explicit decoder binding would be less error-prone.

## Summary by File

- `PlayerUpdateWriter.cs`: mostly faithful on mask bits and movement/update ordering; notable divergences are missing NPC-update orchestration and different missing-map handling.
- `NpcUpdateWriter.cs`: mostly faithful to the legacy NPC update path.
- `LoginHandler.cs`: partial handshake parser only, not a full port of `Login.java`.
- `PacketDecoders.cs`: highest-risk file in the audit; many core packets are decoded with the wrong byte order or not decoded fully.
- `PacketRouter.cs`: incomplete opcode coverage and weaker defensive framing than Java.
- `LoginFrames.cs`: not protocol-faithful, especially for HD tab/interface setup.
- `GameFrames.cs`: largely close to Java, but inconsistent with `LoginFrames.cs`.
- `IsaacCipher.cs`: algorithm is not faithful to standard RS ISAAC indexing and is likely unusable for opcode ciphering.

## Build Verification

I could not run `dotnet build` because `dotnet` is not installed in this environment. Source-level inspection did not reveal obvious namespace or syntax errors in the audited files, but build status is not verified in this report.
