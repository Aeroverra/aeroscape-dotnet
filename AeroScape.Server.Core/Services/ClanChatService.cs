using System;
using System.Collections.Concurrent;
using AeroScape.Server.Core.Engine;
using AeroScape.Server.Core.Entities;
using Microsoft.Extensions.Logging;

namespace AeroScape.Server.Core.Services;

public sealed class ClanChatService
{
    private readonly GameEngine _engine;
    private readonly IClientUiService _ui;
    private readonly IClanChatPersistenceService? _persistence;
    private readonly ILogger<ClanChatService> _logger;
    private readonly ConcurrentDictionary<string, ClanChannel> _channels = new(StringComparer.OrdinalIgnoreCase);

    public ClanChatService(GameEngine engine, IClientUiService ui, IClanChatPersistenceService? persistence, ILogger<ClanChatService> logger)
    {
        _engine = engine;
        _ui = ui;
        _persistence = persistence;
        _logger = logger;
    }

    public void CreateOrRenameChat(Player owner, string clanName)
    {
        var channel = _channels.AddOrUpdate(
            owner.Username,
            _ => new ClanChannel(owner.Username, clanName),
            (_, existing) =>
            {
                existing.ClanName = clanName;
                return existing;
            });

        owner.OwnClanName = channel.ClanName;
        _ui.SendMessage(owner, $"You changed the name of your clan to: {channel.ClanName}");

        // Persist the change
        if (_persistence != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _persistence.SaveChannelAsync(owner.Username, channel);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to persist clan channel for {Owner}", owner.Username);
                }
            });
        }
    }

    public bool JoinChat(Player player, string ownerName)
    {
        LeaveChat(player);

        if (!_channels.TryGetValue(ownerName, out var channel))
        {
            int ownerId = _engine.GetIdFromName(ownerName);
            var owner = ownerId > 0 ? _engine.Players[ownerId] : null;
            if (owner is null)
                return false;

            channel = _channels.GetOrAdd(owner.Username, _ => new ClanChannel(owner.Username, string.IsNullOrWhiteSpace(owner.OwnClanName) ? owner.Username : owner.OwnClanName));
        }

        if (string.IsNullOrWhiteSpace(channel.ClanName) || channel.JoinRequirement > GetRank(channel, player.Username))
            return false;

        channel.Members[player.Username] = new ClanMember(player.Username);
        player.VisitingClanName = channel.ClanName;
        player.ClanChannel = 1;
        _ui.SendMessage(player, $"You are now talking in: {channel.ClanName}");
        return true;
    }

    public void LeaveChat(Player player)
    {
        foreach (var channel in _channels.Values)
            channel.Members.TryRemove(player.Username, out _);

        player.VisitingClanName = string.Empty;
        player.ClanChannel = 0;
        _ui.ResetClanChatList(player);
    }

    public bool SendMessage(Player player, string message)
    {
        foreach (var channel in _channels.Values)
        {
            if (!channel.Members.ContainsKey(player.Username))
                continue;

            if (channel.TalkRequirement > GetRank(channel, player.Username))
                return false;

            channel.LastMessage = (player.Username, message);
            foreach (var member in channel.Members.Values)
            {
                int id = _engine.GetIdFromName(member.Name);
                var target = id > 0 ? _engine.Players[id] : null;
                if (target is not null)
                    _ui.SendClanChat(target, player, channel.ClanName, message);
            }
            return true;
        }

        return false;
    }

    public void RankPlayer(Player owner, string name, int rank)
    {
        if (!_channels.TryGetValue(owner.Username, out var channel))
            return;

        channel.Ranks[name] = rank;

        // Persist the change
        if (_persistence != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _persistence.SaveChannelAsync(owner.Username, channel);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to persist clan rank change for {Owner}", owner.Username);
                }
            });
        }
    }

    public bool Kick(Player owner, string name)
    {
        if (!_channels.TryGetValue(owner.Username, out var channel))
            return false;

        if (!channel.Members.TryRemove(name, out _))
            return false;

        int id = _engine.GetIdFromName(name);
        var target = id > 0 ? _engine.Players[id] : null;
        if (target is not null)
        {
            target.VisitingClanName = string.Empty;
            target.ClanChannel = 0;
            _ui.SendMessage(target, "You've been kick from the chat.");
            _ui.ResetClanChatList(target);
        }

        return true;
    }

    public void SetRequirement(Player owner, int requirementType, int value)
    {
        if (!_channels.TryGetValue(owner.Username, out var channel))
            return;

        switch (requirementType)
        {
            case 1:
                channel.JoinRequirement = value;
                break;
            case 2:
                channel.TalkRequirement = value;
                break;
            case 3:
                channel.KickRequirement = value;
                break;
        }

        // Persist the change
        if (_persistence != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _persistence.SaveChannelAsync(owner.Username, channel);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to persist clan requirement change for {Owner}", owner.Username);
                }
            });
        }
    }

    public void SetLootShare(Player player, bool enabled)
    {
        var channel = FindPlayersChannel(player);
        if (channel is not null)
            channel.LootShareOn = enabled;
    }

    public void LoadChannel(ClanChannel channel)
    {
        _channels[channel.Owner] = channel;
    }

    public bool LootShareOn(Player player)
        => FindPlayersChannel(player)?.LootShareOn == true;

    private ClanChannel? FindPlayersChannel(Player player)
    {
        foreach (var channel in _channels.Values)
            if (channel.Members.ContainsKey(player.Username))
                return channel;

        return null;
    }

    private static int GetRank(ClanChannel channel, string name)
        => channel.Owner.Equals(name, StringComparison.OrdinalIgnoreCase)
            ? 7
            : channel.Ranks.GetValueOrDefault(name, 0);
}
