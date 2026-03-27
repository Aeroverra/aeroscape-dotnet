using System.Collections.Concurrent;
using AeroScape.Server.Core.Entities;

namespace AeroScape.Server.Core.Services;

public interface IClanChatPersistenceService
{
    Task LoadAllChannelsAsync();
    Task SaveChannelAsync(string ownerName, ClanChannel channel);
    Task DeleteChannelAsync(string ownerName);
}

public class ClanChannel
{
    public string Owner { get; }
    public string ClanName { get; set; }
    public int JoinRequirement { get; set; }
    public int TalkRequirement { get; set; }
    public int KickRequirement { get; set; } = 7;
    public bool LootShareOn { get; set; }
    public ConcurrentDictionary<string, int> Ranks { get; } = new(StringComparer.OrdinalIgnoreCase);
    public ConcurrentDictionary<string, ClanMember> Members { get; } = new(StringComparer.OrdinalIgnoreCase);
    public (string PlayerName, string Message) LastMessage { get; set; }

    public ClanChannel(string owner, string clanName)
    {
        Owner = owner;
        ClanName = clanName;
    }
}

public class ClanMember
{
    public string Name { get; }
    public int Chance { get; set; }

    public ClanMember(string name)
    {
        Name = name;
    }
}