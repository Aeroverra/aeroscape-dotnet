using System;
using System.Threading;
using System.Threading.Tasks;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class ObjectOption2MessageHandler : IMessageHandler<ObjectOption2Message>
{
    public Task HandleAsync(PlayerSession session, ObjectOption2Message message, CancellationToken cancellationToken)
    {
        // TODO: Implement object option 2 logic
        // Legacy examples:
        //   28089 → Open bank
        //   34874 → Coordinate-based traversal (gate/door)
        Console.WriteLine($"[ObjectOption2] Player {session.SessionId} used object option 2: ObjectId={message.ObjectId}, X={message.ObjectX}, Y={message.ObjectY}");
        return Task.CompletedTask;
    }
}
