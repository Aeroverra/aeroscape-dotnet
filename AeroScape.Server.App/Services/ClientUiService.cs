using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Frames;
using AeroScape.Server.Core.Services;

namespace AeroScape.Server.App.Services;

public sealed class ClientUiService : IClientUiService
{
    private readonly GameFrames _frames;

    public ClientUiService(GameFrames frames)
    {
        _frames = frames;
    }

    public void SendMessage(Player player, string message)
        => Write(player, w => _frames.SendMessage(w, message));

    public void ShowLongTextInput(Player player, int inputId, string question)
    {
        player.InputId = inputId;
        Write(player, w => _frames.RunScript(w, 109, [question], "s"));
    }

    public void OpenBank(Player player)
    {
        Write(player, w =>
        {
            _frames.SetConfig2(w, 563, 4194304);
            _frames.SetConfig(w, 115, player.WithdrawNote ? 1 : 0);
            _frames.SetConfig(w, 305, player.InsertMode ? 1 : 0);
            _frames.ShowInterface(w, player, 762);
            _frames.SetInventory(w, player, 763);
            _frames.SetBankOptions(w);
            _frames.SetItems(w, -1, 64207, 95, player.BankItems, player.BankItemsN);
            _frames.SetItems(w, -1, 64209, 93, player.Items, player.ItemsN);
            _frames.SetInterfaceConfig(w, 762, 18, true);
            _frames.SetInterfaceConfig(w, 762, 19, true);
            _frames.SetInterfaceConfig(w, 762, 23, true);
            _frames.SetString(w, Player.BankSize.ToString(), 762, 98);
        });
    }

    public void OpenShop(Player player, string title)
    {
        Write(player, w =>
        {
            _frames.ShowInterface(w, player, 620);
            _frames.SetInventory(w, player, 621);
            _frames.SetString(w, "Main Stock", 620, 31);
            _frames.SetString(w, "Closed", 620, 32);
            _frames.SetString(w, "AeroScape", 620, 28);
            _frames.SetString(w, title, 620, 22);
            _frames.SetInterfaceConfig(w, 620, 23, true);
            _frames.SetInterfaceConfig(w, 620, 24, false);
            _frames.SetInterfaceConfig(w, 620, 29, true);
            _frames.SetInterfaceConfig(w, 620, 25, false);
            _frames.SetInterfaceConfig(w, 620, 27, false);
            _frames.SetInterfaceConfig(w, 620, 26, true);
            _frames.SetAccessMask(w, 1278, 24, 620, 0, 40);

            var setShopParams = new object[] { 868, 93 };
            int inventoryHash = 621 << 16;
            int shopHash = (620 << 16) + 24;
            object[] shopParams;
            object[] inventoryParams;
            if (player.PartyShop)
            {
                shopParams = new object[] { "", "", "", "", "", "", "", "", "Value", -1, 0, 4, 10, 31, shopHash };
                inventoryParams = new object[] { "", "", "", "", "", "", "Give 5", "Give 1", "Value", -1, 0, 7, 4, 93, inventoryHash };
            }
            else
            {
                shopParams = new object[] { "", "", "", "", "", "", "Buy 5", "Buy 1", "Value", -1, 0, 4, 10, 31, shopHash };
                inventoryParams = new object[] { "", "", "", "", "", "", "Sell 5", "Sell 1", "Value", -1, 0, 7, 4, 93, inventoryHash };
            }

            _frames.RunScript(w, 25, setShopParams, "vg");
            _frames.RunScript(w, 150, inventoryParams, "IviiiIsssssssss");
            _frames.RunScript(w, 150, shopParams, "IviiiIsssssssss");
            _frames.SetAccessMask(w, 1278, 0, 621, 0, 28);
            _frames.SetItems(w, -1, 64209, 93, player.Items, player.ItemsN);
            _frames.SetItems(w, -1, 64271, 31, player.ShopItems, player.ShopItemsN);
        });
    }

    public void RefreshShop(Player player)
    {
        Write(player, w =>
        {
            _frames.SetItems(w, -1, 64209, 93, player.Items, player.ItemsN);
            _frames.SetItems(w, -1, 64271, 31, player.ShopItems, player.ShopItemsN);
        });
    }

    public void ShowNpcDialogue(Player player, int npcId, string name, string line, int animationId = 9850)
    {
        Write(player, w =>
        {
            _frames.ShowChatboxInterface(w, player, 241);
            _frames.AnimateInterfaceId(w, animationId, 241, 2);
            _frames.SetNPCId(w, npcId, 241, 2);
            _frames.SetString(w, name, 241, 3);
            _frames.SetString(w, line, 241, 4);
        });
    }

    public void ShowOptionDialogue(Player player, string option1, string option2, string option3)
    {
        Write(player, w =>
        {
            _frames.SetString(w, option1, 458, 1);
            _frames.SetString(w, option2, 458, 2);
            _frames.SetString(w, option3, 458, 3);
            _frames.ShowChatboxInterface(w, player, 458);
        });
    }

    public void ShowInterface(Player player, int interfaceId)
        => Write(player, w => _frames.ShowInterface(w, player, interfaceId));

    public void UpdateCastleWarsCounters(Player player)
    {
        Write(player, w =>
        {
            _frames.SetString(w, player.ZamFL.ToString(), 59, 0);
            _frames.SetString(w, player.SaraFL.ToString(), 59, 1);
        });
    }

    public void ResetClanChatList(Player player)
    {
        Write(player, w =>
        {
            _frames.ResetList(w);
            _frames.SetConfig(w, 1083, 0);
        });
    }

    public void SendClanChat(Player recipient, Player sender, string clanName, string message)
        => Write(recipient, w => _frames.SendClanChat(w, sender, sender.Username, clanName, message));

    private static void Write(Player player, Action<FrameWriter> build)
    {
        var session = player.Session;
        if (session is null)
        {
            return;
        }

        using var w = new FrameWriter(4096);
        build(w);
        w.FlushToAsync(session.GetStream(), session.CancellationToken).GetAwaiter().GetResult();
    }
}
