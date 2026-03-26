using System;
using System.Threading;
using System.Threading.Tasks;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Services;
using AeroScape.Server.Core.Session;
using Microsoft.Extensions.Logging;

namespace AeroScape.Server.Core.Handlers;

public class CommandMessageHandler : IMessageHandler<CommandMessage>
{
    private readonly ILogger<CommandMessageHandler> _logger;
    private readonly CommandService _commands;

    public CommandMessageHandler(ILogger<CommandMessageHandler> logger, CommandService commands)
    {
        _logger = logger;
        _commands = commands;
    }

    public Task HandleAsync(PlayerSession session, CommandMessage message, CancellationToken cancellationToken)
    {
        var player = session.Entity;
        if (player is null || string.IsNullOrWhiteSpace(message.CommandText))
            return Task.CompletedTask;

        var parts = message.CommandText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return Task.CompletedTask;

        string command = parts[0].ToLowerInvariant();
        string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

        bool handled = _commands.Execute(player, command, args, message.CommandText);
        _logger.LogInformation("Command {Command} from {Username} handled={Handled}", command, player.Username, handled);
        return Task.CompletedTask;
    }
}
