using AeroScape.Server.Core.Combat;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Items;
using AeroScape.Server.Core.Util;
using AeroScape.Server.Core.World;

namespace AeroScape.Server.Core.Frames;

/// <summary>
/// Port of DavidScape/io/Frames.java using exact legacy 508 packet writes.
/// </summary>
public sealed class GameFrames
{
    private static readonly int[] ChatEncodeValues =
    [
        0, 1024, 2048, 3072, 4096, 5120, 6144, 8192, 9216, 12288, 10240, 11264,
        16384, 18432, 17408, 20480, 21504, 22528, 23552, 24576, 25600, 26624,
        27648, 28672, 29696, 30720, 31744, 32768, 33792, 34816, 35840, 36864,
        536870912, 16777216, 37888, 65536, 38912, 131072, 196608, 33554432,
        524288, 1048576, 1572864, 262144, 67108864, 4194304, 134217728, 327680,
        8388608, 2097152, 12582912, 13631488, 14680064, 15728640, 100663296,
        101187584, 101711872, 101974016, 102760448, 102236160, 40960, 393216,
        229376, 117440512, 104857600, 109051904, 201326592, 205520896, 209715200,
        213909504, 106954752, 218103808, 226492416, 234881024, 222298112,
        224395264, 268435456, 272629760, 276824064, 285212672, 289406976,
        223346688, 293601280, 301989888, 318767104, 297795584, 298844160,
        310378496, 102498304, 335544320, 299892736, 300941312, 301006848,
        300974080, 39936, 301465600, 49152, 1073741824, 369098752, 402653184,
        1342177280, 1610612736, 469762048, 1476395008, -2147483648, -1879048192,
        352321536, 1543503872, -2013265920, -1610612736, -1342177280,
        -1073741824, -1543503872, 356515840, -1476395008, -805306368, -536870912,
        -268435456, 1577058304, -134217728, 360710144, -67108864, 364904448,
        51200, 57344, 52224, 301203456, 53248, 54272, 55296, 56320, 301072384,
        301073408, 301074432, 301075456, 301076480, 301077504, 301078528,
        301079552, 301080576, 301081600, 301082624, 301083648, 301084672,
        301085696, 301086720, 301087744, 301088768, 301089792, 301090816,
        301091840, 301092864, 301093888, 301094912, 301095936, 301096960,
        301097984, 301099008, 301100032, 301101056, 301102080, 301103104,
        301104128, 301105152, 301106176, 301107200, 301108224, 301109248,
        301110272, 301111296, 301112320, 301113344, 301114368, 301115392,
        301116416, 301117440, 301118464, 301119488, 301120512, 301121536,
        301122560, 301123584, 301124608, 301125632, 301126656, 301127680,
        301128704, 301129728, 301130752, 301131776, 301132800, 301133824,
        301134848, 301135872, 301136896, 301137920, 301138944, 301139968,
        301140992, 301142016, 301143040, 301144064, 301145088, 301146112,
        301147136, 301148160, 301149184, 301150208, 301151232, 301152256,
        301153280, 301154304, 301155328, 301156352, 301157376, 301158400,
        301159424, 301160448, 301161472, 301162496, 301163520, 301164544,
        301165568, 301166592, 301167616, 301168640, 301169664, 301170688,
        301171712, 301172736, 301173760, 301174784, 301175808, 301176832,
        301177856, 301178880, 301179904, 301180928, 301181952, 301182976,
        301184000, 301185024, 301186048, 301187072, 301188096, 301189120,
        301190144, 301191168, 301193216, 301195264, 301194240, 301197312,
        301198336, 301199360, 301201408, 301202432
    ];

    private static readonly byte[] ChatEncodeSizes =
    [
        22, 22, 22, 22, 22, 22, 21, 22, 22, 20, 22, 22, 22, 21, 22, 22, 22, 22,
        22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 3, 8, 22, 16, 22,
        16, 17, 7, 13, 13, 13, 16, 7, 10, 6, 16, 10, 11, 12, 12, 12, 12, 13, 13,
        14, 14, 11, 14, 19, 15, 17, 8, 11, 9, 10, 10, 10, 10, 11, 10, 9, 7, 12,
        11, 10, 10, 9, 10, 10, 12, 10, 9, 8, 12, 12, 9, 14, 8, 12, 17, 16, 17,
        22, 13, 21, 4, 7, 6, 5, 3, 6, 6, 5, 4, 10, 7, 5, 6, 4, 4, 6, 10, 5, 4, 4,
        5, 7, 6, 10, 6, 10, 22, 19, 22, 14, 22, 22, 22, 22, 22, 22, 22, 22, 22,
        22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22,
        22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22,
        22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22,
        22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22,
        22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22,
        22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22,
        22, 22, 22, 21, 22, 21, 22, 22, 22, 21, 22, 22
    ];

    private static int _messageCounter = 1;
    private readonly MapDataService _mapData;
    private readonly ItemDefinitionLoader _items;

    public GameFrames(MapDataService mapData, ItemDefinitionLoader items)
    {
        _mapData = mapData;
        _items = items;
    }

    public void CreateObject(FrameWriter w, Player p, int objectId, int objectX, int objectY, int face, int type)
    {
        SendCoords(w, p, objectX - ((p.MapRegionX - 6) * 8), objectY - ((p.MapRegionY - 6) * 8));
        int ot = (type << 2) + (face & 3);
        w.CreateFrame(30);
        w.WriteWordBigEndian(objectId);
        w.WriteByteA(0);
        w.WriteByteC(ot);
    }

    public void CreateObject(FrameWriter w, Player p, int objectId, int height, int objectX, int objectY, int face, int type)
        => CreateObject(w, p, objectId, objectX, objectY, face, type);

    public void PlaySound(FrameWriter w, int soundId, int j, int delay)
    {
        w.CreateFrame(119);
        w.WriteWord(soundId);
        w.WriteByte(j);
        w.WriteWord(delay);
    }

    public void SetLoot(FrameWriter w, Player p)
    {
        SetConfig(w, 1083, 1);
        SendMessage(w, "LootShare is now on.");
    }

    public void SendClanChat(FrameWriter w, Player senderRights, string from, string clanName, string message)
    {
        int id = _messageCounter;
        if (id > 16000000)
        {
            id = 1;
            _messageCounter = 1;
        }

        w.CreateFrameVarSize(229);
        w.WriteQWord(NameUtil.StringToLong(from));
        w.WriteByte(1);
        w.WriteQWord(NameUtil.StringToLong(clanName));
        w.WriteRShort(1);

        byte[] bytes = new byte[message.Length + 1];
        bytes[0] = (byte)message.Length;
        EncryptPlayerChat(bytes, 0, 1, message.Length, message);

        w.WriteBytes(
            [
                (byte)((id << 16) & 0xFF),
                (byte)((id << 8) & 0xFF),
                (byte)(id & 0xFF)
            ],
            3,
            0);

        _messageCounter++;
        w.WriteByte(senderRights.Rights);
        w.WriteBytes(bytes, bytes.Length, 0);
        w.EndFrameVarSize();
    }

    public void Test(FrameWriter w)
    {
        w.CreateFrame(313);
        w.WriteByteC(1);
        w.WriteByteC(2);
        w.WriteByteC(3);
        w.WriteString("HELLO");
    }

    public void RestoreTabs(FrameWriter w, Player p) => ShowTabs(w, p);

    public void ResetList(FrameWriter w)
    {
        w.CreateFrameVarSizeWord(82);
        w.WriteQWord(0);
        w.EndFrameVarSizeWord();
    }

    public void Packet190(FrameWriter w, int id)
    {
        w.CreateFrame(190);
        w.WriteWord(id);
    }

    public void ItemOnInterface(FrameWriter w, int interfaceId, int child, int itemSize, int itemId)
    {
        int inter = (interfaceId * 65536) + child;
        w.CreateFrame(35);
        w.WriteDWordV2(inter);
        w.WriteDWordBigEndian(itemSize);
        w.WriteWordBigEndianA(itemId);
    }

    public void ConnectToFServer(FrameWriter w)
    {
        w.CreateFrame(115);
        w.WriteByte(2);
    }

    public void SetAccessMask(FrameWriter w, int set, int window, int inter, int off, int len)
    {
        w.CreateFrame(223);
        w.WriteWord(len);
        w.WriteWordBigEndianA(off);
        w.WriteWordBigEndian(window);
        w.WriteWordBigEndian(inter);
        w.WriteWordBigEndian(set);
        w.WriteWordBigEndian(0);
    }

    public void RestoreInventory(FrameWriter w, Player p)
    {
        SetInterface(w, 1, p.UsingHd ? 746 : 548, 71, 56);
        ShowTabs(w, p);
    }

    public void RestoreInventory2(FrameWriter w, Player p) => RestoreInventory(w, p);

    public void SendSentPrivateMessage(FrameWriter w, long name, string message)
    {
        byte[] bytes = new byte[message.Length];
        EncryptPlayerChat(bytes, 0, 0, message.Length, message);
        w.CreateFrameVarSize(89);
        w.WriteQWord(name);
        w.WriteByte(message.Length);
        w.WriteBytes(bytes, bytes.Length, 0);
        w.EndFrameVarSize();
    }

    public void SendReceivedPrivateMessage(FrameWriter w, long name, int rights, string message)
    {
        int id = _messageCounter++;
        if (id > 16000000)
        {
            id = 1;
        }

        byte[] bytes = new byte[message.Length + 1];
        bytes[0] = (byte)message.Length;
        EncryptPlayerChat(bytes, 0, 1, message.Length, message);

        w.CreateFrameVarSize(178);
        w.WriteQWord(name);
        w.WriteWord(1);
        w.WriteByte((id << 16) & 0xFF);
        w.WriteByte((id << 8) & 0xFF);
        w.WriteByte(id & 0xFF);
        w.WriteByte(rights);
        w.WriteBytes(bytes, bytes.Length, 0);
        w.EndFrameVarSize();
    }

    public void SendFriend(FrameWriter w, long name, int world)
    {
        w.CreateFrameVarSize(154);
        w.WriteQWord(name);
        w.WriteWord(world);
        w.WriteByte(1);
        if (world != 0)
        {
            if (world == 66)
            {
                w.WriteString(NameUtil.LongToString(name).Equals("david", StringComparison.OrdinalIgnoreCase) ? "OWNER" : "Online");
            }
            else
            {
                w.WriteString("DavidScape " + world);
            }
        }
        w.EndFrameVarSize();
    }

    public void CreateLocalObject(FrameWriter w, int objectId, int objectX, int objectY, int face, int type)
    {
        SendCoords(w, objectX, objectY);
        int ot = (type << 2) + (face & 3);
        w.CreateFrame(30);
        w.WriteWordBigEndian(objectId);
        w.WriteByteA(0);
        w.WriteByteC(ot);
    }

    public void DeleteLocalObject(FrameWriter w, int objectX, int objectY, int type)
        => CreateLocalObject(w, 6951, objectX, objectY, 1, type);

    public void TeleportOnMapdata(FrameWriter w, Player p, int height, int x, int y)
    {
        w.CreateFrame(57);
        w.WriteByteS(height);
        w.WriteByteA(y);
        w.WriteByteA(x);
        p.AbsX += x - p.CurrentX;
        p.AbsY += y - p.CurrentY;
        p.CurrentX = x;
        p.CurrentY = y;
    }

    public void SendMapRegion2(FrameWriter w, Player p, int[][][] xPallete, int[][][] yPallete, int[][][] zPallete)
    {
        w.CreateFrameVarSizeWord(173);
        w.WriteByteA(p.HeightLevel);
        w.WriteWord(p.MapRegionY);
        w.WriteWordA(p.CurrentX);
        w.InitBitAccess();
        for (int height = 0; height < 4; height++)
        {
            for (int xCalc = 0; xCalc < 13; xCalc++)
            {
                for (int yCalc = 0; yCalc < 13; yCalc++)
                {
                    if (zPallete[height][xCalc][yCalc] != -1 && xPallete[height][xCalc][yCalc] != -1 && yPallete[height][xCalc][yCalc] != -1)
                    {
                        int x = xPallete[height][xCalc][yCalc];
                        int y = yPallete[height][xCalc][yCalc];
                        int z = zPallete[height][xCalc][yCalc];
                        w.WriteBits(1, 1);
                        w.WriteBits(26, (z << 24) | (x << 14) | (y << 3));
                    }
                    else
                    {
                        w.WriteBits(1, 0);
                    }
                }
            }
        }

        w.FinishBitAccess();
        int[] sent = new int[4 * 13 * 13];
        int sentIndex = 0;
        for (int height = 0; height < 4; height++)
        {
            for (int xCalc = 0; xCalc < 13; xCalc++)
            {
                for (int yCalc = 0; yCalc < 13; yCalc++)
                {
                    if (zPallete[height][xCalc][yCalc] == -1 || xPallete[height][xCalc][yCalc] == -1 || yPallete[height][xCalc][yCalc] == -1)
                    {
                        continue;
                    }

                    int x = xPallete[height][xCalc][yCalc] / 8;
                    int y = yPallete[height][xCalc][yCalc] / 8;
                    int region = y + (x << 8);
                    bool alreadySent = false;
                    for (int i = 0; i < sentIndex; i++)
                    {
                        if (sent[i] == region)
                        {
                            alreadySent = true;
                            break;
                        }
                    }

                    if (alreadySent)
                    {
                        continue;
                    }

                    sent[sentIndex++] = region;
                    int[] mapData = _mapData.GetMapData(region) ?? [0, 0, 0, 0];
                    w.WriteDWordBigEndian(mapData[0]);
                    w.WriteDWordBigEndian(mapData[1]);
                    w.WriteDWordBigEndian(mapData[2]);
                    w.WriteDWordBigEndian(mapData[3]);
                }
            }
        }

        w.WriteWordA(p.CurrentY);
        w.WriteWordA(p.MapRegionX);
        w.EndFrameVarSizeWord();
    }

    public void SendIgnores(FrameWriter w, IEnumerable<long> ignores)
    {
        w.CreateFrameVarSizeWord(240);
        foreach (long ignore in ignores)
        {
            w.WriteQWord(ignore);
        }
        w.EndFrameVarSizeWord();
    }

    public void CreateGlobalObject(IEnumerable<Player?> players, int objectId, int height, int objectX, int objectY, int face, int type, Func<Player, FrameWriter> writerFactory)
    {
        foreach (Player? p in players)
        {
            if (p == null)
            {
                continue;
            }

            CreateObject(writerFactory(p), p, objectId, height, objectX, objectY, face, type);
        }
    }

    public void SendPlayerCoords(FrameWriter w, int x, int y)
    {
        w.CreateFrame(218);
        w.WriteByteA(x);
        w.WriteByte(y);
    }

    public int GetDistance(int coordX1, int coordY1, int coordX2, int coordY2)
    {
        int deltaX = coordX2 - coordX1;
        int deltaY = coordY2 - coordY1;
        return (int)Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaY, 2));
    }

    public void SetWindowPane(FrameWriter w, int set)
    {
        w.CreateFrame(239);
        w.WriteWord(set);
        w.WriteByteA(0);
    }

    public void Logout(FrameWriter w, Player p)
    {
        w.CreateFrame(104);
        p.Disconnected[0] = true;
        p.Disconnected[1] = true;
    }

    public void SetInterface(FrameWriter w, int showId, int windowId, int interfaceId, int childId)
    {
        w.CreateFrame(93);
        w.WriteWord(childId);
        w.WriteByteA(showId);
        w.WriteWord(windowId);
        w.WriteWord(interfaceId);
    }

    public void SetPlayerOption(FrameWriter w, string option, int slot)
    {
        w.CreateFrameVarSize(252);
        w.WriteByteC(1);
        w.WriteString(option);
        w.WriteByteC(slot);
        w.EndFrameVarSize();
    }

    public void SetNPCId(FrameWriter w, int npcId, int interfaceId, int childId)
    {
        w.CreateFrame(6);
        w.WriteWordBigEndian(interfaceId);
        w.WriteWordBigEndian(childId);
        w.WriteWordBigEndian(npcId);
    }

    public void AnimateInterfaceId(FrameWriter w, int emoteId, int interfaceId, int childId)
    {
        w.CreateFrame(245);
        w.WriteWordBigEndian(interfaceId);
        w.WriteWordBigEndian(childId);
        w.WriteWord(emoteId);
    }

    public void SetConfig(FrameWriter w, int id, int set)
    {
        if (set < 128)
        {
            SetConfig1(w, id, set);
        }
        else
        {
            SetConfig2(w, id, set);
        }
    }

    public void SetConfig1(FrameWriter w, int id, int set)
    {
        w.CreateFrame(100);
        w.WriteWordA(id);
        w.WriteByteA(set);
    }

    public void SetConfig2(FrameWriter w, int id, int set)
    {
        w.CreateFrame(161);
        w.WriteWord(id);
        w.WriteDWordV1(set);
    }

    public void CreateProjectile(FrameWriter w, Player p, int casterY, int casterX, int offsetY, int offsetX, int angle, int speed, int gfxMoving, int startHeight, int endHeight, int lockon)
    {
        SendCoords(w, p, (casterX - ((p.MapRegionX - 6) * 8)) - 3, (casterY - ((p.MapRegionY - 6) * 8)) - 2);
        w.CreateFrame(112);
        w.WriteByte(angle);
        w.WriteByte(offsetX);
        w.WriteByte(offsetY);
        w.WriteRShort(lockon);
        w.WriteWord(gfxMoving);
        w.WriteByte(startHeight);
        w.WriteByte(endHeight);
        w.WriteWord(51);
        w.WriteWord(speed);
        w.WriteByte(16);
        w.WriteByte(64);
    }

    public void CreateGlobalProjectile(IEnumerable<Player?> players, int casterY, int casterX, int offsetY, int offsetX, int gfxMoving, int startHeight, int endHeight, int speed, int atkIndex, Func<Player, FrameWriter> writerFactory)
    {
        foreach (Player? p in players)
        {
            if (p == null || p.Disconnected[0])
            {
                continue;
            }

            CreateProjectile(writerFactory(p), p, casterY, casterX, offsetY, offsetX, 50, speed, gfxMoving, startHeight, endHeight, atkIndex);
        }
    }

    public void RunScript(FrameWriter w, int id, object[] args, string valstring)
    {
        if (valstring.Length != args.Length)
        {
            throw new ArgumentException("Argument array size mismatch", nameof(args));
        }

        w.CreateFrameVarSizeWord(152);
        w.WriteString(valstring);
        int j = 0;
        for (int i = valstring.Length - 1; i >= 0; i--)
        {
            if (valstring[i] == 's')
            {
                w.WriteString((string)args[j]);
            }
            else
            {
                w.WriteDWord((int)args[j]);
            }
            j++;
        }
        w.WriteDWord(id);
        w.EndFrameVarSize();
    }

    public void SetBankOptions(FrameWriter w)
    {
        w.CreateFrame(223);
        w.WriteWord(496);
        w.WriteWordBigEndianA(0);
        w.WriteWordBigEndian(73);
        w.WriteWordBigEndian(762);
        w.WriteWordBigEndian(1278);
        w.WriteWordBigEndian(20);
        w.CreateFrame(223);
        w.WriteWord(27);
        w.WriteWordBigEndianA(0);
        w.WriteWordBigEndian(0);
        w.WriteWordBigEndian(763);
        w.WriteWordBigEndian(1150);
        w.WriteWordBigEndian(18);
    }

    public void SetEnergy(FrameWriter w, Player p)
    {
        w.CreateFrame(99);
        w.WriteByte(p.RunEnergy);
    }

    public void SetTab(FrameWriter w, Player p, int tabId, int childId)
    {
        if (!p.UsingHd)
        {
            SetInterface(w, 1, childId == 137 ? 752 : 548, tabId, childId);
        }
        else
        {
            SetInterface(w, 1, 746, tabId, childId);
        }
    }

    public void SetOverlay(FrameWriter w, Player p, int childId)
        => SetInterface(w, 1, p.UsingHd ? 746 : 548, 5, childId);

    public bool RemoveEquipment(FrameWriter w, Player p, int itemId, int index)
    {
        if (itemId <= 0 || index < 0 || index > 12)
        {
            return false;
        }

        if (!AddInventoryItem(p, p.Equipment[index], p.EquipmentN[index]))
        {
            SendMessage(w, "Not enough space in your inventory.");
            return false;
        }

        p.Equipment[index] = -1;
        p.EquipmentN[index] = -1;
        w.CreateFrameVarSizeWord(135);
        w.WriteByte(1);
        w.WriteByte(131);
        w.WriteByte(0);
        w.WriteByte(28);
        w.WriteWord(28);
        w.WriteByte(index);
        w.WriteWord(0);
        w.WriteByte(0);
        w.EndFrameVarSizeWord();
        p.AppearanceUpdateReq = true;
        p.UpdateReq = true;
        p.CalculateEquipmentBonus();
        SetWeaponTab(w, p);
        return true;
    }

    public void RemoveOverlay(FrameWriter w, Player p)
        => SetInterface(w, 1, p.UsingHd ? 746 : 548, 5, 56);

    public void ShowInterface(FrameWriter w, Player p, int childId)
    {
        if (!p.UsingHd)
        {
            SetInterface(w, 0, 548, 8, childId);
        }
        else
        {
            SetInterface(w, 0, 746, 3, childId);
        }
        p.InterfaceId = childId;
    }

    public void RemoveShownInterface(FrameWriter w, Player p)
    {
        if (!p.UsingHd)
        {
            SetInterface(w, 1, 548, 8, 56);
        }
        else
        {
            SetInterface(w, 1, 746, 3, 56);
        }
        p.InterfaceId = -1;
    }

    public void ShowChatboxInterface(FrameWriter w, Player p, int childId)
    {
        SetInterface(w, 0, 752, 12, childId);
        p.ChatboxInterfaceId = childId;
    }

    public void RemoveChatboxInterface(FrameWriter w, Player p)
    {
        SetConfig(w, 334, 1);
        w.CreateFrame(246);
        w.WriteWord(752);
        w.WriteWord(12);
        p.ChatboxInterfaceId = -1;
    }

    public void SetInventory(FrameWriter w, Player p, int childId)
        => SetInterface(w, 0, p.UsingHd ? 746 : 548, 71, childId);

    public void ShowTabs(FrameWriter w, Player p)
    {
        int mainInterface = p.UsingHd ? 746 : 548;
        for (int b = 16; b <= 21; b++)
        {
            SetInterfaceConfig(w, mainInterface, b, false);
        }
        for (int a = 32; a <= 38; a++)
        {
            SetInterfaceConfig(w, mainInterface, a, false);
        }
        SetInterfaceConfig(w, mainInterface, 14, false);
        SetInterfaceConfig(w, mainInterface, 31, false);
        SetInterfaceConfig(w, mainInterface, 63, false);
        SetInterfaceConfig(w, mainInterface, 72, false);
    }

    public void SetInterfaces(FrameWriter w, Player p)
    {
        for (int i = 0; i < 137; i++)
        {
            SetString(w, "!~_-_~_-_~!", 274, 13 + i);
        }
        SetString(w, "DavidScape 508", 274, 5);
        SetString(w, "Quest and Teles:", 274, 6);
        SetString(w, "Dragon Slayer Quest", 274, 7);
        SetString(w, "PVP", 274, 8);
        SetString(w, "GWD", 274, 9);
        SetString(w, "Home", 274, 10);
        SetString(w, "Help Desk", 274, 11);
        SetString(w, "Staff Zone", 274, 12);
        SetString(w, "Barbarian Assault", 274, 13);
        SetString(w, "Barrows", 274, 14);
        SetString(w, "Membership/Donate", 274, 16);
        SetString(w, "Member Area", 274, 17);
        SetString(w, "Newest Client", 274, 18);
        if (!p.UsingHd)
        {
            SetTab(w, p, 6, 745);
            SetTab(w, p, 11, 751);
            SetTab(w, p, 68, 752);
            SetTab(w, p, 64, 748);
            SetTab(w, p, 65, 749);
            SetTab(w, p, 66, 750);
            SetTab(w, p, 67, 747);
            SetTab(w, p, 8, 137);
            SetTab(w, p, 73, 92);
            SetTab(w, p, 74, 320);
            SetTab(w, p, 75, 274);
            SetTab(w, p, 76, 149);
            SetTab(w, p, 77, 387);
            SetTab(w, p, 78, 271);
            p.IsAncients = p.Equipment[CombatConstants.SlotWeapon] == 4675 ? 1 : 0;
            SetInterface(w, 1, 548, 79, p.IsAncients == 1 ? 193 : 192);
            SetTab(w, p, 81, 550);
            SetTab(w, p, 82, 551);
            SetTab(w, p, 83, 589);
            SetTab(w, p, 84, 261);
            SetTab(w, p, 85, 464);
            SetTab(w, p, 86, 187);
            SetTab(w, p, 87, 182);
        }
        else
        {
            SetInterface(w, 1, 549, 0, 746);
            SetInterface(w, 1, 752, 8, 137);
            SetInterface(w, 1, 746, 87, 92);
            SetInterface(w, 1, 746, 88, 320);
            SetInterface(w, 1, 746, 89, 274);
            SetInterface(w, 1, 746, 90, 149);
            SetInterface(w, 1, 746, 91, 387);
            SetInterface(w, 1, 746, 92, 271);
            p.IsAncients = p.Equipment[CombatConstants.SlotWeapon] == 4675 ? 1 : 0;
            SetInterface(w, 1, 746, 93, p.IsAncients == 1 ? 193 : 192);
            SetInterface(w, 1, 746, 95, 550);
            SetInterface(w, 1, 746, 96, 551);
            SetInterface(w, 1, 746, 97, 589);
            SetInterface(w, 1, 746, 98, 261);
            SetInterface(w, 1, 746, 99, 464);
            SetInterface(w, 1, 746, 65, 752);
            SetInterface(w, 1, 746, 18, 751);
            SetInterface(w, 1, 746, 13, 748);
            SetInterface(w, 1, 746, 14, 749);
            SetInterface(w, 1, 746, 15, 750);
            SetInterface(w, 1, 746, 12, 747);
            SetInterface(w, 1, 746, 100, 187);
            SetInterface(w, 1, 746, 101, 182);
        }

        SetWeaponTab(w, p);
    }

    public void SetConfigs(FrameWriter w, Player p)
    {
        SetConfig(w, 1160, -1);
        SetConfig(w, 173, 0);
        SetConfig(w, 313, -1);
        SetConfig(w, 465, -1);
        SetConfig(w, 802, -1);
        SetConfig(w, 1085, 249852);
    }

    public void SetWeaponTab(FrameWriter w, Player p)
    {
        int weaponId = p.Equipment[CombatConstants.SlotWeapon];
        string weapon = _items.GetItemName(weaponId);
        int attackTabId = p.UsingHd ? 87 : 73;
        int childId;

        if (weaponId == -1)
        {
            childId = 92;
            if (p.AttackStyle == 3)
            {
                p.AttackStyle = 2;
                SetConfig(w, 43, 2);
            }
        }
        else if (weapon == "Abyssal whip")
        {
            childId = 93;
            if (p.AttackStyle == 3)
            {
                p.AttackStyle = 2;
                SetConfig(w, 43, 2);
            }
        }
        else if (weapon is "Granite maul" or "Tzhaar-ket-om" or "Torags hammers")
        {
            childId = 76;
            if (p.AttackStyle == 3)
            {
                p.AttackStyle = 2;
                SetConfig(w, 43, 2);
            }
        }
        else if (weapon == "Veracs flail" || weapon.EndsWith("mace", StringComparison.Ordinal))
        {
            childId = 88;
        }
        else if (weapon.EndsWith("crossbow", StringComparison.Ordinal) || weapon.EndsWith(" c'bow", StringComparison.Ordinal))
        {
            childId = 79;
            if (p.AttackStyle == 3)
            {
                p.AttackStyle = 2;
                SetConfig(w, 43, 2);
            }
        }
        else if (weapon.EndsWith("bow", StringComparison.Ordinal) || weapon.EndsWith("bow full", StringComparison.Ordinal) || weapon == "Seercull")
        {
            childId = 77;
            if (p.AttackStyle == 3)
            {
                p.AttackStyle = 2;
                SetConfig(w, 43, 2);
            }
        }
        else if (weapon.StartsWith("Staff", StringComparison.Ordinal) || weapon.EndsWith("staff", StringComparison.Ordinal) || weapon == "Toktz-mej-tal")
        {
            childId = 90;
        }
        else if (weapon.EndsWith("dart", StringComparison.Ordinal) || weapon.EndsWith("knife", StringComparison.Ordinal) || weapon.EndsWith("thrownaxe", StringComparison.Ordinal) || weapon == "Toktz-xil-ul")
        {
            childId = 91;
            if (p.AttackStyle == 3)
            {
                p.AttackStyle = 2;
                SetConfig(w, 43, 2);
            }
        }
        else if (weapon.EndsWith("dagger", StringComparison.Ordinal) || weapon.EndsWith("dagger(s)", StringComparison.Ordinal) || weapon.EndsWith("dagger(+)", StringComparison.Ordinal) || weapon.EndsWith("dagger(p)", StringComparison.Ordinal))
        {
            childId = 89;
        }
        else if (weapon.EndsWith("pickaxe", StringComparison.Ordinal))
        {
            childId = 83;
        }
        else if (weapon.EndsWith("axe", StringComparison.Ordinal) || weapon.EndsWith("battleaxe", StringComparison.Ordinal))
        {
            childId = 75;
        }
        else if (weapon.EndsWith("halberd", StringComparison.Ordinal))
        {
            childId = 84;
            if (p.AttackStyle == 3)
            {
                p.AttackStyle = 2;
                SetConfig(w, 43, 2);
            }
        }
        else if (weapon.EndsWith("spear", StringComparison.Ordinal) || weapon == "Guthans warspear")
        {
            childId = 85;
            if (p.AttackStyle == 3)
            {
                p.AttackStyle = 2;
                SetConfig(w, 43, 2);
            }
        }
        else if (weapon.EndsWith("claws", StringComparison.Ordinal))
        {
            childId = 78;
        }
        else if (weapon.EndsWith("2h sword", StringComparison.Ordinal) || weapon.EndsWith("godsword", StringComparison.Ordinal) || weapon == "Saradomin sword")
        {
            childId = 81;
        }
        else
        {
            childId = 82;
        }

        SetTab(w, p, attackTabId, childId);
        SetString(w, weapon, childId, 0);
    }

    public void SetWelcome(FrameWriter w)
    {
        SetWindowPane(w, 549);
        SetInterface(w, 1, 549, 2, 378);
        SetInterface(w, 1, 549, 3, 17);
        SetString(w, "Messages of the Week!", 447, 0);
        SetString(w, "by: Davidi2", 447, 1);
        SetString(w, "Official Server for the MoparScape Client!", 447, 2);
        SetString(w, "No unread messages", 378, 37);
        SetString(w, "0", 378, 39);
        SetString(w, "", 378, 94);
        SetString(w, "You have 0 days of member credit remaining. Please click here to extend your credit", 378, 93);
        SetString(w, "999", 378, 96);
        SetString(w, "Welcome to DavidScape", 378, 115);
        SetString(w, "", 378, 116);
    }

    public void SendCoords(FrameWriter w, Player p, int x, int y)
    {
        w.CreateFrame(177);
        w.WriteByte(y);
        w.WriteByteS(x);
    }

    public void SendCoords(FrameWriter w, int x, int y)
    {
        w.CreateFrame(177);
        w.WriteByte(y);
        w.WriteByteS(x);
    }

    public void CreateGroundItem(FrameWriter w, Player p, int itemId, int itemAmt, int itemX, int itemY, int itemHeight)
    {
        if (GetDistance(itemX, itemY, p.AbsX, p.AbsY) <= 60 && p.HeightLevel == itemHeight)
        {
            SendCoords(w, p, itemX - ((p.MapRegionX - 6) * 8), itemY - ((p.MapRegionY - 6) * 8));
            w.CreateFrame(25);
            w.WriteWordBigEndianA(itemAmt);
            w.WriteByte(0);
            w.WriteWordBigEndianA(itemId);
        }
    }

    public void RemoveGroundItem(FrameWriter w, Player p, int itemId, int itemX, int itemY, int itemHeight)
    {
        if (GetDistance(itemX, itemY, p.AbsX, p.AbsY) <= 60 && p.HeightLevel == itemHeight)
        {
            SendCoords(w, p, itemX - ((p.MapRegionX - 6) * 8), itemY - ((p.MapRegionY - 6) * 8));
            w.CreateFrame(201);
            w.WriteByte(0);
            w.WriteWord(itemId);
        }
    }

    public void SetSkillLvl(FrameWriter w, Player p, int lvlId)
    {
        w.CreateFrame(217);
        w.WriteByteC(p.SkillLvl[lvlId]);
        w.WriteDWordV2(p.SkillXP[lvlId]);
        w.WriteByteC(lvlId);
    }

    public void SetSkillLvl2(FrameWriter w, Player p, int lvlId, int lvl)
    {
        w.CreateFrame(217);
        w.WriteByteC(lvl);
        w.WriteDWordV2(p.SkillXP[lvlId]);
        w.WriteByteC(lvlId);
    }

    public void SetItems(FrameWriter w, int interfaceId, int childId, int type, int[] itemArray, int[] itemAmt)
    {
        w.CreateFrameVarSizeWord(255);
        w.WriteWord(interfaceId);
        w.WriteWord(childId);
        w.WriteWord(type);
        w.WriteWord(itemArray.Length);
        for (int i = 0; i < itemArray.Length; i++)
        {
            if (itemAmt[i] > 254)
            {
                w.WriteByteS(255);
                w.WriteDWordV2(itemAmt[i]);
            }
            else
            {
                w.WriteByteS(itemAmt[i]);
            }

            w.WriteWordBigEndian(itemArray[i] + 1);
        }
        w.EndFrameVarSizeWord();
    }

    public void SetInterfaceConfig(FrameWriter w, int interfaceId, int childId, bool set)
    {
        w.CreateFrame(59);
        w.WriteByteC(set ? 1 : 0);
        w.WriteWord(childId);
        w.WriteWord(interfaceId);
    }

    public void SendMessage(FrameWriter w, string s)
    {
        w.CreateFrameVarSize(218);
        w.WriteString(s);
        w.EndFrameVarSize();
    }

    public void SetString(FrameWriter w, string str, int interfaceId, int childId)
    {
        int sSize = str.Length + 5;
        w.CreateFrame(179);
        w.WriteByte(sSize / 256);
        w.WriteByte(sSize % 256);
        w.WriteString(str);
        w.WriteWord(childId);
        w.WriteWord(interfaceId);
    }

    public void UpdateMovement(FrameWriter w, Player p)
    {
        w.CreateFrameVarSizeWord(216);
        w.InitBitAccess();
        w.WriteBits(1, 1);
        if (p.RunDir == -1)
        {
            w.WriteBits(2, 1);
            w.WriteBits(3, p.WalkDir);
            w.WriteBits(1, p.UpdateReq ? 1 : 0);
        }
        else
        {
            w.WriteBits(2, 2);
            w.WriteBits(3, p.RunDir);
            w.WriteBits(3, p.WalkDir);
            w.WriteBits(1, p.UpdateReq ? 1 : 0);
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

    public void NoMovement(FrameWriter w, Player p)
    {
        w.CreateFrameVarSizeWord(216);
        w.InitBitAccess();
        w.WriteBits(1, p.UpdateReq ? 1 : 0);
        if (p.UpdateReq)
        {
            w.WriteBits(2, 0);
        }
    }

    public void Teleport(FrameWriter w, Player p)
    {
        w.CreateFrameVarSizeWord(216);
        w.InitBitAccess();
        w.WriteBits(1, 1);
        w.WriteBits(2, 3);
        w.WriteBits(7, p.CurrentX);
        w.WriteBits(1, 1);
        w.WriteBits(2, p.HeightLevel);
        w.WriteBits(1, p.UpdateReq ? 1 : 0);
        w.WriteBits(7, p.CurrentY);
    }

    public bool SetMapRegion(FrameWriter w, Player p)
    {
        w.CreateFrameVarSizeWord(142);
        w.WriteWordA(p.MapRegionX);
        w.WriteWordBigEndianA(p.CurrentY);
        w.WriteWordA(p.CurrentX);
        bool forceSend = true;
        p.RebuildNPCList = true;

        if (((p.MapRegionX / 8) == 48 || (p.MapRegionX / 8) == 49) && (p.MapRegionY / 8) == 48)
        {
            forceSend = false;
        }
        if ((p.MapRegionX / 8) == 48 && (p.MapRegionY / 8) == 148)
        {
            forceSend = false;
        }

        for (int xCalc = (p.MapRegionX - 6) / 8; xCalc <= (p.MapRegionX + 6) / 8; xCalc++)
        {
            for (int yCalc = (p.MapRegionY - 6) / 8; yCalc <= (p.MapRegionY + 6) / 8; yCalc++)
            {
                int region = yCalc + (xCalc << 8);
                if (forceSend || (yCalc != 49 && yCalc != 149 && yCalc != 147 && xCalc != 50 && (xCalc != 49 || yCalc != 47)))
                {
                    int[]? mapData = _mapData.GetMapData(region);
                    if (mapData == null)
                    {
                        p.AbsX = 3254;
                        p.AbsY = 3420;
                        p.HeightLevel = 0;
                        return false;
                    }

                    w.WriteDWord(mapData[0]);
                    w.WriteDWord(mapData[1]);
                    w.WriteDWord(mapData[2]);
                    w.WriteDWord(mapData[3]);
                }
            }
        }

        w.WriteByteC(p.HeightLevel);
        w.WriteWord(p.MapRegionY);
        w.EndFrameVarSizeWord();
        return true;
    }

    private static bool AddInventoryItem(Player p, int itemId, int amount)
    {
        if (itemId < 0 || amount <= 0)
        {
            return true;
        }

        for (int i = 0; i < p.Items.Length; i++)
        {
            if (p.Items[i] == -1)
            {
                p.Items[i] = itemId;
                p.ItemsN[i] = amount;
                return true;
            }
        }

        return false;
    }

    private static int EncryptPlayerChat(byte[] output, int outOffset, int bitOffset, int length, string message)
    {
        byte[] source = System.Text.Encoding.Default.GetBytes(message);
        try
        {
            int end = length + outOffset;
            int carry = 0;
            int bitPosition = bitOffset << 3;
            for (; end > outOffset; outOffset++)
            {
                int charIndex = source[outOffset] & 0xFF;
                int encoded = ChatEncodeValues[charIndex];
                int size = ChatEncodeSizes[charIndex];
                int byteIndex = bitPosition >> 3;
                int shift = bitPosition & 0x7;
                carry &= -shift >> 31;
                bitPosition += size;
                int lastByte = ((shift + size) - 1 >> 3) + byteIndex;
                shift += 24;
                output[byteIndex] = (byte)(carry = carry | UnsignedRightShift(encoded, shift));
                if (lastByte > byteIndex)
                {
                    byteIndex++;
                    shift -= 8;
                    output[byteIndex] = (byte)(carry = UnsignedRightShift(encoded, shift));
                    if (lastByte > byteIndex)
                    {
                        byteIndex++;
                        shift -= 8;
                        output[byteIndex] = (byte)(carry = UnsignedRightShift(encoded, shift));
                        if (lastByte > byteIndex)
                        {
                            shift -= 8;
                            byteIndex++;
                            output[byteIndex] = (byte)(carry = UnsignedRightShift(encoded, shift));
                            if (byteIndex < lastByte)
                            {
                                shift -= 8;
                                byteIndex++;
                                output[byteIndex] = (byte)(carry = encoded << -shift);
                            }
                        }
                    }
                }
            }

            return -bitOffset + ((7 + bitPosition) >> 3);
        }
        catch
        {
            return 0;
        }
    }

    private static int UnsignedRightShift(int value, int shift) => (int)((uint)value >> shift);
}
