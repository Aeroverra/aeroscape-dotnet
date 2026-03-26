using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class ItemGiveMessageHandler : IMessageHandler<ItemGiveMessage>
{
    private readonly ILogger<ItemGiveMessageHandler> _logger;

    public ItemGiveMessageHandler(ILogger<ItemGiveMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, ItemGiveMessage message, CancellationToken cancellationToken)
    {
        // TODO: Implement item give logic
        // Legacy behaviour:
        //   - Reset woodcutting & mining
        //   - Look up target player by index
        //   - Transfer item from sender to target (add to target, delete from sender)
        _logger.LogInformation("[ItemGive] Player {SessionId} giving item {ItemId} to player index {TargetPlayerIndex}", session.SessionId, message.ItemId, message.TargetPlayerIndex);
        return Task.CompletedTask;
    }
}
