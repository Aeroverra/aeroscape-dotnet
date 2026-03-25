using System;
using System.Net.Sockets;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using AeroScape.Server.Core.Entities;

namespace AeroScape.Server.Core.Session;

public class PlayerSession : IAsyncDisposable
{
    public Guid SessionId { get; } = Guid.NewGuid();
    public int Revision { get; set; }
    public Player Entity { get; set; }
    public string IpAddress { get; }
    
    private readonly TcpClient _tcpClient;
    private readonly PipeWriter _writer;
    private readonly CancellationTokenSource _cts = new();

    public PlayerSession(TcpClient tcpClient, PipeWriter writer)
    {
        _tcpClient = tcpClient;
        _writer = writer;
        IpAddress = tcpClient.Client.RemoteEndPoint?.ToString() ?? "Unknown";
    }
    
    public CancellationToken CancellationToken => _cts.Token;

    public void Disconnect()
    {
        _cts.Cancel();
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _tcpClient.Dispose();
        await _writer.CompleteAsync();
    }
}