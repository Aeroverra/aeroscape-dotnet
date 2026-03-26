using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Services;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class PrayerMessageHandler : IMessageHandler<PrayerMessage>
{
    private readonly ILogger<PrayerMessageHandler> _logger;
    private readonly PrayerService _prayers;

    public PrayerMessageHandler(ILogger<PrayerMessageHandler> logger, PrayerService prayers)
    {
        _logger = logger;
        _prayers = prayers;
    }

    public Task HandleAsync(PlayerSession session, PrayerMessage message, CancellationToken cancellationToken)
    {
        if (session.Entity is null)
            return Task.CompletedTask;

        bool handled = _prayers.Toggle(session.Entity, message.ButtonId);
        _logger.LogInformation("[Prayer] Player {Username} button={ButtonId} handled={Handled}", session.Entity.Username, message.ButtonId, handled);
        return Task.CompletedTask;
    }
}
