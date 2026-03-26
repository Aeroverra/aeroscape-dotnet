using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AeroScape.Server.Core.Entities;
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
        Stream stream, Player player, bool usingHD, MapDataService mapData, CancellationToken ct)
    {
        var w = new FrameWriter(8192);

        // ── 1. Window pane (setWindowPane → frame 239) ──────────────────
        // Welcome screen window pane = 549
        w.CreateFrame(239);
        w.WriteWord(549);
        w.WriteByteA(0);

        // ── 2. Welcome interface (setInterface: show=1, window=549, pos=2, child=378) ──
        WriteSetInterface(w, 1, 549, 2, 378);
        // Message panel
        WriteSetInterface(w, 1, 549, 3, 17);

        // Welcome strings
        WriteSetString(w, "AeroScape", 378, 115);
        WriteSetString(w, "", 378, 116);
        WriteSetString(w, "No unread messages", 378, 37);
        WriteSetString(w, "0", 378, 39);
        WriteSetString(w, "", 378, 94);
        WriteSetString(w, "Welcome to AeroScape", 378, 93);
        WriteSetString(w, "999", 378, 96);

        await w.FlushToAsync(stream, ct);

        // ── 3. Friend server connection status (frame 115) ──────────────
        w.CreateFrame(115);
        w.WriteByte(2);

        // ── 4. Map region ───────────────────────────────────────────────
        WriteMapRegion(w, player, mapData);

        await w.FlushToAsync(stream, ct);

        // ── 5. Main game pane + tabs ────────────────────────────────────
        int mainPane = usingHD ? 746 : 548;
        w.CreateFrame(239);
        w.WriteWord(mainPane);
        w.WriteByteA(0);

        // Set all the tab interfaces
        if (!usingHD)
        {
            WriteSetInterface(w, 1, 548, 6, 745);    // Minimap area
            WriteSetInterface(w, 1, 548, 11, 751);   // Chat options
            WriteSetInterface(w, 1, 548, 68, 752);   // Chatbox
            WriteSetInterface(w, 1, 548, 64, 748);   // HP bar
            WriteSetInterface(w, 1, 548, 65, 749);   // Prayer bar
            WriteSetInterface(w, 1, 548, 66, 750);   // Energy bar
            WriteSetInterface(w, 1, 548, 67, 747);   // Summoning bar
            WriteSetInterface(w, 1, 548, 8, 137);    // Player name on chat
            WriteSetInterface(w, 1, 548, 73, 92);    // Attack tab
            WriteSetInterface(w, 1, 548, 74, 320);   // Skill tab
            WriteSetInterface(w, 1, 548, 75, 274);   // Quest tab
            WriteSetInterface(w, 1, 548, 76, 149);   // Inventory tab
            WriteSetInterface(w, 1, 548, 77, 387);   // Equipment tab
            WriteSetInterface(w, 1, 548, 78, 271);   // Prayer tab
            WriteSetInterface(w, 1, 548, 79, 192);   // Magic tab
            WriteSetInterface(w, 1, 548, 80, 662);   // Summoning tab
            WriteSetInterface(w, 1, 548, 81, 550);   // Friend tab
            WriteSetInterface(w, 1, 548, 82, 551);   // Ignore tab
            WriteSetInterface(w, 1, 548, 83, 589);   // Clan tab
            WriteSetInterface(w, 1, 548, 84, 261);   // Setting tab
            WriteSetInterface(w, 1, 548, 85, 464);   // Emote tab
            WriteSetInterface(w, 1, 548, 86, 239);   // Music tab
            WriteSetInterface(w, 1, 548, 87, 182);   // Logout tab
        }
        else
        {
            WriteSetInterface(w, 1, 746, 6, 745);
            WriteSetInterface(w, 1, 746, 11, 751);
            WriteSetInterface(w, 1, 746, 68, 752);
            WriteSetInterface(w, 1, 746, 64, 748);
            WriteSetInterface(w, 1, 746, 65, 749);
            WriteSetInterface(w, 1, 746, 66, 750);
            WriteSetInterface(w, 1, 746, 67, 747);
            WriteSetInterface(w, 1, 746, 8, 137);
            WriteSetInterface(w, 1, 746, 73, 92);
            WriteSetInterface(w, 1, 746, 74, 320);
            WriteSetInterface(w, 1, 746, 75, 274);
            WriteSetInterface(w, 1, 746, 76, 149);
            WriteSetInterface(w, 1, 746, 77, 387);
            WriteSetInterface(w, 1, 746, 78, 271);
            WriteSetInterface(w, 1, 746, 79, 192);
            WriteSetInterface(w, 1, 746, 80, 662);
            WriteSetInterface(w, 1, 746, 81, 550);
            WriteSetInterface(w, 1, 746, 82, 551);
            WriteSetInterface(w, 1, 746, 83, 589);
            WriteSetInterface(w, 1, 746, 84, 261);
            WriteSetInterface(w, 1, 746, 85, 464);
            WriteSetInterface(w, 1, 746, 86, 239);
            WriteSetInterface(w, 1, 746, 87, 182);
        }

        await w.FlushToAsync(stream, ct);

        // ── 6. Configs ──────────────────────────────────────────────────
        WriteConfig(w, 1160, -1);
        WriteConfig(w, 173, 0);
        WriteConfig(w, 313, -1);
        WriteConfig(w, 465, -1);
        WriteConfig(w, 802, -1);
        WriteConfig(w, 1085, 249852);
        WriteConfig(w, 172, player.AutoRetaliate);
        WriteConfig(w, 43, player.AttackStyle);

        // ── 7. Skills ───────────────────────────────────────────────────
        for (int i = 0; i < Player.SkillCount; i++)
        {
            WriteSkillLevel(w, player, i);
        }

        await w.FlushToAsync(stream, ct);

        // ── 8. Inventory + Equipment ────────────────────────────────────
        WriteItems(w, 149, 0, 93, player.Items, player.ItemsN);
        WriteItems(w, 387, 28, 93, player.Equipment, player.EquipmentN);

        // ── 9. Player options ───────────────────────────────────────────
        WritePlayerOption(w, "null", 1);
        WritePlayerOption(w, "Trade", 2);
        WritePlayerOption(w, "Duel", 3);

        // ── 10. Run energy ──────────────────────────────────────────────
        w.CreateFrame(99);
        w.WriteByte(player.RunEnergy);

        // ── 11. Welcome message ─────────────────────────────────────────
        WriteSendMessage(w, "Welcome to AeroScape.");

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

    /// <summary>Frame 142 (var-size word): setMapRegion with XTEA keys from MapDataService</summary>
    private static void WriteMapRegion(FrameWriter w, Player p, MapDataService mapData)
    {
        // Calculate map region from absolute coordinates
        p.MapRegionX = (p.AbsX >> 3) - 6;
        p.MapRegionY = (p.AbsY >> 3) - 6;
        p.CurrentX = p.AbsX - 8 * p.MapRegionX;
        p.CurrentY = p.AbsY - 8 * p.MapRegionY;

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
