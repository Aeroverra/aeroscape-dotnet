using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AeroScape.Server.Core.Session;

public interface IPlayerSessionManager
{
    void AddSession(PlayerSession session);
    void RemoveSession(Guid sessionId);
    PlayerSession GetSession(Guid sessionId);
    IEnumerable<PlayerSession> GetAllSessions();
}

public class PlayerSessionManager : IPlayerSessionManager
{
    private readonly ConcurrentDictionary<Guid, PlayerSession> _sessions = new();

    public void AddSession(PlayerSession session)
    {
        _sessions.TryAdd(session.SessionId, session);
    }

    public void RemoveSession(Guid sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }

    public PlayerSession GetSession(Guid sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }

    public IEnumerable<PlayerSession> GetAllSessions()
    {
        return _sessions.Values;
    }
}