using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Items;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;
using AeroScape.Server.Core.Frames;

namespace AeroScape.Server.Core.Handlers;

public class EquipItemMessageHandler : IMessageHandler<EquipItemMessage>
{
    private readonly ILogger<EquipItemMessageHandler> _logger;
    private readonly PlayerEquipmentService _equipment;
    private readonly GameFrames _frames;

    public EquipItemMessageHandler(ILogger<EquipItemMessageHandler> logger, PlayerEquipmentService equipment, GameFrames frames)
    {
        _logger = logger;
        _equipment = equipment;
        _frames = frames;
    }
    public Task HandleAsync(PlayerSession session, EquipItemMessage message, CancellationToken cancellationToken)
    {
        var player = session.Entity;
        if (player is null)
        {
            return Task.CompletedTask;
        }

        var equipped = _equipment.Equip(player, message.ItemId, message.Slot, message.InterfaceId);
        if (equipped && player.Session is { } sessionState)
        {
            player.IsAncients = player.Equipment[3] == 4675 ? 1 : 0;
            using var w = new FrameWriter(4096);
            _frames.SetItems(w, 149, 0, 93, player.Items, player.ItemsN);
            _frames.SetItems(w, 387, 28, 93, player.Equipment, player.EquipmentN);
            _frames.SetWeaponTab(w, player);
            w.FlushToAsync(sessionState.GetStream(), sessionState.CancellationToken).GetAwaiter().GetResult();
        }

        _logger.LogInformation("[EquipItem] Player {SessionId} equip item {ItemId} slot {Slot} success={Success}", session.SessionId, message.ItemId, message.Slot, equipped);
        return Task.CompletedTask;
    }
}
