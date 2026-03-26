using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class ObjectOption2MessageHandler : IMessageHandler<ObjectOption2Message>
{
    private readonly ILogger<ObjectOption2MessageHandler> _logger;

    public ObjectOption2MessageHandler(ILogger<ObjectOption2MessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, ObjectOption2Message message, CancellationToken cancellationToken)
    {
        // TODO: Implement object option 2 logic
        // Legacy examples:
        //   28089 → Open bank
        //   34874 → Coordinate-based traversal (gate/door)
        _logger.LogInformation("[ObjectOption2] Player {SessionId} used object option 2: ObjectId={ObjectId}, X={ObjectX}, Y={ObjectY}", session.SessionId, message.ObjectId, message.ObjectX, message.ObjectY);
        return Task.CompletedTask;
    }
}
