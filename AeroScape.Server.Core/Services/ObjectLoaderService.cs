using System;
using System.Collections.Generic;
using System.IO;

namespace AeroScape.Server.Core.Services;

public sealed record LoadedObject(int ObjectId, int X, int Y, int Face, int Type);

public sealed class ObjectLoaderService
{
    public List<LoadedObject> LoadFile(string path)
    {
        var result = new List<LoadedObject>();
        if (!File.Exists(path))
            return result;

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line == "[EOF]")
                continue;

            var split = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
            if (split.Length < 5 || !split[0].StartsWith("object=", StringComparison.Ordinal))
                continue;

            result.Add(new LoadedObject(
                int.Parse(split[0].Split('=')[1]),
                int.Parse(split[1]),
                int.Parse(split[2]),
                int.Parse(split[3]),
                int.Parse(split[4])));
        }

        return result;
    }
}
