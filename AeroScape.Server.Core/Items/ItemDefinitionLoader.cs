using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace AeroScape.Server.Core.Items;

public sealed class ItemDefinitionLoader
{
    public const int MaxItemAmount = 2147483647; // Java Integer.MAX_VALUE

    private static readonly HashSet<int> UntradableItems = [6570];
    private readonly Dictionary<int, ItemDefinition> _definitions = [];
    private readonly string _itemsPath;
    private readonly string _stackablePath;
    private readonly string _equipmentPath;

    public ItemDefinitionLoader()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var dataRoot = Path.Combine(root, "legacy-java", "server508", "data", "items");
        _itemsPath = Path.Combine(dataRoot, "items.cfg");
        _stackablePath = Path.Combine(dataRoot, "stackable.dat");
        _equipmentPath = Path.Combine(dataRoot, "equipment.dat");
        Load();
    }

    public IReadOnlyDictionary<int, ItemDefinition> Definitions => _definitions;

    public ItemDefinition? Get(int itemId) => _definitions.GetValueOrDefault(itemId);

    public string GetItemName(int itemId)
    {
        if (itemId < 0)
        {
            return "Unarmed";
        }

        return _definitions.TryGetValue(itemId, out var definition)
            ? definition.Name
            : $"Item {itemId}";
    }

    public string GetItemDescription(int itemId)
    {
        if (itemId < 0)
        {
            return "An item.";
        }

        return _definitions.TryGetValue(itemId, out var definition)
            ? definition.Description
            : $"Item {itemId}";
    }

    public int GetEquipId(int itemId) => _definitions.GetValueOrDefault(itemId)?.EquipId ?? 0;

    public int GetItemValue(int itemId) => _definitions.GetValueOrDefault(itemId)?.LowAlch ?? 1;

    public int[] GetBonuses(int itemId) => _definitions.GetValueOrDefault(itemId)?.Bonuses ?? new int[12];

    public bool IsStackable(int itemId)
    {
        if (!_definitions.TryGetValue(itemId, out var definition))
        {
            return false;
        }

        return definition.IsNote || definition.Stackable;
    }

    public bool IsNoted(int itemId) => _definitions.GetValueOrDefault(itemId)?.IsNote ?? false;

    public bool IsUntradable(int itemId) => UntradableItems.Contains(itemId);

    public bool CanBeNoted(int itemId) => FindNote(itemId) >= 0;

    public int FindNote(int itemId)
    {
        var itemName = GetItemName(itemId);
        return _definitions.Values
            .FirstOrDefault(x => x.IsNote && string.Equals(x.Name, itemName, StringComparison.Ordinal))?.Id ?? -1;
    }

    public int FindUnnote(int itemId)
    {
        var itemName = GetItemName(itemId);
        return _definitions.Values
            .FirstOrDefault(x => !x.IsNote && string.Equals(x.Name, itemName, StringComparison.Ordinal))?.Id ?? -1;
    }

    private void Load()
    {
        LoadItemList();
        LoadStackableData();
        LoadEquipmentData();
        ApplyNoteFlags();
    }

    private void LoadItemList()
    {
        if (!File.Exists(_itemsPath))
        {
            return;
        }

        foreach (var rawLine in File.ReadLines(_itemsPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line == "[ENDOFITEMLIST]")
            {
                continue;
            }

            var split = line.IndexOf('=');
            if (split <= 0 || !line.StartsWith("item", StringComparison.Ordinal))
            {
                continue;
            }

            var tokenized = line[(split + 1)..]
                .Replace("\t\t", "\t", StringComparison.Ordinal)
                .Replace("\t\t", "\t", StringComparison.Ordinal)
                .Replace("\t\t", "\t", StringComparison.Ordinal)
                .Split('\t', StringSplitOptions.RemoveEmptyEntries);

            if (tokenized.Length < 7)
            {
                continue;
            }

            var id = int.Parse(tokenized[0], CultureInfo.InvariantCulture);
            var name = tokenized[1].Replace('_', ' ');
            var description = tokenized[2].Replace('_', ' ');
            var shopValue = int.Parse(tokenized[3], CultureInfo.InvariantCulture);
            var lowAlch = int.Parse(tokenized[4], CultureInfo.InvariantCulture);
            var bonuses = new int[12];
            for (var i = 0; i < bonuses.Length && (6 + i) < tokenized.Length; i++)
            {
                bonuses[i] = int.Parse(tokenized[6 + i], CultureInfo.InvariantCulture);
            }

            _definitions[id] = new ItemDefinition(id, name, description, shopValue, lowAlch, bonuses);
        }
    }

    private void LoadStackableData()
    {
        if (!File.Exists(_stackablePath))
        {
            return;
        }

        foreach (var rawLine in File.ReadLines(_stackablePath))
        {
            if (!int.TryParse(rawLine.Trim(), out var itemId))
            {
                continue;
            }

            if (_definitions.TryGetValue(itemId, out var definition))
            {
                definition.Stackable = true;
            }
        }
    }

    private void LoadEquipmentData()
    {
        if (!File.Exists(_equipmentPath))
        {
            return;
        }

        foreach (var rawLine in File.ReadLines(_equipmentPath))
        {
            var line = rawLine.Trim();
            var split = line.IndexOf(':');
            if (split <= 0)
            {
                continue;
            }

            if (!int.TryParse(line[..split], out var itemId) || !int.TryParse(line[(split + 1)..], out var equipId))
            {
                continue;
            }

            if (_definitions.TryGetValue(itemId, out var definition))
            {
                definition.EquipId = equipId;
            }
        }
    }

    private void ApplyNoteFlags()
    {
        foreach (var definition in _definitions.Values)
        {
            if (definition.Description.StartsWith("Swap", StringComparison.OrdinalIgnoreCase))
            {
                definition.IsNote = true;
                definition.Stackable = true;
            }
        }
    }
}

public sealed class ItemDefinition(
    int id,
    string name,
    string description,
    int shopValue,
    int lowAlch,
    int[] bonuses)
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public string Description { get; } = description;
    public int ShopValue { get; } = shopValue;
    public int LowAlch { get; } = lowAlch;
    public int EquipId { get; set; }
    public bool Stackable { get; set; }
    public bool IsNote { get; set; }
    public int[] Bonuses { get; } = bonuses;
}
