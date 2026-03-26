using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Session;
using AeroScape.Server.Core.Util;

namespace AeroScape.Server.Network.Login;

/// <summary>
/// Handles the RS 508 login handshake directly on the raw NetworkStream,
/// BEFORE the connection is handed off to the pipe-based game packet router.
///
/// Port of DavidScape/io/Login.java — the three-stage handshake:
///   Stage 0: Read connection type (14=login, 15=update) → send server session key
///   Stage 1: Read login type byte (16 or 18)
///   Stage 2: Read the full login block (version, username, password) → send response
/// </summary>
public sealed class LoginHandler
{
    private readonly ILogger _logger;

    public LoginHandler(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// The cache update keys sent to the client for JS5/update-server handshake.
    /// Mirrors Misc.uKeys from the legacy Java server.
    /// </summary>
    private static readonly byte[] UpdateKeys =
    {
        0xff, 0x00, 0xff, 0x00, 0x00, 0x00, 0x00, 0xd8, 0x84, 0xa1, 0xa1, 0x2b,
        0x00, 0x00, 0x00, 0xba, 0x58, 0x64, 0xe8, 0x14, 0x00, 0x00, 0x00, 0x7b,
        0xcc, 0xa0, 0x7e, 0x23, 0x00, 0x00, 0x00, 0x48, 0x20, 0x0e, 0xe3, 0x6e,
        0x00, 0x00, 0x01, 0x88, 0xec, 0x0d, 0x58, 0xed, 0x00, 0x00, 0x00, 0x71,
        0xb9, 0x4c, 0xc0, 0x50, 0x00, 0x00, 0x01, 0x8b, 0x5b, 0x61, 0x79, 0x20,
        0x00, 0x00, 0x00, 0x0c, 0x0c, 0x69, 0xb1, 0xc8, 0x00, 0x00, 0x02, 0x31,
        0xc8, 0x56, 0x67, 0x52, 0x00, 0x00, 0x00, 0x69, 0x78, 0x17, 0x7b, 0xe2,
        0x00, 0x00, 0x00, 0xc3, 0x29, 0x76, 0x27, 0x6a, 0x00, 0x00, 0x00, 0x05,
        0x44, 0xe7, 0x75, 0xcb, 0x00, 0x00, 0x00, 0x08, 0x7d, 0x21, 0x80, 0xd5,
        0x00, 0x00, 0x01, 0x58, 0xeb, 0x7d, 0x49, 0x8e, 0x00, 0x00, 0x00, 0x0c,
        0xf4, 0xdf, 0xd6, 0x4d, 0x00, 0x00, 0x00, 0x18, 0xec, 0x33, 0x31, 0x7e,
        0x00, 0x00, 0x00, 0x01, 0xf7, 0x7a, 0x09, 0xe3, 0x00, 0x00, 0x00, 0xd7,
        0xe6, 0xa7, 0xa5, 0x18, 0x00, 0x00, 0x00, 0x45, 0xb5, 0x0a, 0xe0, 0x64,
        0x00, 0x00, 0x00, 0x75, 0xba, 0xf2, 0xa2, 0xb9, 0x00, 0x00, 0x00, 0x5f,
        0x31, 0xff, 0xfd, 0x16, 0x00, 0x00, 0x01, 0x48, 0x03, 0xf5, 0x55, 0xab,
        0x00, 0x00, 0x00, 0x1e, 0x85, 0x03, 0x5e, 0xa7, 0x00, 0x00, 0x00, 0x23,
        0x4e, 0x81, 0xae, 0x7d, 0x00, 0x00, 0x00, 0x18, 0x67, 0x07, 0x33, 0xe3,
        0x00, 0x00, 0x00, 0x14, 0xab, 0x81, 0x05, 0xac, 0x00, 0x00, 0x00, 0x03,
        0x24, 0x75, 0x85, 0x14, 0x00, 0x00, 0x00, 0x36
    };

    /// <summary>
    /// Perform the full login handshake on the given session.
    /// Returns a <see cref="LoginResult"/> on success, or null if the handshake fails.
    /// </summary>
    public async Task<LoginResult?> HandleLoginAsync(PlayerSession session, CancellationToken ct)
    {
        var stream = session.GetStream();
        stream.ReadTimeout = 15_000;  // 15s total login timeout
        stream.WriteTimeout = 5_000;

        try
        {
            // ═══════════════════════════════════════════════════════════════
            // Stage 0: Connection type
            // ═══════════════════════════════════════════════════════════════
            var header = new byte[2];
            await ReadExactAsync(stream, header, 0, 2, ct);

            int connectionType = header[0] & 0xFF;
            int _nameHash = header[1] & 0xFF; // unused but consumed

            if (connectionType == 15)
            {
                // ───────────────────────────────────────────────────────────
                // Update server (JS5 cache) request — send cache update keys.
                // The client connects with opcode 15 FIRST to fetch cache
                // checksums before attempting a login with opcode 14.
                // Mirrors DavidScape/io/Login.java → updateServer()
                // ───────────────────────────────────────────────────────────
                _logger.LogDebug("Session {Id}: update server request, sending cache keys", session.SessionId);

                // Stage 0 of update server: consume 3 remaining header bytes, send 0 (success)
                var updateHeader = new byte[3];
                await ReadExactAsync(stream, updateHeader, 0, 3, ct);

                var successByte = new byte[] { 0 };
                await stream.WriteAsync(successByte, ct);
                await stream.FlushAsync(ct);

                // Stage 1 of update server: client sends 8 more bytes, then we send the update keys
                var updateBlock = new byte[8];
                await ReadExactAsync(stream, updateBlock, 0, 8, ct);

                await stream.WriteAsync(UpdateKeys, ct);
                await stream.FlushAsync(ct);

                // After update keys are sent, the client disconnects and
                // reconnects with opcode 14 for the actual login.
                _logger.LogDebug("Session {Id}: update keys sent, client will reconnect for login", session.SessionId);
                return null;
            }

            if (connectionType != 14)
            {
                _logger.LogDebug("Session {Id}: unexpected connection type {Type}", session.SessionId, connectionType);
                return null;
            }

            // Generate server session key
            var rng = new Random();
            long serverSessionKey = ((long)(rng.NextDouble() * 99999999D) << 32)
                                  + (long)(rng.NextDouble() * 99999999D);

            // Send response: 0 (success byte) + 8 bytes (server session key)
            var stage0Response = new byte[9];
            stage0Response[0] = 0; // status
            WriteLong(stage0Response, 1, serverSessionKey);
            await stream.WriteAsync(stage0Response, ct);
            await stream.FlushAsync(ct);

            // ═══════════════════════════════════════════════════════════════
            // Stage 1: Login type byte + login packet size
            // ═══════════════════════════════════════════════════════════════
            var stage1Header = new byte[3];
            await ReadExactAsync(stream, stage1Header, 0, 3, ct);

            int loginType = stage1Header[0] & 0xFF;
            if (loginType != 16 && loginType != 18 && loginType != 14)
            {
                _logger.LogDebug("Session {Id}: unexpected login type {Type}", session.SessionId, loginType);
                return null;
            }

            int loginPacketSize = ((stage1Header[1] & 0xFF) << 8) | (stage1Header[2] & 0xFF);

            // ═══════════════════════════════════════════════════════════════
            // Stage 2: Read the full login block
            // ═══════════════════════════════════════════════════════════════
            if (loginPacketSize <= 0 || loginPacketSize > 500)
            {
                _logger.LogDebug("Session {Id}: invalid login packet size {Size}", session.SessionId, loginPacketSize);
                return null;
            }

            var loginBlock = new byte[loginPacketSize];
            await ReadExactAsync(stream, loginBlock, 0, loginPacketSize, ct);

            int offset = 0;

            // Client version (4 bytes)
            int clientVersion = ReadInt(loginBlock, ref offset);
            if (clientVersion != 508 && clientVersion != 800 && clientVersion != 900)
            {
                _logger.LogDebug("Session {Id}: unsupported client version {Version}", session.SessionId, clientVersion);
                return null;
            }
            session.Revision = clientVersion;

            bool usingHD = false;

            // Skip: 1 byte (unknown) + 3 words (6 bytes) + 24 bytes (cache IDX) = 31 bytes
            offset += 1; // unknown byte
            offset += 2; // word
            offset += 2; // word
            offset += 2; // word
            offset += 24; // 24 cache idx bytes

            // Read junk string (null/newline terminated)
            while (offset < loginBlock.Length && loginBlock[offset] != 10 && loginBlock[offset] != 0)
                offset++;
            if (offset < loginBlock.Length) offset++; // skip terminator

            // 29 DWords (116 bytes)
            offset += 29 * 4;

            // HD/LD detection byte
            if (offset >= loginBlock.Length)
            {
                _logger.LogDebug("Session {Id}: login block too short at HD byte", session.SessionId);
                return null;
            }

            int hdByte = loginBlock[offset++] & 0xFF;
            usingHD = (hdByte == 10);

            int encryption = hdByte;
            if (encryption != 10 && encryption != 64)
            {
                if (offset < loginBlock.Length)
                    encryption = loginBlock[offset++] & 0xFF;
            }

            if (encryption != 10 && encryption != 64)
            {
                _logger.LogDebug("Session {Id}: invalid encryption marker {E}", session.SessionId, encryption);
                return null;
            }

            // Client session key (8 bytes)
            if (offset + 8 > loginBlock.Length) return null;
            long clientSessionKey = ReadLong(loginBlock, ref offset);

            // Server session key echo (8 bytes)
            if (offset + 8 > loginBlock.Length) return null;
            long serverKeyEcho = ReadLong(loginBlock, ref offset);

            // Username as encoded long (8 bytes)
            if (offset + 8 > loginBlock.Length) return null;
            long usernameLong = ReadLong(loginBlock, ref offset);
            string username = NameUtil.LongToString(usernameLong);
            username = NameUtil.Normalise(username);

            if (string.IsNullOrWhiteSpace(username))
            {
                _logger.LogDebug("Session {Id}: empty username", session.SessionId);
                return null;
            }

            // Validate username characters
            foreach (char c in username)
            {
                if (!char.IsLetterOrDigit(c) && c != ' ')
                {
                    _logger.LogDebug("Session {Id}: invalid character in username", session.SessionId);
                    return null;
                }
            }

            // Password (newline/null terminated string)
            string password = ReadString(loginBlock, ref offset);
            if (string.IsNullOrEmpty(password))
            {
                _logger.LogDebug("Session {Id}: empty password", session.SessionId);
                return null;
            }

            // Build ISAAC seeds from client+server session keys
            int[] isaacSeed = new int[4];
            isaacSeed[0] = (int)(clientSessionKey >> 32);
            isaacSeed[1] = (int)clientSessionKey;
            isaacSeed[2] = (int)(serverSessionKey >> 32);
            isaacSeed[3] = (int)serverSessionKey;

            _logger.LogInformation("Session {Id}: login request from '{Username}' (rev {Rev}, HD={HD})",
                session.SessionId, username, clientVersion, usingHD);

            return new LoginResult(username, password, isaacSeed, usingHD, clientVersion);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Session {Id}: login handshake error", session.SessionId);
            return null;
        }
    }

    /// <summary>
    /// Send the login response to the client.
    /// </summary>
    public async Task SendLoginResponseAsync(PlayerSession session, int returnCode, int rights, int playerId, CancellationToken ct)
    {
        var stream = session.GetStream();

        // The Java server sends: returnCode, rights, 0, 0, 0, 1, 0, playerId, 0
        var response = new byte[9];
        response[0] = (byte)returnCode;
        response[1] = (byte)rights;
        response[2] = 0;
        response[3] = 0;
        response[4] = 0;
        response[5] = 1;
        response[6] = 0;
        response[7] = (byte)playerId;
        response[8] = 0;

        await stream.WriteAsync(response, ct);
        await stream.FlushAsync(ct);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static async Task ReadExactAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken ct)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset + totalRead, count - totalRead), ct);
            if (read == 0)
                throw new EndOfStreamException("Client disconnected during login handshake");
            totalRead += read;
        }
    }

    private static int ReadInt(byte[] buf, ref int offset)
    {
        int val = (buf[offset] << 24) | (buf[offset + 1] << 16) | (buf[offset + 2] << 8) | buf[offset + 3];
        offset += 4;
        return val;
    }

    private static long ReadLong(byte[] buf, ref int offset)
    {
        long hi = (uint)ReadInt(buf, ref offset);
        long lo = (uint)ReadInt(buf, ref offset);
        return (hi << 32) | lo;
    }

    private static void WriteLong(byte[] buf, int offset, long val)
    {
        buf[offset]     = (byte)(val >> 56);
        buf[offset + 1] = (byte)(val >> 48);
        buf[offset + 2] = (byte)(val >> 40);
        buf[offset + 3] = (byte)(val >> 32);
        buf[offset + 4] = (byte)(val >> 24);
        buf[offset + 5] = (byte)(val >> 16);
        buf[offset + 6] = (byte)(val >> 8);
        buf[offset + 7] = (byte)val;
    }

    private static string ReadString(byte[] buf, ref int offset)
    {
        var sb = new StringBuilder();
        while (offset < buf.Length)
        {
            byte b = buf[offset++];
            if (b == 10 || b == 0) break;
            sb.Append((char)b);
        }
        return sb.ToString();
    }
}

/// <summary>
/// Result of a successful login handshake parse.
/// </summary>
public sealed record LoginResult(
    string Username,
    string Password,
    int[] IsaacSeed,
    bool UsingHD,
    int ClientVersion
);
