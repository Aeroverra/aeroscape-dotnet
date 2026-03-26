using System;
using System.IO;
using AeroScape.Server.Core.Entities;

namespace AeroScape.Server.Core.Services;

public sealed class LegacyFileManager
{
    public void AppendData(string relativePath, string line)
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.AppendAllText(path, line + Environment.NewLine);
    }

    public void SaveCharacterSnapshot(Player player)
    {
        string directory = Path.Combine(Directory.GetCurrentDirectory(), "data", "characters", "mainsave");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, player.Username + ".txt");

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
        writer.WriteLine($"DragonSlayer:{player.DragonSlayer}");
        writer.WriteLine($"QuestPoints:{player.QuestPoints}");

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
}
