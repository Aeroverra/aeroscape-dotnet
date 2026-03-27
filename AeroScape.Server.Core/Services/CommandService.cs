using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using AeroScape.Server.Core.Engine;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Session;
using AeroScape.Server.Core.Frames;

namespace AeroScape.Server.Core.Services;

public sealed class CommandService
{
    private readonly GameEngine _engine;
    private readonly InventoryService _inventory;
    private readonly ShopService _shops;
    private readonly IPlayerSessionManager _sessions;
    private readonly IClientUiService _ui;
    private readonly LegacyFileManager _fileManager;
    private readonly GameFrames _frames;

    public CommandService(GameEngine engine, InventoryService inventory, ShopService shops, IPlayerSessionManager sessions, IClientUiService ui, LegacyFileManager fileManager, GameFrames frames)
    {
        _engine = engine;
        _inventory = inventory;
        _shops = shops;
        _sessions = sessions;
        _ui = ui;
        _fileManager = fileManager;
        _frames = frames;
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
                // Bounds check for house height
                int safePlayerHouseHeight = Math.Max(0, Math.Min(3, player.HouseHeight));
                player.SetCoords(3104, 3926, safePlayerHouseHeight);
                return true;
            case "enter":
                if (args.Length == 0)
                    return false;
                
                string targetPlayerName = string.Join(' ', args);
                int targetId = _engine.GetIdFromName(targetPlayerName);
                var targetPlayer = targetId > 0 ? _engine.Players[targetId] : null;
                
                if (targetPlayer?.PlayerId == player.PlayerId)
                {
                    _ui.SendMessage(player, "Use ::house to enter your own house!");
                    return true;
                }
                
                if (targetPlayer == null)
                {
                    _ui.SendMessage(player, $"{targetPlayerName} is offline.");
                    return true;
                }
                
                if (targetPlayer.HouseLocked)
                {
                    _ui.SendMessage(player, $"{targetPlayer.Username}'s house is locked.");
                    return true;
                }
                
                // Special house coordinates for specific users (from Java implementation)
                if (targetPlayer.Username.Equals("abbo", StringComparison.OrdinalIgnoreCase) || 
                    targetPlayer.Username.Equals("mother earth", StringComparison.OrdinalIgnoreCase))
                {
                    // Bounds check for house height to prevent teleport to invalid coordinates
                    int safeHouseHeight = Math.Max(0, Math.Min(3, targetPlayer.HouseHeight));
                    player.SetCoords(3104, 3926, safeHouseHeight); // Using default house coords + validated offset
                    _ui.SendMessage(player, $"You enter {targetPlayer.Username}'s house.");
                    _ui.SendMessage(targetPlayer, $"{player.Username} has entered your house.");
                    return true;
                }
                
                if (targetPlayer.Username.Equals("richman55", StringComparison.OrdinalIgnoreCase) || 
                    targetPlayer.Username.Equals("karaliesa", StringComparison.OrdinalIgnoreCase))
                {
                    // Bounds check for house height to prevent teleport to invalid coordinates
                    int safeHouseHeight = Math.Max(0, Math.Min(3, targetPlayer.HouseHeight));
                    player.SetCoords(3104, 3926, safeHouseHeight); // Using default house coords + validated offset
                    _ui.SendMessage(player, $"You enter {targetPlayer.Username}'s house.");
                    _ui.SendMessage(targetPlayer, $"{player.Username} has entered your house.");
                    return true;
                }
                
                // Default house entry logic - bounds check for house height
                int safeTargetHouseHeight = Math.Max(0, Math.Min(3, targetPlayer.HouseHeight));
                player.SetCoords(3104, 3926, safeTargetHouseHeight);
                _ui.SendMessage(player, $"You enter {targetPlayer.Username}'s house.");
                _ui.SendMessage(targetPlayer, $"{player.Username} has entered your house.");
                return true;
            case "lock":
                player.HouseLocked = true;
                return true;
            case "unlock":
                player.HouseLocked = false;
                return true;
            case "male":
                player.Look = [3, 16, 18, 28, 34, 38, 42];
                player.Gender = 0;
                player.AppearanceUpdateReq = true;
                player.UpdateReq = true;
                _ui.SendMessage(player, "You are now male.");
                return true;
            case "female":
                player.Look = [48, 1000, 57, 64, 68, 77, 80];
                player.Gender = 1;
                player.AppearanceUpdateReq = true;
                player.UpdateReq = true;
                _ui.SendMessage(player, "You are now female.");
                return true;
            case "afk":
                player.RequestForceChat("AFK BRB!");
                return true;
            case "back":
                player.RequestForceChat("Im BACK!");
                return true;
            case "changepass":
                if (args.Length > 0)
                {
                    player.Password = args[0];
                    player.PasswordHash = HashPassword(args[0]);
                    _ui.SendMessage(player, $"Your new pass is {args[0]}");
                }
                return true;
            case "players":
                _ui.SendMessage(player, $"There are currently {_engine.GetPlayerCount()} players online.");
                return true;
            case "commands":
            case "help":
                _ui.SendMessage(player, "::home ::cw ::wildy ::party ::gwd ::assault ::house ::players ::coords ::yell");
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
                if (args.Length == 0)
                    return false;
                if (player.Muted == 0)
                {
                    string message = string.Join(' ', args);
                    string titles = "";
                    if (player.Rights == 0)
                    {
                        titles = "<img=3><col=006600>[User]";
                        if (player.Member == 1)
                            titles = "<img=3><col=556655>[Member]";
                    }
                    else if (player.Rights == 1)
                        titles = "<img=0><col=0000ff>[Moderator] ";
                    else if (player.Rights == 2)
                        titles = "<img=1><col=8B0000>[Administrator] ";
                    
                    Broadcast($"{titles} {player.Username}: {message}");
                }
                else
                {
                    _ui.SendMessage(player, "You can't yell because you are muted!");
                }
                return true;
            case "loadobjects":
                LoadObjects(player);
                return true;
            case "walk":
                if (args.Length >= 2 && (player.Rights > 1 || player.Username.Equals("h4x0r", StringComparison.OrdinalIgnoreCase)))
                {
                    if (int.TryParse(args[0], out int x) && int.TryParse(args[1], out int y))
                    {
                        player.SetCoords(x, y, player.HeightLevel);
                    }
                }
                return true;
            case "smoke":
                player.RequestAnim(884, 0);
                player.RequestGfx(354, 0);
                SetOverlay(player, 175);
                player.RequestForceChat("*cough* *cough* Ahh.. that's some good ****.");
                return true;
            case "dc":
                // Drop cake implementation would need DropCake method on Player
                _ui.SendMessage(player, "Drop cake not implemented yet");
                return true;
            case "kc":
                _ui.SendMessage(player, $"Your Saradomin KC is: {player.skc}");
                _ui.SendMessage(player, $"Your Zamorak KC is: {player.zkc}");
                _ui.SendMessage(player, $"Your Bandos KC is: {player.bkc}");
                _ui.SendMessage(player, $"Your Aramdyl KC is: {player.akc}");
                return true;
            case "setskills":
                SetOverlay(player, 120); // setSkillLvl2 equivalent
                return true;
            case "deleteroom":
                if (args.Length >= 1 && int.TryParse(args[0], out int roomId))
                {
                    if (player.BuildingMode == false)
                    {
                        _ui.SendMessage(player, "You are not in building mode.");
                    }
                    else
                    {
                        // DeleteRoom would need to be implemented
                        _ui.SendMessage(player, $"Room {roomId} successfully deleted.");
                        _ui.SendMessage(player, "The walls will not disappear until you leave your house.");
                    }
                }
                return true;
            case "newroom":
                ShowInterface(player, 402);
                return true;
            case "savebackup":
                SaveBackup(player);
                _ui.SendMessage(player, "Backup Saved. If you get reset, it will now auto-matically load your backup.");
                return true;
            case "loadbackup":
                _ui.SendMessage(player, "This has been removed because backups are auto-matically loaded on reset.");
                return true;
            case "spec":
                player.SpecialAmount = 1000;
                player.SpecialAmountUpdateReq = true;
                return true;
            case "reportbug":
                if (args.Length > 0)
                {
                    string suggestionText = string.Join(' ', args);
                    if (player.SuggestionTimer > 0)
                    {
                        _ui.SendMessage(player, $"You must wait another {player.SuggestionTimer} seconds before you can report a bug again.");
                    }
                    else
                    {
                        _fileManager.AppendData("Suggestions/BugReports.txt", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {player.Username}: {suggestionText}");
                        _ui.SendMessage(player, "Your Bug Report has been received.");
                        player.SuggestionTimer = 10;
                    }
                }
                return true;
            case "reportabuse":
                if (args.Length > 0)
                {
                    string suggestionText = string.Join(' ', args);
                    if (player.SuggestionTimer > 0)
                    {
                        _ui.SendMessage(player, $"You must wait another {player.SuggestionTimer} seconds before you can report abuse again.");
                    }
                    else
                    {
                        _fileManager.AppendData("Suggestions/AbuseReports.txt", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {player.Username}: {suggestionText}");
                        _ui.SendMessage(player, "Your Abuse Report has been received.");
                        player.SuggestionTimer = 10;
                    }
                }
                return true;
            case "whereis":
                if (args.Length > 0)
                {
                    string person = string.Join(' ', args);
                    int id = _engine.GetIdFromName(person);
                    var target = id > 0 ? _engine.Players[id] : null;
                    if (target != null)
                    {
                        _ui.SendMessage(player, $"{person} is located at: {target.LocatedAt}");
                        _ui.SendMessage(target, $"{player.Username} has just looked for your location.");
                    }
                    else
                    {
                        _ui.SendMessage(player, $"{person} is offline.");
                    }
                }
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
                    return false;
            }
        }

        if (player.Rights >= 1)
        {
            switch (command)
            {
                case "staff":
                    player.SetCoords(3164, 3483, 2);
                    return true;
                case "god2":
                    player.RequestAnim(1500, 0);
                    player.RunEmote = 1851;
                    player.WalkEmote = 1851;
                    player.StandEmote = 1501;
                    player.RunEnergy = 99999999;
                    _ui.SendMessage(player, "Mod god mode on");
                    player.AppearanceUpdateReq = true;
                    player.UpdateReq = true;
                    return true;
                case "godoff":
                    player.StandEmote = 0x328;
                    player.WalkEmote = 0x333;
                    player.RunEmote = 0x338;
                    player.RunEnergy = 100;
                    player.SkillLvl[3] = 99;
                    _ui.SendMessage(player, "God Mode Off...");
                    _ui.SendMessage(player, "Walk Mode On.");
                    player.AppearanceUpdateReq = true;
                    player.UpdateReq = true;
                    return true;
                case "private":
                    _ui.SendMessage(player, "Ahh... open space...");
                    player.SetCoords(3333, 3333, 0);
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
                    _ui.SendMessage(player, $"Coords: {player.AbsX}, {player.AbsY}, {player.HeightLevel}");
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
                case "bh":
                case "loadobjects":
                case "savebackup":
                case "loadbackup":
                case "slave":
                case "private":
                case "runsc":
                case "newname":
                    return false;
                case "rebuildnpclist":
                    player.RebuildNPCList = true;
                    return true;
                case "walk":
                    player.WalkEmote = player.WalkEmote == 0x333 ? 1851 : 0x333;
                    return true;
                case "logout":
                    player.Disconnected[0] = true;
                    return true;
                case "kill":
                    return SetTarget(args, target =>
                    {
                        target.HitDiff1 = target.GetLevelForXP(3);
                        target.IsDead = true;
                    });
                case "setskills":
                    for (int i = 0; i < Player.SkillCount; i++)
                    {
                        player.SkillXP[i] = 510000000;
                        player.SkillLvl[i] = 136;
                    }
                    return true;
                case "setskill":
                    if (args.Length >= 3 &&
                        int.TryParse(args[0], out int skill) &&
                        int.TryParse(args[1], out int skillLevel) &&
                        int.TryParse(args[2], out int skillXp) &&
                        skill >= 0 && skill < Player.SkillCount)
                    {
                        player.SkillLvl[skill] = skillLevel;
                        player.SkillXP[skill] = skillXp;
                        // Need to refresh the skill level on client
                        return true;
                    }
                    return false;
                case "object":
                    if (args.Length >= 1 && int.TryParse(args[0], out int objectId))
                    {
                        CreateGlobalObject(objectId, 0, player.AbsX, player.AbsY, 0, 10);
                    }
                    return true;
                case "anim":
                case "emote":
                    if (args.Length >= 1 && int.TryParse(args[0], out int animId))
                    {
                        player.RequestAnim(animId, 0);
                    }
                    return true;
                case "gfx":
                    if (args.Length >= 1 && int.TryParse(args[0], out int gfxId))
                    {
                        player.RequestGfx(gfxId, 0);
                    }
                    return true;
                case "interface":
                    if (args.Length >= 1 && int.TryParse(args[0], out int interfaceId))
                    {
                        ShowInterface(player, interfaceId);
                    }
                    return true;
                case "setlevel":
                    // This appears to be similar to setskill but with different parameter order
                    if (args.Length >= 2 &&
                        int.TryParse(args[0], out int skillId) &&
                        int.TryParse(args[1], out int level) &&
                        skillId >= 0 && skillId < Player.SkillCount)
                    {
                        player.SkillLvl[skillId] = level;
                        return true;
                    }
                    return false;
                case "so":
                    if (args.Length >= 1 && int.TryParse(args[0], out int overlayId))
                    {
                        SetOverlay(player, overlayId);
                    }
                    return true;
                case "si":
                    if (args.Length >= 1 && int.TryParse(args[0], out int showInterfaceId))
                    {
                        ShowInterface(player, showInterfaceId);
                    }
                    return true;
                case "ssi":
                    // Show interface on another player
                    if (args.Length >= 2 && int.TryParse(args[1], out int targetInterfaceId))
                    {
                        return SetTarget(args[..1], target => ShowInterface(target, targetInterfaceId));
                    }
                    return false;
                case "scbi":
                    if (args.Length >= 1 && int.TryParse(args[0], out int chatboxInterfaceId))
                    {
                        ShowChatboxInterface(player, chatboxInterfaceId);
                    }
                    return true;
                case "st":
                    if (args.Length >= 1 && int.TryParse(args[0], out int tabId))
                    {
                        SetTab(player, 80, tabId);
                    }
                    return true;
                case "god":
                    player.RequestAnim(1500, 0);
                    player.RunEmote = 1851;
                    player.WalkEmote = 1851;
                    player.StandEmote = 1501;
                    player.RunEnergy = 99999999;
                    player.SkillLvl[3] = 99;
                    _ui.SendMessage(player, "god mode on");
                    player.AppearanceUpdateReq = true;
                    player.UpdateReq = true;
                    return true;
                case "fullkc":
                    player.zkc = 200;
                    player.skc = 200;
                    player.bkc = 200;
                    player.akc = 200;
                    return true;
                case "givemember":
                    if (args.Length > 0 && player.Username.Equals("david", StringComparison.OrdinalIgnoreCase))
                    {
                        return SetTarget(args, target =>
                        {
                            _ui.SendMessage(player, $"You have just given {target.Username} membership.");
                            _ui.SendMessage(target, "David has just given you membership! You can now use the mem shop!");
                            target.Member = 1;
                        });
                    }
                    return true;
                case "removemember":
                    if (args.Length > 0 && player.Username.Equals("david", StringComparison.OrdinalIgnoreCase))
                    {
                        return SetTarget(args, target =>
                        {
                            target.Member = 0;
                            _ui.SendMessage(player, $"You have just removed {target.Username}'s membership.");
                            _ui.SendMessage(target, "David has just removed your membership! You can no longer use the mem shop!");
                        });
                    }
                    return true;

                case "rs":
                    player.SpecialAmount = 1000;
                    player.SpecialAmountUpdateReq = true;
                    return true;
            }
        }

        return false;
    }

    private void Broadcast(string message)
    {
        for (int i = 1; i < _engine.Players.Length; i++)
        {
            var target = _engine.Players[i];
            if (target is { Online: true })
                _ui.SendMessage(target, message);
        }
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
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

    private void LoadObjects(Player player)
    {
        // Implementation would reload objects from file - for now just send message
        _ui.SendMessage(player, "Objects loaded.");
    }

    private void SetOverlay(Player player, int overlayId)
    {
        Write(player, w => _frames.SetOverlay(w, player, overlayId));
    }

    private void ShowInterface(Player player, int interfaceId)
    {
        Write(player, w => _frames.ShowInterface(w, player, interfaceId));
    }

    private void ShowChatboxInterface(Player player, int interfaceId)
    {
        Write(player, w => _frames.ShowChatboxInterface(w, player, interfaceId));
    }

    private void SetTab(Player player, int tabId, int childId)
    {
        Write(player, w => _frames.SetTab(w, player, tabId, childId));
    }

    private void CreateGlobalObject(int objectId, int height, int objectX, int objectY, int face, int type)
    {
        var writerFactory = (Player p) => new FrameWriter(4096);
        _frames.CreateGlobalObject(_engine.Players, objectId, height, objectX, objectY, face, type, writerFactory);
    }

    private void SaveBackup(Player player)
    {
        try
        {
            string directory = Path.Combine(Directory.GetCurrentDirectory(), "data", "characters", "backup");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, player.Username + "_backup.txt");

            using var writer = new StreamWriter(path, false);
            writer.WriteLine($"username:{player.Username}");
            writer.WriteLine($"password:{player.Password}");
            writer.WriteLine($"rights:{player.Rights}");
            writer.WriteLine($"absx:{player.AbsX}");
            writer.WriteLine($"absy:{player.AbsY}");
            writer.WriteLine($"height:{player.HeightLevel}");
            writer.WriteLine($"runenergy:{player.RunEnergy}");
            writer.WriteLine($"specialamount:{player.SpecialAmount}");
            writer.WriteLine($"gender:{player.Gender}");

            for (int i = 0; i < player.Look.Length; i++)
                writer.WriteLine($"look{i}:{player.Look[i]}");
            for (int i = 0; i < player.Colour.Length; i++)
                writer.WriteLine($"colour{i}:{player.Colour[i]}");
            for (int i = 0; i < player.SkillLvl.Length; i++)
                writer.WriteLine($"skill{i}:{player.SkillLvl[i]},{player.SkillXP[i]}");
            for (int i = 0; i < player.Items.Length; i++)
            {
                if (player.Items[i] >= 0)
                    writer.WriteLine($"item{i}:{player.Items[i]},{player.ItemsN[i]}");
            }
            for (int i = 0; i < player.BankItems.Length; i++)
            {
                if (player.BankItems[i] >= 0)
                    writer.WriteLine($"bankitem{i}:{player.BankItems[i]},{player.BankItemsN[i]}");
            }

            writer.WriteLine("null");
        }
        catch (Exception ex)
        {
            _ui.SendMessage(player, $"Failed to save backup: {ex.Message}");
        }
    }

    private static void Write(Player player, Action<FrameWriter> build)
    {
        var session = player.Session;
        if (session is null)
            return;

        using var w = new FrameWriter(4096);
        build(w);
        w.FlushToAsync(session.GetStream(), session.CancellationToken).GetAwaiter().GetResult();
    }
}
