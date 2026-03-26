# Manual Bug Report — From Nicholas's Testing (Round 2)

## Client Exception on Login

The 508 client throws a RuntimeException on login:
```
Exception in thread "Thread-3" RuntimeException_Sub1
    at Class14_Sub8_Sub14.method554(Class14_Sub8_Sub14.java:1368)
    at Applet_Sub1.method62(Applet_Sub1.java:1172)
    at Applet_Sub1.run(Applet_Sub1.java:771)
    at java.lang.Thread.run(Thread.java:750)
```

The client gets to the RuneScape loading screen but crashes with this exception. The server console shows it IS successfully connecting and executing DB commands (INSERT INTO Skills with Experience, Level, PlayerId, SkillIndex) — so login handshake completes and the player is created in the database, but something in the post-login frame sequence crashes the client.

## Server Console Observations
- Server is repeatedly executing `INSERT INTO "Skills"` SQL statements with parameters for Experience, Level, PlayerId, SkillIndex
- Multiple `Executed DbCommand` log lines visible
- The server appears to be creating skills rows for a new player account
- Server does NOT show any errors — the crash is client-side only

## Root Cause Hypothesis
The post-login frame sequence (LoginFrames.cs) is sending something the client doesn't expect. This could be:
1. Wrong frame ID or byte order in setMapRegion (frame 142)
2. Wrong interface IDs in the tab setup
3. Player update packet format mismatch
4. Missing or malformed initial player appearance data

## Priority: CRITICAL — This blocks all gameplay testing
