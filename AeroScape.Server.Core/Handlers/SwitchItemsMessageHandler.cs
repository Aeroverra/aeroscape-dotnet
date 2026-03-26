using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class SwitchItemsMessageHandler : IMessageHandler<SwitchItemsMessage>
{
    private readonly ILogger<SwitchItemsMessageHandler> _logger;

    public SwitchItemsMessageHandler(ILogger<SwitchItemsMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, SwitchItemsMessage message, CancellationToken cancellationToken)
    {
        // TODO: Implement item switching logic based on interface id.
        // Legacy packet reads: toId (UnsignedWordBigEndianA), junk byte, fromId (UnsignedWordBigEndianA),
        //   junk word, interfaceId (UnsignedByte), junk byte.
        // Interface 149 = inventory swap: swap items[fromId] <-> items[toId], then refresh.
        _logger.LogInformation("[SwitchItems] Player {SessionId} swapped slot {FromSlot} -> {ToSlot} on interface {InterfaceId}", session.SessionId, message.FromSlot, message.ToSlot, message.InterfaceId);

        switch (message.InterfaceId)
        {
            case 149:
                // Inventory swap
                // TODO: Validate slot bounds against player inventory length.
                // TODO: Swap items[FromSlot] <-> items[ToSlot] and itemsN[FromSlot] <-> itemsN[ToSlot].
                // TODO: Send updated inventory frame (interface 149, container 93).
                break;

            default:
                _logger.LogInformation("[SwitchItems] Unhandled interface: {InterfaceId}", message.InterfaceId);
                break;
        }

        return Task.CompletedTask;
    }
}
