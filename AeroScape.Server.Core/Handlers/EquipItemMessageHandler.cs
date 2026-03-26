using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class EquipItemMessageHandler : IMessageHandler<EquipItemMessage>
{
    private readonly ILogger<EquipItemMessageHandler> _logger;

    public EquipItemMessageHandler(ILogger<EquipItemMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, EquipItemMessage message, CancellationToken cancellationToken)
    {
        // TODO: Implement item equip logic
        _logger.LogInformation("[EquipItem] Player {SessionId} equipped item {ItemId} to slot {Slot}", session.SessionId, message.ItemId, message.Slot);
        return Task.CompletedTask;
    }
}