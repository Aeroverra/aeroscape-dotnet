using System;
using AeroScape.Server.Core.Engine;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Services;

public sealed class CommandService
{
    private readonly GameEngine _engine;
    private readonly InventoryService _inventory;
    private readonly ShopService _shops;
    private readonly IPlayerSessionManager _sessions;

    public CommandService(GameEngine engine, InventoryService inventory, ShopService shops, IPlayerSessionManager sessions)
    {
        _engine = engine;
        _inventory = inventory;
        _shops = shops;
        _sessions = sessions;
    }

    public bool Execute(Player player, string command, string[] args, string raw)
    {
        if (player.Jailed && command != "yell")
            return false;

        switch (command)
        {
            case "verifycode":
                if (args.Length > 0 && int.TryParse(args[0], out int code) && code == player.VerificationCode)
                {
                    player.DoneCode = 1;
                    player.SetCoords(3222, 3219, 0);
                }
                return true;
            case "home":
                player.SetCoords(3222, 3219, 0);
                return true;
            case "cw":
                player.SetCoords(2442, 3090, 0);
                return true;
            case "wildy":
            case "pvp":
                player.SetCoords(3243, 3516, 0);
                return true;
            case "party":
                player.SetCoords(3046, 3371, 0);
                return true;
            case "gwd":
                player.SetCoords(2882, 5310, 2);
                return true;
            case "fixgwd":
                if (player.AbsX > 2816 && player.AbsX < 2942 && player.AbsY < 5374 && player.AbsY > 5253)
                    player.SetCoords(player.AbsX, player.AbsY, 2);
                return true;
            case "assault":
                player.SetCoords(2592, 5285, 0);
                return true;
            case "house":
                player.SetCoords(3104, 3926, player.HouseHeight);
                return true;
            case "lock":
                player.HouseLocked = true;
                return true;
            case "unlock":
                player.HouseLocked = false;
                return true;
            case "male":
                player.Gender = 0;
                player.AppearanceUpdateReq = true;
                player.UpdateReq = true;
                return true;
            case "female":
                player.Gender = 1;
                player.AppearanceUpdateReq = true;
                player.UpdateReq = true;
                return true;
            case "afk":
                player.RequestForceChat("AFK BRB!");
                return true;
            case "back":
                player.RequestForceChat("Im BACK!");
                return true;
            case "changepass":
                if (args.Length > 0)
                    player.Password = args[0];
                return true;
            case "players":
                return true;
            case "commands":
            case "help":
                return true;
            case "bank":
                player.InterfaceId = 762;
                return true;
            case "empty":
                Array.Fill(player.Items, -1);
                Array.Fill(player.ItemsN, 0);
                return true;
            case "restoreenergy":
                player.RunEnergy = 100;
                player.RunEnergyUpdateReq = true;
                return true;
            case "restorestats":
                for (int i = 0; i < Player.SkillCount; i++)
                    player.SkillLvl[i] = player.GetLevelForXP(i);
                return true;
            case "modern":
                player.IsAncients = 0;
                player.IsLunar = 0;
                return true;
            case "ancients":
                player.IsAncients = 1;
                player.IsLunar = 0;
                return true;
            case "lunar":
                player.IsLunar = 1;
                player.IsAncients = 0;
                return true;
            case "yell":
                return true;
        }

        if (player.Rights >= 1)
        {
            switch (command)
            {
                case "kick":
                    return SetTarget(args, target => target.Disconnected[0] = true);
                case "mute":
                    return SetTarget(args, target => target.Muted = 1);
                case "unmute":
                    return SetTarget(args, target => target.Muted = 0);
                case "ban":
                    return SetTarget(args, target => target.Banned = 1);
                case "unban":
                    return SetTarget(args, target => target.Banned = 0);
                case "jail":
                    return SetTarget(args, target =>
                    {
                        target.Jailed = true;
                        target.SetCoords(2604, 3105, 0);
                    });
                case "ipmute":
                case "ipban":
                    return true;
            }
        }

        if (player.Rights >= 2)
        {
            switch (command)
            {
                case "tele":
                    if (args.Length >= 3 &&
                        int.TryParse(args[0], out int x) &&
                        int.TryParse(args[1], out int y) &&
                        int.TryParse(args[2], out int h))
                    {
                        player.SetCoords(x, y, h);
                    }
                    return true;
                case "coords":
                    return true;
                case "item":
                    if (args.Length >= 2 &&
                        int.TryParse(args[0], out int itemId) &&
                        int.TryParse(args[1], out int amount))
                    {
                        _inventory.AddItem(player, itemId, amount);
                    }
                    return true;
                case "teleto":
                case "xteleto":
                    return SetTarget(args, target => player.SetCoords(target.AbsX, target.AbsY, target.HeightLevel));
                case "teletome":
                    return SetTarget(args, target => target.SetCoords(player.AbsX, player.AbsY, player.HeightLevel));
                case "npc":
                    if (args.Length >= 1 && int.TryParse(args[0], out int npcType))
                        _engine.SpawnNpc(npcType, player.AbsX + 1, player.AbsY, player.HeightLevel, player.AbsX - 1, player.AbsY - 1, player.AbsX + 1, player.AbsY + 1, true);
                    return true;
                case "pnpc":
                    if (args.Length >= 1 && int.TryParse(args[0], out int morph))
                    {
                        player.NpcType = morph;
                        player.AppearanceUpdateReq = true;
                        player.UpdateReq = true;
                    }
                    return true;
                case "unpc":
                case "unpnpc":
                    player.NpcType = -1;
                    player.AppearanceUpdateReq = true;
                    player.UpdateReq = true;
                    return true;
                case "master":
                    for (int i = 0; i < Player.SkillCount; i++)
                    {
                        player.SkillXP[i] = 14000000;
                        player.SkillLvl[i] = player.GetLevelForXP(i);
                    }
                    return true;
                case "food":
                    _inventory.AddItem(player, 385, 28);
                    return true;
                case "capes2":
                case "hoods":
                case "pouches":
                case "char":
                case "staff":
                case "god":
                case "god2":
                case "godoff":
                case "fullkc":
                case "bh":
                case "rebuildnpclist":
                case "loadobjects":
                case "savebackup":
                case "loadbackup":
                case "object":
                case "so":
                case "si":
                case "ssi":
                case "scbi":
                case "emote":
                case "gfx":
                case "kill":
                case "logout":
                case "walk":
                case "deleteroom":
                case "newroom":
                case "setskill":
                case "setskills":
                case "slave":
                case "private":
                case "runsc":
                case "newname":
                case "givemember":
                case "removemember":
                    return true;
            }
        }

        return false;
    }

    private bool SetTarget(string[] args, Action<Player> action)
    {
        if (args.Length == 0)
            return false;

        int id = _engine.GetIdFromName(string.Join(' ', args));
        if (id <= 0)
            return false;

        var target = _engine.Players[id];
        if (target == null)
            return false;

        action(target);
        return true;
    }
}
