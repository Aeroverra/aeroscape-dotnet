using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Engine;
using AeroScape.Server.Core.Session;
using AeroScape.Server.Core.World;
using AeroScape.Server.Network.Frames;
using AeroScape.Server.Network.Login;
using AeroScape.Server.Network.Protocol;

namespace AeroScape.Server.Network.Listeners;

/// <summary>
/// Long-running hosted service that listens for incoming TCP connections on the
/// game port (default 43594), performs the RS 508 login handshake, then spawns
/// a pipe-based read loop for game packets.
/// </summary>
public sealed class TcpBackgroundService : BackgroundService
{
    private readonly ILogger<TcpBackgroundService> _logger;
    private readonly IPlayerSessionManager _sessions;
    private readonly PacketRouter _router;
    private readonly GameEngine _engine;
    private readonly IPlayerLoginService _loginService;
    private readonly MapDataService _mapData;

    /// <summary>Game port — classic RS 508 default.</summary>
    private const int DefaultPort = 43594;

    public TcpBackgroundService(
        ILogger<TcpBackgroundService> logger,
        IPlayerSessionManager sessions,
        PacketRouter router,
        GameEngine engine,
        IPlayerLoginService loginService,
        MapDataService mapData)
    {
        _logger       = logger;
        _sessions     = sessions;
        _router       = router;
        _engine       = engine;
        _loginService = loginService;
        _mapData      = mapData;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, DefaultPort);
        listener.Start();
        _logger.LogInformation("AeroScape TCP listener started on port {Port}", DefaultPort);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var tcp = await listener.AcceptTcpClientAsync(stoppingToken);
                tcp.NoDelay = true;
                _ = HandleClientAsync(tcp, stoppingToken);
            }
        }
        finally
        {
            listener.Stop();
            _logger.LogInformation("AeroScape TCP listener stopped");
        }
    }

    private async Task HandleClientAsync(TcpClient tcp, CancellationToken stoppingToken)
    {
        var pipe = new Pipe();
        var session = new PlayerSession(tcp, pipe.Writer);
        _sessions.AddSession(session);
        _logger.LogInformation("Client connected: {Ip} (session {Id})", session.IpAddress, session.SessionId);

        try
        {
            // ═══════════════════════════════════════════════════════════════
            // Phase 1: Login handshake (directly on NetworkStream, no pipe)
            // ═══════════════════════════════════════════════════════════════
            var loginHandler = new LoginHandler(_logger);
            var loginResult = await loginHandler.HandleLoginAsync(session, stoppingToken);

            if (loginResult is null)
            {
                _logger.LogInformation("Session {Id}: login handshake failed, disconnecting", session.SessionId);
                return;
            }

            // Load or create the player in the database
            var (player, returnCode) = await _loginService.LoadOrCreatePlayerAsync(
                loginResult.Username, loginResult.Password);

            // Find a world slot for the player
            int slot = _engine.FindFreePlayerSlot();
            if (slot == -1)
            {
                returnCode = 7; // Server full
            }

            // Send login response
            await loginHandler.SendLoginResponseAsync(
                session, returnCode, player.Rights, slot > 0 ? slot : 1, stoppingToken);

            if (returnCode != 2)
            {
                _logger.LogInformation("Session {Id}: login rejected (code {Code}) for '{User}'",
                    session.SessionId, returnCode, loginResult.Username);
                return;
            }

            // Initialise ISAAC ciphers
            session.InitIsaac(loginResult.IsaacSeed);

            // Register player in the game engine
            player.InitDefaults();
            // Restore position (defaults from DB or spawn point)
            if (player.AbsX == 0 && player.AbsY == 0)
            {
                player.AbsX = 3222;
                player.AbsY = 3219;
            }
            player.Online = true;
            _engine.AddPlayer(player, slot);
            session.Entity = player;

            _logger.LogInformation("Session {Id}: '{User}' logged in (slot {Slot})",
                session.SessionId, loginResult.Username, slot);

            // ═══════════════════════════════════════════════════════════════
            // Phase 2: Send post-login initialization frames
            // ═══════════════════════════════════════════════════════════════
            var netStream = session.GetStream();
            await LoginFrames.SendLoginSequenceAsync(
                netStream, player, loginResult.UsingHD, _mapData, stoppingToken);

            // ═══════════════════════════════════════════════════════════════
            // Phase 3: Game packet processing (pipe-based)
            // ═══════════════════════════════════════════════════════════════
            var fillTask  = FillPipeAsync(netStream, pipe.Writer, session.CancellationToken, stoppingToken);
            var readTask  = ReadPipeAsync(pipe.Reader, session, stoppingToken);
            await Task.WhenAll(fillTask, readTask);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Session {Id} error", session.SessionId);
        }
        finally
        {
            // Clean up game engine slot
            if (session.Entity is { } p && p.PlayerId > 0)
            {
                _engine.RemovePlayer(p.PlayerId);
                _logger.LogInformation("Player '{User}' removed from world", p.Username);
            }

            _sessions.RemoveSession(session.SessionId);
            await session.DisposeAsync();
            _logger.LogInformation("Session {Id} disconnected", session.SessionId);
        }
    }

    /// <summary>Reads raw bytes from the network stream into the pipe.</summary>
    private static async Task FillPipeAsync(
        NetworkStream stream, PipeWriter writer,
        CancellationToken sessionToken, CancellationToken appToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(sessionToken, appToken);

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var memory = writer.GetMemory(512);
                int bytesRead = await stream.ReadAsync(memory, cts.Token);
                if (bytesRead == 0) break; // client disconnected

                writer.Advance(bytesRead);
                var result = await writer.FlushAsync(cts.Token);
                if (result.IsCompleted) break;
            }
        }
        finally
        {
            await writer.CompleteAsync();
        }
    }

    /// <summary>Reads framed packets from the pipe and routes them.</summary>
    private async Task ReadPipeAsync(
        PipeReader reader, PlayerSession session, CancellationToken stoppingToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(session.CancellationToken, stoppingToken);

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var result = await reader.ReadAsync(cts.Token);
                var buffer = result.Buffer;

                long consumed = await _router.ProcessBufferAsync(session, buffer);
                reader.AdvanceTo(buffer.GetPosition(consumed), buffer.End);

                if (result.IsCompleted) break;
            }
        }
        finally
        {
            await reader.CompleteAsync();
        }
    }
}
