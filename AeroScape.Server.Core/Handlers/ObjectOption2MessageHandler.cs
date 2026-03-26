using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Services;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class ObjectOption2MessageHandler : IMessageHandler<ObjectOption2Message>
{
    private readonly ILogger<ObjectOption2MessageHandler> _logger;
    private readonly ObjectInteractionService _objects;

    public ObjectOption2MessageHandler(ILogger<ObjectOption2MessageHandler> logger, ObjectInteractionService objects)
    {
        _logger = logger;
        _objects = objects;
    }

    public Task HandleAsync(PlayerSession session, ObjectOption2Message message, CancellationToken cancellationToken)
    {
        if (session.Entity is null)
            return Task.CompletedTask;

        bool handled = _objects.HandleOption2(session.Entity, message.ObjectId, message.ObjectX, message.ObjectY);
        _logger.LogInformation("[ObjectOption2] Player {Username} object={ObjectId} handled={Handled}", session.Entity.Username, message.ObjectId, handled);
        return Task.CompletedTask;
    }
}
