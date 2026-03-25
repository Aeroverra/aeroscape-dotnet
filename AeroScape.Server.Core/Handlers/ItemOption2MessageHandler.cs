using System;
using System.Threading;
using System.Threading.Tasks;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class ItemOption2MessageHandler : IMessageHandler<ItemOption2Message>
{
    public Task HandleAsync(PlayerSession session, ItemOption2Message message, CancellationToken cancellationToken)
    {
        // TODO: Implement item option 2 logic
        // Legacy switch was on InterfaceId:
        //   387 → Unequip item from equipment screen
        Console.WriteLine($"[ItemOption2] Player {session.SessionId} used item option 2: ItemId={message.ItemId}, Slot={message.ItemSlot}, Interface={message.InterfaceId}");
        return Task.CompletedTask;
    }
}
