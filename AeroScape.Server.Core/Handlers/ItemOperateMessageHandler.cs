using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class ItemOperateMessageHandler : IMessageHandler<ItemOperateMessage>
{
    private readonly ILogger<ItemOperateMessageHandler> _logger;

    public ItemOperateMessageHandler(ILogger<ItemOperateMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, ItemOperateMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[ItemOperate] Player {SessionId} operated item {ItemId} in slot {SlotId}", session.SessionId, message.ItemId, message.SlotId);
        return Task.CompletedTask;
    }
}