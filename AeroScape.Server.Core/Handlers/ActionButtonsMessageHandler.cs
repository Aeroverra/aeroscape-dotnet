using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Items;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Services;
using AeroScape.Server.Core.Session;
namespace AeroScape.Server.Core.Handlers;

public class ActionButtonsMessageHandler : IMessageHandler<ActionButtonsMessage>
{
    private readonly ILogger<ActionButtonsMessageHandler> _logger;
    private readonly PlayerBankService _bank;
    private readonly TradingService _trading;
    private readonly PlayerItemsService _items;
    private readonly MagicService _magic;
    private readonly ShopService _shops;
    private readonly PrayerService _prayers;
    private readonly ClanChatService _clanChat;
    private readonly ConstructionService _construction;
    private readonly IClientUiService _ui;

    public ActionButtonsMessageHandler(ILogger<ActionButtonsMessageHandler> logger, PlayerBankService bank, TradingService trading, PlayerItemsService items, MagicService magic, ShopService shops, PrayerService prayers, ClanChatService clanChat, ConstructionService construction, IClientUiService ui)
    {
        _logger = logger;
        _bank = bank;
        _trading = trading;
        _items = items;
        _magic = magic;
        _shops = shops;
        _prayers = prayers;
        _clanChat = clanChat;
        _construction = construction;
        _ui = ui;
    }
    public Task HandleAsync(PlayerSession session, ActionButtonsMessage message, CancellationToken cancellationToken)
    {
        var player = session.Entity;
        if (player is null)
        {
            return Task.CompletedTask;
        }

        switch (message.InterfaceId)
        {
            case 192:
                _magic.TryCastModernAction(player, message.ButtonId);
                break;
            case 193:
                _magic.TryCastAncientAction(player, message.ButtonId);
                break;
            case 430:
                _magic.TryCastLunarAction(player, message.ButtonId);
                break;
            case 589:
                if (message.ButtonId == 9)
                {
                    _ui.ShowInterface(player, 590);
                }
                else if (message.ButtonId == 14)
                {
                    _clanChat.SetLootShare(player, !_clanChat.LootShareOn(player));
                }
                break;
            case 590:
                if (message.ButtonId == 22)
                {
                    if (message.PacketOpcode == 233)
                        _ui.ShowLongTextInput(player, 0, "Enter clan prefix:");
                    else if (message.PacketOpcode == 21)
                        _clanChat.CreateOrRenameChat(player, string.Empty);
                }
                break;
            case 402:
                _construction.AddRoom(player, message.ButtonId - 160);
                break;
            case 300:
                player.Smithing.SmithItem(message.ButtonId);
                break;
            case 271:
                _prayers.Toggle(player, message.ButtonId);
                break;
            case 763:
                if (message.ButtonId == 0)
                {
                    var depositAmount = message.PacketOpcode switch
                    {
                        233 => 1,
                        21 => 5,
                        169 => 10,
                        214 => player.BankX,
                        232 => message.SlotId >= 0 && message.SlotId < player.Items.Length ? _items.InvItemCount(player, player.Items[message.SlotId]) : 0,
                        _ => 0
                    };

                    if (depositAmount > 0)
                    {
                        _bank.Deposit(player, message.SlotId, depositAmount);
                    }
                }
                break;
            case 762:
                if (message.ButtonId == 73)
                {
                    var withdrawAmount = message.PacketOpcode switch
                    {
                        233 => 1,
                        21 => 5,
                        169 => 10,
                        214 => player.BankX,
                        232 => message.SlotId >= 0 && message.SlotId < player.BankItems.Length ? player.BankItemsN[message.SlotId] : 0,
                        133 => message.SlotId >= 0 && message.SlotId < player.BankItems.Length ? Math.Max(0, player.BankItemsN[message.SlotId] - 1) : 0,
                        _ => 0
                    };

                    if (withdrawAmount > 0)
                    {
                        _bank.Withdraw(player, message.SlotId, withdrawAmount);
                    }
                }
                else if (message.ButtonId == 16)
                {
                    player.WithdrawNote = !player.WithdrawNote;
                }
                else if (message.ButtonId == 14)
                {
                    player.InsertMode = !player.InsertMode;
                }
                else if (message.ButtonId is 41 or 39 or 37 or 35 or 33 or 31 or 29 or 27 or 25)
                {
                    if (message.PacketOpcode == 21)
                    {
                        _bank.CollapseTab(player, _bank.GetArrayIndex(message.ButtonId));
                    }
                    else if (message.PacketOpcode == 233)
                    {
                        player.ViewingBankTab = _bank.GetArrayIndex(message.ButtonId);
                    }
                }
                break;
            case 620:
                if (message.ButtonId == 24 && message.SlotId >= 0 && message.SlotId < player.ShopItems.Length)
                {
                    var itemId = player.ShopItems[message.SlotId];
                    if (itemId >= 0)
                    {
                        switch (message.PacketOpcode)
                        {
                            case 233: // Value
                                int shopValue = _shops.GetPrice(player.ShopId, message.SlotId);
                                _ui.SendMessage(player, $"This item costs {shopValue} coin{(shopValue != 1 ? "s" : "")}.");
                                break;
                            case 21:
                                _shops.Buy(player, itemId, 1);
                                break;
                            case 169:
                                _shops.Buy(player, itemId, 5);
                                break;
                            case 214:
                                _shops.Buy(player, itemId, 10);
                                break;
                            case 90: // Examine
                                // Would need item definition service for descriptions
                                _ui.SendMessage(player, "Examine functionality not yet implemented.");
                                break;
                        }
                    }
                }
                break;
            case 621:
                if (message.ButtonId == 0 && message.SlotId >= 0 && message.SlotId < player.Items.Length)
                {
                    var itemId = player.Items[message.SlotId];
                    if (itemId >= 0)
                    {
                        switch (message.PacketOpcode)
                        {
                            case 21:
                                _shops.Sell(player, itemId, 1);
                                break;
                            case 169:
                                _shops.Sell(player, itemId, 5);
                                break;
                            case 214:
                                _shops.Sell(player, itemId, 10);
                                break;
                        }
                    }
                }
                break;
            case 458:
                HandleInterface458Choice(player, message.ButtonId);
                break;
        }

        _trading.HandleActionButton(player, message.InterfaceId, message.PacketOpcode, message.ButtonId, message.SlotId);
        _logger.LogInformation("[ActionButtons] Player {SessionId} opcode {Opcode} interface {InterfaceId} button {ButtonId} item {ItemId} slot {SlotId}", session.SessionId, message.PacketOpcode, message.InterfaceId, message.ButtonId, message.ItemId, message.SlotId);
        return Task.CompletedTask;
    }

    private void HandleInterface458Choice(Player player, int buttonId)
    {
        switch (buttonId)
        {
            case 1:
                if (player.Choice == 1)
                {
                    player.Choice = 0;
                    player.Dialogue = 104;
                    _ui.ShowNpcDialogue(player, 198, "Guildmaster", "I think the oracle on ice mountain has a map.");
                }
                else if (player.Choice == 2)
                {
                    if (_items.HaveItem(player, 1538, 1))
                    {
                        player.Choice = 0;
                        player.Dialogue = 109;
                        _ui.ShowNpcDialogue(player, 744, "Klarense", "Looks like you have a map! Lets go.");
                    }
                    else
                    {
                        player.Choice = 0;
                        player.Dialogue = 0;
                        _ui.ShowNpcDialogue(player, 744, "Klarense", "Sorry mate, I need a map to do that.", 9827);
                    }
                }
                else if (player.Choice == 3)
                {
                    player.Choice = 0;
                    player.Dialogue = 0;
                    player.SetCoords(2399, 5178, 0);
                    // Remove interfaces
                }
                else if (player.ClanGame)
                {
                    player.ClanGame = false;
                    // Set clan war properties
                    player.SetCoords(3291, 3830, 4);
                    _ui.SendMessage(player, "<col=ff0000> You have been brought to a server clan wars game.");
                }
                else if (player.CookTimer > 0)
                {
                    player.CookAmount = 1;
                }
                else if (player.SmithingTimer > 0)
                {
                    player.SmithingAmount = 1;
                }
                else if (player.TalkAgent)
                {
                    // Handle estate agent skill cape or level check
                    var constructionLevel = player.SkillLvl[22]; // Construction skill index
                    if (constructionLevel > 119)
                    {
                        // Give skill cape - this would need specific implementation
                        _ui.SendMessage(player, "You are eligible for the Construction skill cape!");
                    }
                    else
                    {
                        _ui.SendMessage(player, $"You need level 99+ Construction. Current level: {constructionLevel}");
                    }
                }
                else if (player.DecorChange)
                {
                    if (_items.HaveItem(player, 995, 500)) // Coins
                    {
                        _items.DeleteItem(player, 995, 500);
                        // Set house decoration - would need specific property
                        _ui.SendMessage(player, "You purchased Stone decoration!");
                    }
                    else
                    {
                        _ui.SendMessage(player, "You do not have enough coins.");
                    }
                }
                else
                {
                    // Default case - Slayer master
                    var slayerLevel = player.SkillLvl[18]; // Slayer skill index
                    if (slayerLevel > 119)
                    {
                        // Give skill cape
                        _ui.SendMessage(player, "You are eligible for the Slayer skill cape!");
                    }
                    else
                    {
                        _ui.SendMessage(player, $"You need level 99+ Slayer. Current level: {slayerLevel}");
                    }
                }
                break;

            case 2:
                if (player.Choice == 1)
                {
                    player.Choice = 0;
                    player.Dialogue = 104;
                    _ui.ShowNpcDialogue(player, 198, "Guildmaster", "Klarense at port sarim may have a boat for sale.");
                }
                else if (player.Choice == 2)
                {
                    player.Choice = 0;
                    player.Dialogue = 0;
                    _ui.ShowNpcDialogue(player, 744, "Klarense", "Uhh..yeah I guess they're pretty cool.", 9827);
                }
                else if (player.Choice == 3)
                {
                    player.Choice = 0;
                    player.Dialogue = 0;
                    player.SetCoords(2442, 3090, 0);
                }
                else if (player.ClanGame)
                {
                    player.ClanGame = false;
                    // Set clan war properties for team 2
                    player.SetCoords(3299, 3722, 4);
                    _ui.SendMessage(player, "<col=ff0000> You have been brought to a server clan wars game.");
                }
                else if (player.CookTimer > 0)
                {
                    player.CookAmount = 5;
                }
                else if (player.SmithingTimer > 0)
                {
                    player.SmithingAmount = 5;
                }
                else if (player.TalkAgent)
                {
                    _ui.ShowNpcDialogue(player, 4247, "EstateAgent", "Just type ::goinhouse [player name].");
                }
                else if (player.DecorChange)
                {
                    var constructionLevel = player.SkillLvl[22];
                    if (constructionLevel < 50)
                    {
                        _ui.SendMessage(player, "You need level 50 construction for this.");
                    }
                    else if (_items.HaveItem(player, 995, 1000))
                    {
                        _items.DeleteItem(player, 995, 1000);
                        _ui.SendMessage(player, "You purchased Dark Stone decoration!");
                    }
                    else
                    {
                        _ui.SendMessage(player, "You do not have enough coins.");
                    }
                }
                else
                {
                    // Assign slayer task
                    var random = new Random();
                    var taskType = random.Next(5);
                    var taskAmount = 1 + random.Next(50);
                    
                    string[] taskNames = {"Dragons", "Guards", "Giants", "Ghosts", "Heroes"};
                    var taskName = taskNames[taskType];
                    
                    _ui.ShowNpcDialogue(player, 1599, "Duradel", $"You must slay {taskAmount} {taskName}.");
                }
                break;

            case 3:
                if (player.Choice == 12)
                {
                    player.Choice = 0;
                }
                else if (player.Choice == 1)
                {
                    player.Choice = 0;
                    player.Dialogue = 104;
                    _ui.ShowNpcDialogue(player, 198, "Guildmaster", "I think the Duke of lumbridge has some sort of shield.");
                }
                else if (player.Choice == 2)
                {
                    player.Choice = 0;
                    player.Dialogue = 0;
                }
                else if (player.Choice == 3)
                {
                    player.Choice = 0;
                    player.Dialogue = 0;
                    player.SetCoords(3048, 3203, 0);
                }
                else if (player.ClanGame)
                {
                    player.ClanGame = false;
                }
                else if (player.CookTimer > 0)
                {
                    player.CookAmount = 28;
                }
                else if (player.SmithingTimer > 0)
                {
                    player.SmithingAmount = 28;
                }
                break;
        }
    }
}
