using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Items;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;
using AeroScape.Server.Core.Services;


namespace AeroScape.Server.Core.Handlers;

public class DropItemMessageHandler : IMessageHandler<DropItemMessage>
{
    private readonly ILogger<DropItemMessageHandler> _logger;
    private readonly PlayerItemsService _items;
    private readonly GroundItemManager _groundItems;
    private readonly ItemDefinitionLoader _definitions;
    private readonly IClientUiService _ui;


    public DropItemMessageHandler(
        ILogger<DropItemMessageHandler> logger, 
        PlayerItemsService items, 
        GroundItemManager groundItems, 
        ItemDefinitionLoader definitions,
        IClientUiService ui)
    {
        _logger = logger;
        _items = items;
        _groundItems = groundItems;
        _definitions = definitions;
        _ui = ui;
    }
    
    public Task HandleAsync(PlayerSession session, DropItemMessage message, CancellationToken cancellationToken)
    {
        var player = session.Entity;
        if (player is null || message.Slot < 0 || message.Slot >= player.Items.Length || player.Items[message.Slot] != message.ItemId)
        {
            return Task.CompletedTask;
        }

        player.ClickDelay = 1;

        // Check LoadedBackup timer as per Java
        if (player.LoadedBackup > 0)
        {
            _ui.SendMessage(player, $"You must wait {(player.LoadedBackup * 3 / 5)} more seconds until you can drop an item after loading your backup.");
            return Task.CompletedTask;
        }

        // Check if admin without drop permission
        if (player.Rights > 1 && !player.Username.Equals("david", StringComparison.OrdinalIgnoreCase))
        {
            _items.DeleteItem(player, message.ItemId, message.Slot, player.ItemsN[message.Slot]);
            return Task.CompletedTask;
        }

        // Handle pet summoning items
        if (HandlePetSummoning(player, message.ItemId, message.Slot))
        {
            return Task.CompletedTask;
        }

        // Handle untradable items - show destroy confirmation
        if (_definitions.IsUntradable(message.ItemId))
        {
            // TODO: Implement destroy item confirmation interface
            // For now, just prevent dropping
            _ui.SendMessage(player, "This item cannot be dropped. It would need to be destroyed.");
            return Task.CompletedTask;
        }

        // Normal item drop
        var amount = player.ItemsN[message.Slot];
        if (_groundItems.CreateGroundItem(message.ItemId, amount, player.AbsX, player.AbsY, player.HeightLevel) &&
            _items.DeleteItem(player, message.ItemId, message.Slot, amount))
        {
            _logger.LogInformation("[DropItem] Player {SessionId} dropped item {ItemId} x{Amount} from slot {Slot}", session.SessionId, message.ItemId, amount, message.Slot);
        }

        return Task.CompletedTask;
    }

    private bool HandlePetSummoning(Entities.Player player, int itemId, int slot)
    {
        if (player.FamiliarId != 0)
        {
            return false; // Already has a familiar
        }

        switch (itemId)
        {
            case 12469: // Baby black dragon
                return SummonPet(player, itemId, slot, 6901, 99, "You drop your dragon on the ground.");
                
            case 12471: // Baby blue dragon
                return SummonPet(player, itemId, slot, 6903, 99, "You drop your dragon on the ground.");
                
            case 8942: // Monkey pet
                return SummonPet(player, itemId, slot, 6943, 75, "You drop your pet monkey on the ground.");
                
            case 12473: // Baby green dragon
                return SummonPet(player, itemId, slot, 6905, 99, "You drop your dragon on the ground.");
                
            case 12475: // Baby red dragon
                return SummonPet(player, itemId, slot, 6907, 99, "You drop your dragon on the ground.");
                
            default:
                return false;
        }
    }

    private bool SummonPet(Entities.Player player, int itemId, int slot, int npcId, int requiredLevel, string message)
    {
        if (player.SkillLvl[23] < requiredLevel) // Summoning skill
        {
            _ui.SendMessage(player, $"You need {requiredLevel} summoning to drop this pet.");
            return true;
        }

        player.RequestAnim(827, 0);
        _ui.SendMessage(player, message);
        
        // TODO: Implement familiar tab UI updates
        // p.frames.setTab(p, 80, 663);
        // p.frames.animateInterfaceId(p, 9850, 663, 3);
        // p.frames.setNPCId(p, npcId, 663, 3);
        
        // TODO: Spawn the familiar NPC
        // Need to implement Engine.NewSummonNPC method or find equivalent
        // _engine.NewSummonNPC(npcId, player.AbsX, player.AbsY + 1, player.HeightLevel, 
        //     player.AbsX + 1, player.AbsY + 1, player.AbsX - 1, player.AbsY - 1, 
        //     false, player.PlayerId);
        
        player.FamiliarType = npcId;
        _items.DeleteItem(player, itemId, slot, player.ItemsN[slot]);
        
        return true;
    }
}
