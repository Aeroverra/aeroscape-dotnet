using System;
using System.Threading;
using System.Threading.Tasks;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class NPCOption1MessageHandler : IMessageHandler<NPCOption1Message>
{
    public Task HandleAsync(PlayerSession session, NPCOption1Message message, CancellationToken cancellationToken)
    {
        // TODO: Implement NPC option 1 logic (talk-to, trade, pickpocket, catch implings, etc.)
        // Legacy switched on NPC type ID for dialogues, shops, skill capes, fishing, quests, familiars.
        Console.WriteLine($"[NPCOption1] Player {session.SessionId} interacted with NPC index {message.NpcIndex}");
        return Task.CompletedTask;
    }
}
