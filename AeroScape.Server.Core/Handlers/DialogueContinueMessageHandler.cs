using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Services;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class DialogueContinueMessageHandler : IMessageHandler<DialogueContinueMessage>
{
    private readonly ILogger<DialogueContinueMessageHandler> _logger;
    private readonly DialogueService _dialogues;

    public DialogueContinueMessageHandler(ILogger<DialogueContinueMessageHandler> logger, DialogueService dialogues)
    {
        _logger = logger;
        _dialogues = dialogues;
    }

    public Task HandleAsync(PlayerSession session, DialogueContinueMessage message, CancellationToken cancellationToken)
    {
        if (session.Entity is null)
            return Task.CompletedTask;

        bool handled = _dialogues.Continue(session.Entity);
        _logger.LogInformation("[DialogueContinue] Player {Username} dialogue={Dialogue} handled={Handled}", session.Entity.Username, session.Entity.Dialogue, handled);
        return Task.CompletedTask;
    }
}
