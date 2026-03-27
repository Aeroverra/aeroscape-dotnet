# AUDIT ROUND 5 - Protocol Layer Bugs

## Bugs

`AeroScape.Server.Network/Update/PlayerUpdateWriter.cs:295-300` — Runtime map-region rebuilds still use zero-XTEA fallback instead of the Java server's teleport-and-abort behavior. While LoginFrames.cs was fixed to teleport to Varrock when XTEA keys are missing (lines 328-338), PlayerUpdateWriter still falls back to sending four zero dwords when `_mapData.GetMapData(region)` returns null. The Java server teleports the player to Varrock and aborts the region frame in this case, matching the LoginFrames fix. This inconsistency means post-login region changes/teleports can still send invalid region data while the login sequence was fixed. — Java ref: `legacy-java/server508/src/main/java/DavidScape/io/Frames.java:1127-1131`

## Fixed Issues from Previous Rounds

The following previously reported bugs have been successfully fixed:

- **PacketRouter.cs ISAAC opcode decryption** — Fixed in line 135, now correctly reads raw opcodes without ISAAC decryption
- **ObjectOption2Decoder packet structure** — Fixed to read 6 bytes (lines 453-457) instead of incorrect 2-byte structure 
- **ActionButtonsDecoder packet structure** — Fixed to read two separate words (lines 267-268) instead of packed DWord extraction
- **EquipItemDecoder interfaceId extraction** — Fixed to ignore the DWord and read only itemId (lines 284-287)
- **LoginFrames XTEA key handling** — Fixed to teleport to Varrock when keys are missing (lines 328-338)
- **NoOpDecoder routing for opcodes 117, 247, 248** — Correctly routed to NoOpDecoder instead of inappropriate handlers