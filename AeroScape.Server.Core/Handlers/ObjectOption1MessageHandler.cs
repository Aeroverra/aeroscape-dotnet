using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Services;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class ObjectOption1MessageHandler : IMessageHandler<ObjectOption1Message>
{
    private readonly ILogger<ObjectOption1MessageHandler> _logger;
    private readonly ObjectInteractionService _objects;

    public ObjectOption1MessageHandler(ILogger<ObjectOption1MessageHandler> logger, ObjectInteractionService objects)
    {
        _logger = logger;
        _objects = objects;
    }

    public Task HandleAsync(PlayerSession session, ObjectOption1Message message, CancellationToken cancellationToken)
    {
        if (session.Entity is null)
            return Task.CompletedTask;

        bool handled = _objects.HandleOption1(session.Entity, message.ObjectId, message.ObjectX, message.ObjectY);
        _logger.LogInformation("[ObjectOption1] Player {Username} object={ObjectId} handled={Handled}", session.Entity.Username, message.ObjectId, handled);
        return Task.CompletedTask;
    }
}
