using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class ObjectExamineMessageHandler : IMessageHandler<ObjectExamineMessage>
{
    private readonly ILogger<ObjectExamineMessageHandler> _logger;

    public ObjectExamineMessageHandler(ILogger<ObjectExamineMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, ObjectExamineMessage message, CancellationToken cancellationToken)
    {
        // TODO: Send object description (or ID if player is mod/admin).
        _logger.LogInformation("[ObjectExamine] Player {SessionId} examined object {ObjectId}", session.SessionId, message.ObjectId);
        return Task.CompletedTask;
    }
}
