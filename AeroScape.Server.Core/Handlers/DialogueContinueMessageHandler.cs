using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class DialogueContinueMessageHandler : IMessageHandler<DialogueContinueMessage>
{
    private readonly ILogger<DialogueContinueMessageHandler> _logger;

    public DialogueContinueMessageHandler(ILogger<DialogueContinueMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, DialogueContinueMessage message, CancellationToken cancellationToken)
    {
        // TODO: Advance the player's current dialogue state.
        // The legacy code had a massive switch on p.Dialogue (0-111+) handling skill capes,
        // quests (Dragon Slayer), destroy confirmations, and more.
        // This will be refactored into a proper DialogueService in future phases.
        _logger.LogInformation("[DialogueContinue] Player {SessionId}", session.SessionId);
        return Task.CompletedTask;
    }
}
