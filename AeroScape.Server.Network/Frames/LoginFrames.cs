using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Items;
using AeroScape.Server.Core.World;

namespace AeroScape.Server.Network.Frames;

/// <summary>
/// Sends the initial set of frames to the client after a successful login.
/// Mirrors the post-login sequence in DavidScape/io/Login.java and Frames.java.
/// </summary>
public static class LoginFrames
{
    /// <summary>
    /// Send the full post-login initialization sequence to the client.
    /// This gets the client past the loading screen and into the game world.
    /// </summary>
    public static async Task SendLoginSequenceAsync(
        Stream stream, Player player, bool usingHD, MapDataService mapData, ItemDefinitionLoader items, CancellationToken ct)
    {
        var w = new FrameWriter(8192);
        WriteMapRegion(w, player, mapData);
        await w.FlushToAsync(stream, ct);
        WriteSetWelcome(w);
        WriteFriendServerStatus(w);
        WriteSetInterfaces(w, player, usingHD);
        WriteConfig(w, 1160, -1);
        WriteConfig(w, 173, 0);
        WriteConfig(w, 313, -1);
        WriteConfig(w, 465, -1);
        WriteConfig(w, 802, -1);
        WriteConfig(w, 1085, 249852);
        for (int i = 0; i < Player.SkillCount; i++)
        {
            WriteSkillLevel(w, player, i);
        }
        WriteItems(w, 149, 0, 93, player.Items, player.ItemsN);
        WriteItems(w, 387, 28, 93, player.Equipment, player.EquipmentN);
        WritePlayerOption(w, "null", 1);
        WritePlayerOption(w, "Trade", 2);
        WritePlayerOption(w, "Duel", 3);
        WriteConfig(w, 172, player.AutoRetaliate);
        WriteConfig(w, 43, player.AttackStyle);
        WriteFriendServerStatus(w);
        WriteWeaponTab(w, player, items, usingHD);
        await w.FlushToAsync(stream, ct);

        w.Dispose();
    }

    // ── Helper methods for specific frames ───────────────────────────────

    /// <summary>Frame 93: setInterface(showId, windowId, interfaceId, childId)</summary>
    private static void WriteSetInterface(FrameWriter w, int showId, int windowId, int interfaceId, int childId)
    {
        w.CreateFrame(93);
        w.WriteWord(childId);
        w.WriteByteA(showId);
        w.WriteWord(windowId);
        w.WriteWord(interfaceId);
    }

    /// <summary>Frame 179: setString (fixed-size with manual length prefix)</summary>
    private static void WriteSetString(FrameWriter w, string text, int interfaceId, int childId)
    {
        int sSize = text.Length + 5; // string length + newline terminator + 2 words
        w.CreateFrame(179);
        w.WriteByte(sSize / 256);
        w.WriteByte(sSize % 256);
        w.WriteString(text);
        w.WriteWord(childId);
        w.WriteWord(interfaceId);
    }

    private static void WriteFriendServerStatus(FrameWriter w)
    {
        w.CreateFrame(115);
        w.WriteByte(2);
    }

    private static void WriteSetWelcome(FrameWriter w)
    {
        w.CreateFrame(239);
        w.WriteWord(549);
        w.WriteByteA(0);
        WriteSetInterface(w, 1, 549, 2, 378);
        WriteSetInterface(w, 1, 549, 3, 17);
        WriteSetString(w, "Messages of the Week!", 447, 0);
        WriteSetString(w, "by: Davidi2", 447, 1);
        WriteSetString(w, "Official Server for the MoparScape Client!", 447, 2);
        WriteSetString(w, "No unread messages", 378, 37);
        WriteSetString(w, "0", 378, 39);
        WriteSetString(w, "", 378, 94);
        WriteSetString(w, "You have 0 days of member credit remaining. Please click here to extend your credit", 378, 93);
        WriteSetString(w, "999", 378, 96);
        WriteSetString(w, "Welcome to DavidScape", 378, 115);
        WriteSetString(w, "", 378, 116);
    }

    private static void WriteSetInterfaces(FrameWriter w, Player p, bool usingHD)
    {
        for (int i = 0; i < 137; i++)
        {
            WriteSetString(w, "!~_-_~_-_~!", 274, 13 + i);
        }

        WriteSetString(w, "DavidScape 508", 274, 5);
        WriteSetString(w, "Quest and Teles:", 274, 6);
        WriteSetString(w, "Dragon Slayer Quest", 274, 7);
        WriteSetString(w, "PVP", 274, 8);
        WriteSetString(w, "GWD", 274, 9);
        WriteSetString(w, "Home", 274, 10);
        WriteSetString(w, "Help Desk", 274, 11);
        WriteSetString(w, "Staff Zone", 274, 12);
        WriteSetString(w, "Barbarian Assault", 274, 13);
        WriteSetString(w, "Barrows", 274, 14);
        WriteSetString(w, "Membership/Donate", 274, 16);
        WriteSetString(w, "Member Area", 274, 17);
        WriteSetString(w, "Newest Client", 274, 18);

        p.IsAncients = p.Equipment[3] == 4675 ? 1 : 0;

        if (!usingHD)
        {
            WriteSetInterface(w, 1, 548, 6, 745);
            WriteSetInterface(w, 1, 548, 11, 751);
            WriteSetInterface(w, 1, 548, 68, 752);
            WriteSetInterface(w, 1, 548, 64, 748);
            WriteSetInterface(w, 1, 548, 65, 749);
            WriteSetInterface(w, 1, 548, 66, 750);
            WriteSetInterface(w, 1, 548, 67, 747);
            WriteSetInterface(w, 1, 752, 8, 137);
            WriteSetInterface(w, 1, 548, 73, 92);
            WriteSetInterface(w, 1, 548, 74, 320);
            WriteSetInterface(w, 1, 548, 75, 274);
            WriteSetInterface(w, 1, 548, 76, 149);
            WriteSetInterface(w, 1, 548, 77, 387);
            WriteSetInterface(w, 1, 548, 78, 271);
            WriteSetInterface(w, 1, 548, 79, p.IsAncients == 1 ? 193 : 192);
            WriteSetInterface(w, 1, 548, 81, 550);
            WriteSetInterface(w, 1, 548, 82, 551);
            WriteSetInterface(w, 1, 548, 83, 589);
            WriteSetInterface(w, 1, 548, 84, 261);
            WriteSetInterface(w, 1, 548, 85, 464);
            WriteSetInterface(w, 1, 548, 86, 187);
            WriteSetInterface(w, 1, 548, 87, 182);
        }
        else
        {
            WriteSetInterface(w, 1, 549, 0, 746);
            WriteSetInterface(w, 1, 752, 8, 137);
            WriteSetInterface(w, 1, 746, 87, 92);
            WriteSetInterface(w, 1, 746, 88, 320);
            WriteSetInterface(w, 1, 746, 89, 274);
            WriteSetInterface(w, 1, 746, 90, 149);
            WriteSetInterface(w, 1, 746, 91, 387);
            WriteSetInterface(w, 1, 746, 92, 271);
            WriteSetInterface(w, 1, 746, 93, p.IsAncients == 1 ? 193 : 192);
            WriteSetInterface(w, 1, 746, 95, 550);
            WriteSetInterface(w, 1, 746, 96, 551);
            WriteSetInterface(w, 1, 746, 97, 589);
            WriteSetInterface(w, 1, 746, 98, 261);
            WriteSetInterface(w, 1, 746, 99, 464);
            WriteSetInterface(w, 1, 746, 65, 752);
            WriteSetInterface(w, 1, 746, 18, 751);
            WriteSetInterface(w, 1, 746, 13, 748);
            WriteSetInterface(w, 1, 746, 14, 749);
            WriteSetInterface(w, 1, 746, 15, 750);
            WriteSetInterface(w, 1, 746, 12, 747);
            WriteSetInterface(w, 1, 746, 100, 187);
            WriteSetInterface(w, 1, 746, 101, 182);
        }
    }

    private static void WriteWeaponTab(FrameWriter w, Player p, ItemDefinitionLoader items, bool usingHD)
    {
        string weapon = items.GetItemName(p.Equipment[3]);
        int attackTabId = usingHD ? 87 : 73;
        int childId;

        if (p.Equipment[3] == -1)
        {
            childId = 92;
            if (p.AttackStyle == 3)
            {
                p.AttackStyle = 2;
                WriteConfig(w, 43, 2);
            }
        }
        else if (weapon == "Abyssal whip")
        {
            childId = 93;
            if (p.AttackStyle == 3)
            {
                p.AttackStyle = 2;
                WriteConfig(w, 43, 2);
            }
        }
        else if (weapon is "Granite maul" or "Tzhaar-ket-om" or "Torags hammers")
        {
            childId = 76;
            if (p.AttackStyle == 3)
            {
                p.AttackStyle = 2;
                WriteConfig(w, 43, 2);
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
                WriteConfig(w, 43, 2);
            }
        }
        else if (weapon.EndsWith("bow", StringComparison.Ordinal) || weapon.EndsWith("bow full", StringComparison.Ordinal) || weapon == "Seercull")
        {
            childId = 77;
            if (p.AttackStyle == 3)
            {
                p.AttackStyle = 2;
                WriteConfig(w, 43, 2);
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
                WriteConfig(w, 43, 2);
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
                WriteConfig(w, 43, 2);
            }
        }
        else if (weapon.EndsWith("spear", StringComparison.Ordinal) || weapon == "Guthans warspear")
        {
            childId = 85;
            if (p.AttackStyle == 3)
            {
                p.AttackStyle = 2;
                WriteConfig(w, 43, 2);
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

        WriteSetInterface(w, 1, usingHD ? 746 : 548, attackTabId, childId);
        WriteSetString(w, weapon, childId, 0);
    }

    /// <summary>Frame 142 (var-size word): setMapRegion with XTEA keys from MapDataService</summary>
    private static void WriteMapRegion(FrameWriter w, Player p, MapDataService mapData)
    {
        // Calculate map region from absolute coordinates
        p.MapRegionX = (p.AbsX >> 3);
        p.MapRegionY = (p.AbsY >> 3);
        p.CurrentX = p.AbsX - 8 * (p.MapRegionX - 6);
        p.CurrentY = p.AbsY - 8 * (p.MapRegionY - 6);

        w.CreateFrameVarSizeWord(142);
        w.WriteWordA(p.MapRegionX);
        w.WriteWordBigEndianA(p.CurrentY);
        w.WriteWordA(p.CurrentX);

        bool forceSend = true;
        if ((p.MapRegionX / 8 == 48 || p.MapRegionX / 8 == 49) && p.MapRegionY / 8 == 48)
            forceSend = false;
        if (p.MapRegionX / 8 == 48 && p.MapRegionY / 8 == 148)
            forceSend = false;

        for (int xCalc = (p.MapRegionX - 6) / 8; xCalc <= (p.MapRegionX + 6) / 8; xCalc++)
        {
            for (int yCalc = (p.MapRegionY - 6) / 8; yCalc <= (p.MapRegionY + 6) / 8; yCalc++)
            {
                // Region id formula from Frames.java:
                //   int region = yCalc + (xCalc << 8);
                // Java used << 1786653352, but Java masks shift to & 31 → 1786653352 % 32 = 8
                int region = (xCalc << 8) + yCalc;

                if (forceSend ||
                    (yCalc != 49 && yCalc != 149 && yCalc != 147 &&
                     xCalc != 50 && (xCalc != 49 || yCalc != 47)))
                {
                    int[]? keys = mapData.GetMapData(region);
                    if (keys != null)
                    {
                        w.WriteDWord(keys[0]);
                        w.WriteDWord(keys[1]);
                        w.WriteDWord(keys[2]);
                        w.WriteDWord(keys[3]);
                    }
                    else
                    {
                        // No XTEA keys for this region — send zeroes (client treats as unencrypted)
                        w.WriteDWord(0);
                        w.WriteDWord(0);
                        w.WriteDWord(0);
                        w.WriteDWord(0);
                    }
                }
            }
        }

        w.WriteByteC(p.HeightLevel);
        w.WriteWord(p.MapRegionY);
        w.EndFrameVarSizeWord();
    }

    /// <summary>Config frame (auto-selects small/large variant)</summary>
    private static void WriteConfig(FrameWriter w, int id, int value)
    {
        if (value >= -128 && value < 128)
        {
            // Small config: frame 100
            w.CreateFrame(100);
            w.WriteWordA(id);
            w.WriteByteA(value);
        }
        else
        {
            // Large config: frame 161
            w.CreateFrame(161);
            w.WriteWord(id);
            w.WriteDWordV1(value);
        }
    }

    /// <summary>Frame 217: setSkillLvl</summary>
    private static void WriteSkillLevel(FrameWriter w, Player p, int skillId)
    {
        w.CreateFrame(217);
        w.WriteByteC(p.SkillLvl[skillId]);
        w.WriteDWordV2(p.SkillXP[skillId]);
        w.WriteByteC(skillId);
    }

    /// <summary>Frame 255 (var-size word): setItems</summary>
    private static void WriteItems(FrameWriter w, int interfaceId, int childId, int type, int[] itemArray, int[] itemAmt)
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

    /// <summary>Frame 252 (var-size): setPlayerOption</summary>
    private static void WritePlayerOption(FrameWriter w, string option, int slot)
    {
        w.CreateFrameVarSize(252);
        w.WriteByteC(1);
        w.WriteString(option);
        w.WriteByteC(slot);
        w.EndFrameVarSize();
    }

    /// <summary>Frame 218 (var-size): sendMessage</summary>
    private static void WriteSendMessage(FrameWriter w, string message)
    {
        w.CreateFrameVarSize(218);
        w.WriteString(message);
        w.EndFrameVarSize();
    }
}
