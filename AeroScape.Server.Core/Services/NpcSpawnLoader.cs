using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Engine;

namespace AeroScape.Server.Core.Services;

public sealed record NpcDefinition(
    int NpcType,
    int CombatLevel,
    int MaxHp,
    int MaxHit,
    int AttackType,
    int Weakness,
    int SpawnTime,
    int AttackEmote,
    int DefendEmote,
    int DeathEmote,
    int AttackDelay,
    string Name,
    string Examine);

public sealed record NpcSpawnDefinition(
    int NpcType,
    int X,
    int Y,
    int Height,
    int MoveRangeX1,
    int MoveRangeY1,
    int MoveRangeX2,
    int MoveRangeY2);

public sealed class NpcSpawnLoader
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public Dictionary<int, NpcDefinition> LoadDefinitions(string path)
    {
        var definitions = new Dictionary<int, NpcDefinition>();
        if (!File.Exists(path))
            return definitions;

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal) || line == "[ENDOFNPCLIST]")
                continue;

            if (!line.StartsWith("npc=", StringComparison.Ordinal))
                continue;

            var parts = line[4..].Replace("\t\t", "\t", StringComparison.Ordinal).Split('\t', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 13)
                continue;

            var definition = new NpcDefinition(
                Parse(parts[0]),
                Parse(parts[1]),
                Parse(parts[2]),
                Parse(parts[3]),
                Parse(parts[4]),
                Parse(parts[5]),
                Parse(parts[6]),
                Parse(parts[7]),
                Parse(parts[8]),
                Parse(parts[9]),
                Parse(parts[10]),
                parts[11].Replace('_', ' '),
                parts[12].Replace('_', ' '));

            definitions[definition.NpcType] = definition;
        }

        return definitions;
    }

    public List<NpcSpawnDefinition> LoadSpawns(string path)
    {
        var spawns = new List<NpcSpawnDefinition>();
        if (!File.Exists(path))
            return spawns;

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal) || line == "[ENDOFSPAWNLIST]")
                continue;

            if (!line.StartsWith("spawn=", StringComparison.Ordinal))
                continue;

            var parts = line[6..].Replace("\t\t", "\t", StringComparison.Ordinal).Split('\t', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 8)
                continue;

            spawns.Add(new NpcSpawnDefinition(
                Parse(parts[0]),
                Parse(parts[1]),
                Parse(parts[2]),
                Parse(parts[3]),
                Parse(parts[4]),
                Parse(parts[5]),
                Parse(parts[6]),
                Parse(parts[7])));
        }

        return spawns;
    }

    public void ApplyDefinition(NPC npc, IReadOnlyDictionary<int, NpcDefinition> definitions)
    {
        if (!definitions.TryGetValue(npc.NpcType, out var def))
            return;

        npc.Name = def.Name;
        npc.CombatLevel = def.CombatLevel;
        npc.MaxHP = def.MaxHp;
        npc.CurrentHP = def.MaxHp;
        npc.MaxHit = def.MaxHit;
        npc.AtkType = def.AttackType;
        npc.Weakness = def.Weakness;
        npc.RespawnDelay = Math.Max(def.SpawnTime, 1);
        npc.AttackEmote = def.AttackEmote;
        npc.DefendEmote = def.DefendEmote;
        npc.DeathEmote = def.DeathEmote;
        npc.AttackDelay = Math.Max(def.AttackDelay, 1);
    }

    private static int Parse(string value) => int.Parse(value, Culture);
}
