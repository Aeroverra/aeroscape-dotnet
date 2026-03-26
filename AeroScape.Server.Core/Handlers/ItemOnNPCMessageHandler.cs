using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class ItemOnNPCMessageHandler : IMessageHandler<ItemOnNPCMessage>
{
    private readonly ILogger<ItemOnNPCMessageHandler> _logger;

    public ItemOnNPCMessageHandler(ILogger<ItemOnNPCMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, ItemOnNPCMessage message, CancellationToken cancellationToken)
    {
        // TODO: Implement item-on-NPC logic (e.g. feeding, quest items, using bones on altars, etc.)
        // Legacy checked InterfaceId == 33152 for a specific interaction.
        _logger.LogInformation("[ItemOnNPC] Player {SessionId} used item {ItemId} on NPC index {NpcIndex} (interface {InterfaceId})", session.SessionId, message.ItemId, message.NpcIndex, message.InterfaceId);
        return Task.CompletedTask;
    }
}
