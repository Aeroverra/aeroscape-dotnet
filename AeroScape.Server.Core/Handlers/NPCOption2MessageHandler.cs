using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Engine;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Services;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public class NPCOption2MessageHandler : IMessageHandler<NPCOption2Message>
{
    private readonly ILogger<NPCOption2MessageHandler> _logger;
    private readonly GameEngine _engine;
    private readonly ShopService _shops;

    public NPCOption2MessageHandler(ILogger<NPCOption2MessageHandler> logger, GameEngine engine, ShopService shops)
    {
        _logger = logger;
        _engine = engine;
        _shops = shops;
    }

    public Task HandleAsync(PlayerSession session, NPCOption2Message message, CancellationToken cancellationToken)
    {
        var player = session.Entity;
        if (player is null || message.NpcIndex <= 0 || message.NpcIndex >= _engine.Npcs.Length)
            return Task.CompletedTask;

        var npc = _engine.Npcs[message.NpcIndex];
        if (npc is null)
            return Task.CompletedTask;

        switch (npc.NpcType)
        {
            case 6970:
                _shops.OpenShop(player, 11);
                break;
            case 549:
                _shops.OpenShop(player, 13);
                break;
            case 548:
                _shops.OpenShop(player, 14);
                break;
            case 521:
                _shops.OpenShop(player, 5);
                break;
            case 682:
                _shops.OpenShop(player, 3);
                break;
            case 494:
            case 495:
            case 2619:
            case 2270:
                player.InterfaceId = 762;
                break;
        }

        _logger.LogInformation("[NPCOption2] Player {Username} npcType={NpcType}", player.Username, npc.NpcType);
        return Task.CompletedTask;
    }
}
