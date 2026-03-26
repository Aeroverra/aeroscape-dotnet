using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class MagicOnNPCMessageHandler : IMessageHandler<MagicOnNPCMessage>
{
    private readonly ILogger<MagicOnNPCMessageHandler> _logger;

    public MagicOnNPCMessageHandler(ILogger<MagicOnNPCMessageHandler> logger)
    {
        _logger = logger;
    }
    public Task HandleAsync(PlayerSession session, MagicOnNPCMessage message, CancellationToken cancellationToken)
    {
        // TODO: Implement magic-on-NPC combat logic.
        // Legacy supported two spellbooks:
        //   InterfaceId 192 = Modern (Wind/Water/Earth/Fire Strike/Bolt/Blast/Wave/Surge)
        //   InterfaceId 193 = Ancient Magicks (Smoke/Shadow/Blood/Ice Rush/Burst/Blitz/Barrage)
        // Each spell was identified by ButtonId and required specific runes + magic level.
        _logger.LogInformation("[MagicOnNPC] Player {SessionId} cast spell (button {ButtonId}, interface {InterfaceId}) on NPC index {NpcIndex}", session.SessionId, message.ButtonId, message.InterfaceId, message.NpcIndex);
        return Task.CompletedTask;
    }
}
