using System.Text;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Util;
using AeroScape.Server.Core.World;
using AeroScape.Server.Network.Frames;

namespace AeroScape.Server.Network.Update;

public sealed class PlayerUpdateWriter
{
    private static readonly int[] OtherEmotes = [0x337, 0x334, 0x335, 0x336];
    private readonly MapDataService _mapData;
    private readonly LegacyAppearanceData _appearanceData;

    public PlayerUpdateWriter(MapDataService mapData)
    {
        _mapData = mapData;
        _appearanceData = new LegacyAppearanceData();
    }

    public void GetNextPlayerMovement(Player p)
    {
        p.MapRegionDidChange = false;
        p.DidTeleport = false;
        p.WalkDir = p.RunDir = -1;
        if (p.TeleportToX != -1 && p.TeleportToY != -1)
        {
            p.MapRegionDidChange = true;
            if (p.MapRegionX != -1 && p.MapRegionY != -1)
            {
                int relX = p.TeleportToX - (p.MapRegionX - 6) * 8;
                int relY = p.TeleportToY - (p.MapRegionY - 6) * 8;
                if (relX >= 16 && relX < 88 && relY >= 16 && relY < 88)
                    p.MapRegionDidChange = false;
            }

            if (p.MapRegionDidChange)
            {
                p.MapRegionX = p.TeleportToX >> 3;
                p.MapRegionY = p.TeleportToY >> 3;
            }

            p.CurrentX = p.TeleportToX - 8 * (p.MapRegionX - 6);
            p.CurrentY = p.TeleportToY - 8 * (p.MapRegionY - 6);
            p.AbsX = p.TeleportToX;
            p.AbsY = p.TeleportToY;
            ResetWalkingQueue(p);
            p.TeleportToX = p.TeleportToY = -1;
            p.DidTeleport = true;
            return;
        }

        p.WalkDir = GetNextWalkingDirection(p);
        if (p.WalkDir == -1)
            return;

        if (p.IsRunning)
            p.RunDir = GetNextWalkingDirection(p);

        if (p.CurrentX < 16 || p.CurrentX >= 88 || p.CurrentY < 16 || p.CurrentY >= 88)
        {
            p.MapRegionDidChange = true;
            p.TeleportToX = p.AbsX;
            p.TeleportToY = p.AbsY;
        }
    }

    public void ResetWalkingQueue(Player p)
    {
        p.WalkingQueueX[0] = p.CurrentX;
        p.WalkingQueueY[0] = p.CurrentY;
        p.WalkingQueue[0] = -1;
        p.WQueueReadPtr = p.WQueueWritePtr = 1;
    }

    public void AddToWalkingQueue(Player p, int x, int y)
    {
        int diffX = x - p.WalkingQueueX[p.WQueueWritePtr - 1];
        int diffY = y - p.WalkingQueueY[p.WQueueWritePtr - 1];
        int max = Math.Max(Math.Abs(diffX), Math.Abs(diffY));

        for (int i = 0; i < max; i++)
        {
            if (diffX < 0) diffX++;
            else if (diffX > 0) diffX--;
            if (diffY < 0) diffY++;
            else if (diffY > 0) diffY--;
            AddStepToWalkingQueue(x - diffX, y - diffY, p);
        }
    }

    public void AddStepToWalkingQueue(int x, int y, Player p)
    {
        int diffX = x - p.WalkingQueueX[p.WQueueWritePtr - 1];
        int diffY = y - p.WalkingQueueY[p.WQueueWritePtr - 1];
        int dir = MiscDirection(diffX, diffY);
        if (p.WQueueWritePtr >= p.WalkingQueueSize)
            return;
        if (dir != -1)
        {
            p.WalkingQueueX[p.WQueueWritePtr] = x;
            p.WalkingQueueY[p.WQueueWritePtr] = y;
            p.WalkingQueue[p.WQueueWritePtr++] = dir;
        }
    }

    public void Update(Player p, Player?[] players, FrameWriter str)
    {
        if (p.Disconnected[0])
            return;

        var newPlayerIds = UpdatePlayer(p, str, players);
        str.WriteBits(11, 2047);
        str.FinishBitAccess();
        AppendPlayerUpdateMasks(p, str);
        for (int i = 0; i < p.PlayerListSize; i++)
        {
            var p2 = p.PlayerList[i];
            if (p2 == null)
                continue;

            if (newPlayerIds[i] == 1)
            {
                bool appearanceFlag = p2.AppearanceUpdateReq;
                bool updateFlag = p2.UpdateReq;
                p2.AppearanceUpdateReq = true;
                p2.UpdateReq = true;
                AppendPlayerUpdateMasks(p2, str);
                p2.AppearanceUpdateReq = appearanceFlag;
                p2.UpdateReq = updateFlag;
            }
            else
            {
                AppendPlayerUpdateMasks(p2, str);
            }
        }
        str.EndFrameVarSizeWord();
    }

    public void ClearUpdateReqs(Player p)
    {
        p.UpdateReq = false;
        p.ChatTextUpdateReq = false;
        p.AppearanceUpdateReq = false;
        p.AnimUpdateReq = false;
        p.GfxUpdateReq = false;
        p.Hit1UpdateReq = false;
        p.Hit2UpdateReq = false;
        p.FaceToUpdateReq = false;
        p.ForceChatUpdateReq = false;
        p.AnimReq = -1;
        p.AnimDelay = 0;
        p.GfxReq = -1;
        p.GfxDelay = 0;
        p.HitDiff1 = 0;
        p.PoisonHit1 = 0;
        p.HitDiff2 = 0;
        p.PoisonHit2 = 0;
    }

    private byte[] UpdatePlayer(Player p, FrameWriter str, Player?[] players)
    {
        var newPlayerIds = new byte[p.PlayerList.Length];

        UpdateThisPlayerMovement(p, str);
        str.WriteBits(8, p.PlayerListSize);
        int size = p.PlayerListSize;
        p.PlayerListSize = 0;
        for (int i = 0; i < size; i++)
        {
            var listed = p.PlayerList[i];
            if (listed == null || !WithinDistance(p, listed) || listed.DidTeleport)
            {
                if (listed != null)
                    p.PlayersInList[listed.PlayerId] = 0;
                str.WriteBits(1, 1);
                str.WriteBits(2, 3);
            }
            else
            {
                UpdatePlayerMovement(listed, str);
                p.PlayerList[p.PlayerListSize++] = listed;
            }
        }

        for (int i = 1; i < players.Length; i++)
        {
            var p2 = players[i];
            if (p2 == null || p2.PlayerId == p.PlayerId || !p2.Online)
                continue;
            if (p.PlayersInList[p2.PlayerId] == 1 || !WithinDistance(p, p2))
                continue;

            newPlayerIds[p.PlayerListSize] = 1;
            AddNewPlayer(p, p2, str);
        }
        return newPlayerIds;
    }

    private void UpdateThisPlayerMovement(Player p, FrameWriter str)
    {
        if (p.MapRegionDidChange)
            SetMapRegion(p, str);

        if (p.DidTeleport)
        {
            Teleport(p, str);
            return;
        }

        if (p.WalkDir == -1)
            NoMovement(p, str);
        else
            UpdateMovement(p, str);
    }

    private void UpdateMovement(Player p, FrameWriter str)
    {
        str.CreateFrameVarSizeWord(216);
        str.InitBitAccess();
        str.WriteBits(1, 1);
        if (p.RunDir == -1)
        {
            str.WriteBits(2, 1);
            str.WriteBits(3, p.WalkDir);
            str.WriteBits(1, p.UpdateReq ? 1 : 0);
        }
        else
        {
            str.WriteBits(2, 2);
            str.WriteBits(3, p.RunDir);
            str.WriteBits(3, p.WalkDir);
            str.WriteBits(1, p.UpdateReq ? 1 : 0);
            if (p.RunEnergy > 0)
            {
                p.RunEnergyUpdateReq = true;
                p.RunEnergy--;
            }
            else
            {
                p.IsRunning = false;
            }
        }
    }

    private static void NoMovement(Player p, FrameWriter str)
    {
        str.CreateFrameVarSizeWord(216);
        str.InitBitAccess();
        str.WriteBits(1, p.UpdateReq ? 1 : 0);
        if (p.UpdateReq)
            str.WriteBits(2, 0);
    }

    private static void Teleport(Player p, FrameWriter str)
    {
        str.CreateFrameVarSizeWord(216);
        str.InitBitAccess();
        str.WriteBits(1, 1);
        str.WriteBits(2, 3);
        str.WriteBits(7, p.CurrentX);
        str.WriteBits(1, 1);
        str.WriteBits(2, p.HeightLevel);
        str.WriteBits(1, p.UpdateReq ? 1 : 0);
        str.WriteBits(7, p.CurrentY);
    }

    private void SetMapRegion(Player p, FrameWriter str)
    {
        str.CreateFrameVarSizeWord(142);
        str.WriteWordA(p.MapRegionX);
        str.WriteWordBigEndianA(p.CurrentY);
        str.WriteWordA(p.CurrentX);
        bool forceSend = true;
        p.RebuildNPCList = true;
        if ((p.MapRegionX / 8 == 48 || p.MapRegionX / 8 == 49) && p.MapRegionY / 8 == 48)
            forceSend = false;
        if (p.MapRegionX / 8 == 48 && p.MapRegionY / 8 == 148)
            forceSend = false;

        for (int xCalc = (p.MapRegionX - 6) / 8; xCalc <= (p.MapRegionX + 6) / 8; xCalc++)
        {
            for (int yCalc = (p.MapRegionY - 6) / 8; yCalc <= (p.MapRegionY + 6) / 8; yCalc++)
            {
                int region = yCalc + (xCalc << 8);
                if (forceSend ||
                    (yCalc != 49 && yCalc != 149 && yCalc != 147 &&
                     xCalc != 50 && (xCalc != 49 || yCalc != 47)))
                {
                    var keys = _mapData.GetMapData(region);
                    if (keys != null)
                    {
                        str.WriteDWord(keys[0]);
                        str.WriteDWord(keys[1]);
                        str.WriteDWord(keys[2]);
                        str.WriteDWord(keys[3]);
                    }
                    else
                    {
                        str.WriteDWord(0);
                        str.WriteDWord(0);
                        str.WriteDWord(0);
                        str.WriteDWord(0);
                    }
                }
            }
        }

        str.WriteByteC(p.HeightLevel);
        str.WriteWord(p.MapRegionY);
        str.EndFrameVarSizeWord();
    }

    private static int GetNextWalkingDirection(Player p)
    {
        if (p.WQueueReadPtr == p.WQueueWritePtr)
            return -1;

        int dir = p.WalkingQueue[p.WQueueReadPtr++];
        p.CurrentX += DirectionDeltaX[dir];
        p.CurrentY += DirectionDeltaY[dir];
        p.AbsX += DirectionDeltaX[dir];
        p.AbsY += DirectionDeltaY[dir];
        return dir;
    }

    private static readonly sbyte[] DirectionDeltaX = [-1, 0, 1, -1, 1, -1, 0, 1];
    private static readonly sbyte[] DirectionDeltaY = [1, 1, 1, 0, 0, -1, -1, -1];

    private static int MiscDirection(int dx, int dy)
    {
        if (dx < 0)
            return dy < 0 ? 5 : dy > 0 ? 0 : 3;
        if (dx > 0)
            return dy < 0 ? 7 : dy > 0 ? 2 : 4;
        return dy < 0 ? 6 : dy > 0 ? 1 : -1;
    }

    private static void UpdatePlayerMovement(Player p, FrameWriter str)
    {
        if (p.WalkDir == -1)
        {
            if (p.UpdateReq)
            {
                str.WriteBits(1, 1);
                str.WriteBits(2, 0);
            }
            else
            {
                str.WriteBits(1, 0);
            }
        }
        else if (p.RunDir == -1)
        {
            str.WriteBits(1, 1);
            str.WriteBits(2, 1);
            str.WriteBits(3, p.WalkDir);
            str.WriteBits(1, p.UpdateReq ? 1 : 0);
        }
        else
        {
            str.WriteBits(1, 1);
            str.WriteBits(2, 2);
            str.WriteBits(3, p.RunDir);
            str.WriteBits(3, p.WalkDir);
            str.WriteBits(1, p.UpdateReq ? 1 : 0);
        }
    }

    private void AppendPlayerUpdateMasks(Player p, FrameWriter str)
    {
        bool b = false;
        if (!p.UpdateReq)
            return;

        int maskData = 0;
        if (b) maskData |= 0x100;
        if (p.Hit2UpdateReq) maskData |= 0x200;
        if (p.FaceToUpdateReq) maskData |= 0x20;
        if (p.ForceChatUpdateReq) maskData |= 0x4;
        if (p.GfxUpdateReq) maskData |= 0x400;
        if (b) maskData |= 0x40;
        if (p.ChatTextUpdateReq) maskData |= 0x8;
        if (p.AnimUpdateReq) maskData |= 0x1;
        if (p.AppearanceUpdateReq) maskData |= 0x80;
        if (p.Hit1UpdateReq) maskData |= 0x2;
        WriteMask(str, maskData);
        if (p.Hit2UpdateReq) AppendHit2(p, str);
        if (p.FaceToUpdateReq) AppendPlayerFaceTo(p, str);
        if (p.ForceChatUpdateReq) AppendPlayerForceChat(p, str);
        if (p.GfxUpdateReq) AppendPlayerGfx(p, str);
        if (p.ChatTextUpdateReq) AppendChatText(p, str);
        if (p.AnimUpdateReq) AppendPlayerAnim(p, str);
        if (p.AppearanceUpdateReq) AppendPlayerAppearance(p, str);
        if (p.Hit1UpdateReq) AppendHit1(p, str);
    }

    private static void WriteMask(FrameWriter str, int maskData)
    {
        if (maskData >= 0x100)
        {
            maskData |= 0x10;
            str.WriteByte(maskData & 0xFF);
            str.WriteByte(maskData >> 8);
        }
        else
        {
            str.WriteByte(maskData);
        }
    }

    private void AppendChatText(Player p, FrameWriter str)
    {
        str.WriteWordA(p.ChatTextEffects);
        str.WriteByteC(p.Rights);
        byte[] plain = Encoding.Latin1.GetBytes(p.ChatText);
        byte[] chatBuf = new byte[256];
        chatBuf[0] = (byte)plain.Length;
        int encodedLength = ChatCodec.EncryptPlayerChat(chatBuf, 0, 1, plain.Length, plain);
        int totalLength = 1 + encodedLength;
        str.WriteByteC(totalLength);
        str.WriteBytes(chatBuf, totalLength, 0);
    }

    private static void AppendHit1(Player p, FrameWriter str)
    {
        str.WriteByteS(p.HitDiff1);
        if (p.PoisonHit1 == 0)
            str.WriteByteS(p.HitDiff1 > 0 ? 1 : 0);
        else
            str.WriteByteS(2);
        int hpRatio = p.SkillLvl[3] * 255 / p.GetLevelForXP(3);
        str.WriteByteS(hpRatio);
    }

    private static void AppendHit2(Player p, FrameWriter str)
    {
        str.WriteByteS(p.HitDiff2);
        if (p.PoisonHit2 == 0)
            str.WriteByteA(p.HitDiff2 > 0 ? 1 : 0);
        else
            str.WriteByteA(2);
    }

    private static void AppendPlayerForceChat(Player p, FrameWriter str) => str.WriteString(p.ForceChat);
    private static void AppendPlayerAnim(Player p, FrameWriter str)
    {
        str.WriteWord(p.AnimReq);
        str.WriteByteS(p.AnimDelay);
    }

    private static void AppendPlayerGfx(Player p, FrameWriter str)
    {
        str.WriteWord(p.GfxReq);
        str.WriteDWordV1(p.GfxDelay);
    }

    private static void AppendPlayerFaceTo(Player p, FrameWriter str) => str.WriteWord(p.FaceToReq);

    private void AppendPlayerAppearance(Player p, FrameWriter str)
    {
        var props = new FrameWriter(128);
        props.WriteByte(p.Gender);
        if ((p.Gender & 0x2) == 2)
        {
            props.WriteByte(0);
            props.WriteByte(0);
        }
        props.WriteByte(p.PkIcon);
        props.WriteByte(p.PrayerIcon);
        if (p.NpcType < 0)
        {
            for (int i = 0; i < 4; i++)
            {
                if (p.Equipment[i] > 0) props.WriteWord(32768 + _appearanceData.GetRealId(p.Equipment[i]));
                else props.WriteByte(0);
            }
            if (p.Equipment[4] > 0) props.WriteWord(32768 + _appearanceData.GetRealId(p.Equipment[4]));
            else props.WriteWord(0x100 + p.Look[2]);
            if (p.Equipment[5] > 0) props.WriteWord(32768 + _appearanceData.GetRealId(p.Equipment[5]));
            else props.WriteByte(0);
            if (!_appearanceData.IsFullbody(p.Equipment[4])) props.WriteWord(0x100 + p.Look[3]);
            else props.WriteByte(0);
            if (p.Equipment[7] > 0) props.WriteWord(32768 + _appearanceData.GetRealId(p.Equipment[7]));
            else props.WriteWord(0x100 + p.Look[5]);
            if (!_appearanceData.IsFullhat(p.Equipment[0]) && !_appearanceData.IsFullmask(p.Equipment[0])) props.WriteWord(0x100 + p.Look[0]);
            else props.WriteByte(0);
            if (p.Equipment[9] > 0) props.WriteWord(32768 + _appearanceData.GetRealId(p.Equipment[9]));
            else props.WriteWord(0x100 + p.Look[4]);
            if (p.Equipment[10] > 0) props.WriteWord(32768 + _appearanceData.GetRealId(p.Equipment[10]));
            else props.WriteWord(0x100 + p.Look[6]);
            if (!_appearanceData.IsFullmask(p.Equipment[0])) props.WriteWord(0x100 + p.Look[1]);
            else props.WriteByte(0);
        }
        else
        {
            props.WriteWord(0xFFFF);
            props.WriteWord(p.NpcType);
        }

        for (int j = 0; j < 5; j++)
            props.WriteByte(p.Colour[j]);
        props.WriteWord(p.StandEmote);
        props.WriteWord(CheckCopter(p, 0));
        props.WriteWord(p.WalkEmote);
        props.WriteWord(CheckCopter(p, 1));
        props.WriteWord(CheckCopter(p, 2));
        props.WriteWord(CheckCopter(p, 3));
        props.WriteWord(p.RunEmote);
        props.WriteQWord(NameUtil.StringToLong(p.Username));
        CalculateCombat(p);
        if (p.Username == "david" || p.Username == "h4x0r")
        {
            props.WriteByte(0);
            p.CombatLevel = 0;
        }
        else
        {
            props.WriteByte(p.CombatLevel);
        }
        props.WriteWord(0);
        str.WriteByte(props.Length);
        str.WriteBytes(props.WrittenSpan.ToArray(), props.Length, 0);
    }

    private static int CheckCopter(Player p, int i) => p.Equipment[3] == 12842 ? 8961 : OtherEmotes[i];

    private static void CalculateCombat(Player p)
    {
        int attack = p.GetLevelForXP(0);
        int defence = p.GetLevelForXP(1);
        int strength = p.GetLevelForXP(2);
        int hp = p.GetLevelForXP(3);
        int prayer = p.GetLevelForXP(5);
        int ranged = p.GetLevelForXP(4);
        int magic = p.GetLevelForXP(6);
        p.CombatLevel = (int)((defence + hp + Math.Floor(prayer / 2.0)) * 0.25) + 1;
        double melee = (attack + strength) * 0.325;
        double ranger = Math.Floor(ranged * 1.5) * 0.325;
        double mage = Math.Floor(magic * 1.5) * 0.325;
        if (p.Username == "david" || p.Username == "David")
        {
            p.CombatLevel = 624;
        }
        else if (melee >= ranger && melee >= mage)
        {
            p.CombatLevel += (int)melee;
        }
        else if (ranger >= melee && ranger >= mage)
        {
            p.CombatLevel += (int)ranger;
        }
        else
        {
            p.CombatLevel += (int)mage;
        }
    }

    private static bool WithinDistance(Player p, Player other)
    {
        if (p.HeightLevel != other.HeightLevel)
            return false;
        int deltaX = other.AbsX - p.AbsX;
        int deltaY = other.AbsY - p.AbsY;
        return deltaX <= 15 && deltaX >= -16 && deltaY <= 15 && deltaY >= -16;
    }

    private static void AddNewPlayer(Player p, Player p2, FrameWriter str)
    {
        p.PlayersInList[p2.PlayerId] = 1;
        p.PlayerList[p.PlayerListSize++] = p2;
        str.WriteBits(11, p2.PlayerId);
        int yPos = p2.AbsY - p.AbsY;
        if (yPos > 15) yPos += 32;
        int xPos = p2.AbsX - p.AbsX;
        if (xPos > 15) xPos += 32;
        str.WriteBits(5, xPos);
        str.WriteBits(1, 1);
        str.WriteBits(3, 1);
        str.WriteBits(1, 1);
        str.WriteBits(5, yPos);
    }
}

internal sealed class LegacyAppearanceData
{
    private readonly Dictionary<int, string> _itemNames = [];
    private readonly Dictionary<int, int> _equipIds = [];

    private static readonly string[] Fullbody =
    [
        "top", "shirt", "Shirt", "blouse", "platebody", "Platebody", "Zamorak d'hide", "Ahrims robetop", "Karils leathertop",
        "brassard", "Robe top", "robetop", "platebody (t)", "platebody (g)",
        "chestplate", "torso", "chainbody", "Varrock armour", "Guthix d'hide", "shirt", "Saradomin d'hide", "Prince tunic", "Wizard robe (g)", "Wizard robe (t)", "Runecrafter robe"
    ];

    private static readonly string[] Fullhat =
    [
        "med helm", "Dharoks helm", "hood", "Initiate helm", "Coif", "Helm of neitiznot"
    ];

    private static readonly string[] Fullmask =
    [
        "full helm", "Slayer helmet", "Veracs helm", "Guthans helm", "Armadyl h",
        "Torags helm", "Karils coif", "full helm (t)", "full helm (g)", "Green h'ween mask", "Red h'ween mask", "Blue h'ween mask", "full helmet"
    ];

    public LegacyAppearanceData()
    {
        string root = FindProjectRoot();
        LoadItems(Path.Combine(root, "legacy-java/server508/data/items/items.cfg"));
        LoadEquipment(Path.Combine(root, "legacy-java/server508/data/items/equipment.dat"));
    }

    public int GetRealId(int item) => _equipIds.TryGetValue(item, out int id) ? id : 0;
    public bool IsFullbody(int itemId) => ContainsAny(GetItemName(itemId), Fullbody, false);
    public bool IsFullhat(int itemId) => ContainsAny(GetItemName(itemId), Fullhat, true);
    public bool IsFullmask(int itemId) => ContainsAny(GetItemName(itemId), Fullmask, true);

    private string GetItemName(int itemId)
    {
        if (itemId == -1)
            return "Unarmed";
        return _itemNames.TryGetValue(itemId, out string? name) ? name : $"Item {itemId}";
    }

    private static bool ContainsAny(string itemName, string[] checks, bool endsWith)
    {
        foreach (string check in checks)
        {
            if (endsWith)
            {
                if (itemName.EndsWith(check, StringComparison.Ordinal))
                    return true;
            }
            else if (itemName.Contains(check, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    private void LoadItems(string path)
    {
        foreach (string line in File.ReadLines(path))
        {
            if (!line.StartsWith("item = ", StringComparison.Ordinal))
                continue;

            string[] parts = line["item = ".Length..].Split('\t');
            if (parts.Length < 2)
                continue;

            if (int.TryParse(parts[0], out int itemId))
                _itemNames[itemId] = parts[1].Replace('_', ' ');
        }
    }

    private void LoadEquipment(string path)
    {
        foreach (string line in File.ReadLines(path))
        {
            int separator = line.IndexOf(':');
            if (separator <= 0)
                continue;
            if (int.TryParse(line[..separator], out int itemId) &&
                int.TryParse(line[(separator + 1)..], out int equipId))
            {
                _equipIds[itemId] = equipId;
            }
        }
    }

    private static string FindProjectRoot()
    {
        string? current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (Directory.Exists(Path.Combine(current, "legacy-java")))
                return current;
            current = Directory.GetParent(current)?.FullName;
        }

        current = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(current))
        {
            if (Directory.Exists(Path.Combine(current, "legacy-java")))
                return current;
            current = Directory.GetParent(current)?.FullName;
        }

        throw new DirectoryNotFoundException("Unable to locate legacy-java data directory for appearance encoding.");
    }
}

internal static class ChatCodec
{
    private static readonly int[] AnIntArray233 = [0, 1024, 2048, 3072, 4096, 5120, 6144, 8192, 9216, 12288, 10240, 11264, 16384, 18432, 17408, 20480, 21504, 22528, 23552, 24576, 25600, 26624, 27648, 28672, 29696, 30720, 31744, 32768, 33792, 34816, 35840, 36864, 536870912, 16777216, 37888, 65536, 38912, 131072, 196608, 33554432, 524288, 1048576, 1572864, 262144, 67108864, 4194304, 134217728, 327680, 8388608, 2097152, 12582912, 13631488, 14680064, 15728640, 100663296, 101187584, 101711872, 101974016, 102760448, 102236160, 40960, 393216, 229376, 117440512, 104857600, 109051904, 201326592, 205520896, 209715200, 213909504, 106954752, 218103808, 226492416, 234881024, 222298112, 224395264, 268435456, 272629760, 276824064, 285212672, 289406976, 223346688, 293601280, 301989888, 318767104, 297795584, 298844160, 310378496, 102498304, 335544320, 299892736, 300941312, 301006848, 300974080, 39936, 301465600, 49152, 1073741824, 369098752, 402653184, 1342177280, 1610612736, 469762048, 1476395008, -2147483648, -1879048192, 352321536, 1543503872, -2013265920, -1610612736, -1342177280, -1073741824, -1543503872, 356515840, -1476395008, -805306368, -536870912, -268435456, 1577058304, -134217728, 360710144, -67108864, 364904448, 51200, 57344, 52224, 301203456, 53248, 54272, 55296, 56320, 301072384, 301073408, 301074432, 301075456, 301076480, 301077504, 301078528, 301079552, 301080576, 301081600, 301082624, 301083648, 301084672, 301085696, 301086720, 301087744, 301088768, 301089792, 301090816, 301091840, 301092864, 301093888, 301094912, 301095936, 301096960, 301097984, 301099008, 301100032, 301101056, 301102080, 301103104, 301104128, 301105152, 301106176, 301107200, 301108224, 301109248, 301110272, 301111296, 301112320, 301113344, 301114368, 301115392, 301116416, 301117440, 301118464, 301119488, 301120512, 301121536, 301122560, 301123584, 301124608, 301125632, 301126656, 301127680, 301128704, 301129728, 301130752, 301131776, 301132800, 301133824, 301134848, 301135872, 301136896, 301137920, 301138944, 301139968, 301140992, 301142016, 301143040, 301144064, 301145088, 301146112, 301147136, 301148160, 301149184, 301150208, 301151232, 301152256, 301153280, 301154304, 301155328, 301156352, 301157376, 301158400, 301159424, 301160448, 301161472, 301162496, 301163520, 301164544, 301165568, 301166592, 301167616, 301168640, 301169664, 301170688, 301171712, 301172736, 301173760, 301174784, 301175808, 301176832, 301177856, 301178880, 301179904, 301180928, 301181952, 301182976, 301184000, 301185024, 301186048, 301187072, 301188096, 301189120, 301190144, 301191168, 301193216, 301195264, 301194240, 301197312, 301198336, 301199360, 301201408, 301202432];
    private static readonly byte[] AByteArray235 = [22, 22, 22, 22, 22, 22, 21, 22, 22, 20, 22, 22, 22, 21, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 3, 8, 22, 16, 22, 16, 17, 7, 13, 13, 13, 16, 7, 10, 6, 16, 10, 11, 12, 12, 12, 12, 13, 13, 14, 14, 11, 14, 19, 15, 17, 8, 11, 9, 10, 10, 10, 10, 11, 10, 9, 7, 12, 11, 10, 10, 9, 10, 10, 12, 10, 9, 8, 12, 12, 9, 14, 8, 12, 17, 16, 17, 22, 13, 21, 4, 7, 6, 5, 3, 6, 6, 5, 4, 10, 7, 5, 6, 4, 4, 6, 10, 5, 4, 4, 5, 7, 6, 10, 6, 10, 22, 19, 22, 14, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 21, 22, 21, 22, 22, 22, 21, 22, 22];

    public static int EncryptPlayerChat(byte[] output, int inputOffset, int outputOffset, int inputLength, byte[] input)
    {
        inputLength += inputOffset;
        int carry = 0;
        int bitPos = outputOffset << 3;
        for (; inputLength > inputOffset; inputOffset++)
        {
            int index = input[inputOffset] & 0xff;
            int packed = AnIntArray233[index];
            int bitCount = AByteArray235[index];
            int bytePos = bitPos >> 3;
            int remaining = bitPos & 0x7;
            carry &= -remaining >> 31;
            bitPos += bitCount;
            int endByte = ((remaining + bitCount - 1) >> 3) + bytePos;
            remaining += 24;
            output[bytePos] = (byte)(carry |= packed >> remaining);
            if (endByte > bytePos)
            {
                bytePos++;
                remaining -= 8;
                output[bytePos] = (byte)(carry = packed >> remaining);
                if (endByte > bytePos)
                {
                    bytePos++;
                    remaining -= 8;
                    output[bytePos] = (byte)(carry = packed >> remaining);
                    if (endByte > bytePos)
                    {
                        remaining -= 8;
                        bytePos++;
                        output[bytePos] = (byte)(carry = packed >> remaining);
                        if (bytePos < endByte)
                        {
                            remaining -= 8;
                            bytePos++;
                            output[bytePos] = (byte)(carry = packed << -remaining);
                        }
                    }
                }
            }
        }
        return ((7 + bitPos) >> 3) - outputOffset;
    }
}
