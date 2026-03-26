using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class ActionButtonsMessageHandler : IMessageHandler<ActionButtonsMessage>
{
    private readonly ILogger<ActionButtonsMessageHandler> _logger;

    public ActionButtonsMessageHandler(ILogger<ActionButtonsMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, ActionButtonsMessage message, CancellationToken cancellationToken)
    {
        // TODO: Implement ActionButtons logic
        _logger.LogInformation("[ActionButtons] Player {SessionId} interface {InterfaceId} button {ButtonId} item {ItemId} slot {SlotId}", session.SessionId, message.InterfaceId, message.ButtonId, message.ItemId, message.SlotId);
        return Task.CompletedTask;
    }
}