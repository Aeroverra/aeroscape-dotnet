using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class SwitchItems2MessageHandler : IMessageHandler<SwitchItems2Message>
{
    private readonly ILogger<SwitchItems2MessageHandler> _logger;

    public SwitchItems2MessageHandler(ILogger<SwitchItems2MessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, SwitchItems2Message message, CancellationToken cancellationToken)
    {
        // TODO: Implement extended item switching / bank tab drag logic.
        // Legacy packet reads: toInterface (DWord), fromInterface (DWord), fromId (UnsignedWord),
        //   toId (UnsignedWordBigEndian). interfaceId = toInterface >> 16. tabId = toInterface - 49938432.
        // Cross-interface drags (inventory <-> bank) are rejected.
        _logger.LogInformation("[SwitchItems2] Player {SessionId} interface={InterfaceId} tab={TabId} from={FromSlot} to={ToSlot}", session.SessionId, message.InterfaceId, message.TabId, message.FromSlot, message.ToSlot);

        // Reject cross-interface drags (inventory <-> bank)
        if ((message.FromInterface == 50003968 || message.ToInterface == 50003968) &&
            message.FromInterface != message.ToInterface)
        {
            return Task.CompletedTask;
        }

        switch (message.InterfaceId)
        {
            case 762: // Bank interface
                HandleBankSwitch(session, message);
                break;

            case 763: // Inventory (while bank is open)
                // TODO: Swap items[FromSlot] <-> items[ToSlot], refresh both inventory containers.
                _logger.LogInformation("[SwitchItems2] Inventory swap (bank open) from={FromSlot} to={ToSlot}", message.FromSlot, message.ToSlot);
                break;

            default:
                _logger.LogInformation("[SwitchItems2] Unhandled interface: {InterfaceId}", message.InterfaceId);
                break;
        }

        return Task.CompletedTask;
    }

    private void HandleBankSwitch(PlayerSession session, SwitchItems2Message message)
    {
        switch (message.TabId)
        {
            case 73: // Swap/insert within bank
                // TODO: If insert mode, use bank insert logic; otherwise swap bankItems[from] <-> bankItems[to].
                _logger.LogInformation("[SwitchItems2] Bank slot swap/insert from={FromSlot} to={ToSlot}", message.FromSlot, message.ToSlot);
                break;

            // Tab icon drags (move item to specific tab)
            case 41: // Main tab (10)
            case 39: // Tab 2
            case 37: // Tab 3
            case 35: // Tab 4
            case 33: // Tab 5
            case 31: // Tab 6
            case 29: // Tab 7
            case 27: // Tab 8
            case 25: // Tab 9
            // Direct tab drags
            case 51: // Main tab (10)
            case 52: // Tab 2
            case 53: // Tab 3
            case 54: // Tab 4
            case 55: // Tab 5
            case 56: // Tab 6
            case 57: // Tab 7
            case 58: // Tab 8
            case 59: // Tab 9
                // TODO: Insert item into target tab, adjust tab start slots, send tab config.
                _logger.LogInformation("[SwitchItems2] Bank tab drag tabId={TabId} from={FromSlot}", message.TabId, message.FromSlot);
                break;

            default:
                _logger.LogInformation("[SwitchItems2] Unhandled bank tab option: {TabId}", message.TabId);
                break;
        }
    }
}
