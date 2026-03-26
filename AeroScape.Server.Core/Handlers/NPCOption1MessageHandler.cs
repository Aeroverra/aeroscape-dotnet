using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Engine;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Services;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class NPCOption1MessageHandler : IMessageHandler<NPCOption1Message>
{
    private readonly ILogger<NPCOption1MessageHandler> _logger;
    private readonly GameEngine _engine;
    private readonly ShopService _shops;

    public NPCOption1MessageHandler(ILogger<NPCOption1MessageHandler> logger, GameEngine engine, ShopService shops)
    {
        _logger = logger;
        _engine = engine;
        _shops = shops;
    }

    public Task HandleAsync(PlayerSession session, NPCOption1Message message, CancellationToken cancellationToken)
    {
        var player = session.Entity;
        if (player is null || message.NpcIndex <= 0 || message.NpcIndex >= _engine.Npcs.Length)
            return Task.CompletedTask;

        var npc = _engine.Npcs[message.NpcIndex];
        if (npc is null)
            return Task.CompletedTask;

        switch (npc.NpcType)
        {
            case 494:
            case 495:
            case 2619:
            case 2270:
                player.InterfaceId = 762;
                break;
            case 549:
            case 548:
            case 521:
            case 682:
            case 6970:
                _shops.OpenShop(player, npc.NpcType switch
                {
                    549 => 13,
                    548 => 14,
                    521 => 5,
                    682 => 3,
                    6970 => 11,
                    _ => 1
                });
                break;
            case 198:
                player.Dialogue = 100;
                break;
            case 747:
                player.Dialogue = 105;
                break;
            case 746:
                player.Dialogue = 108;
                break;
        }

        _logger.LogInformation("[NPCOption1] Player {Username} npcType={NpcType}", player.Username, npc.NpcType);
        return Task.CompletedTask;
    }
}
