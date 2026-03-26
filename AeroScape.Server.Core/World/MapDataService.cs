using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace AeroScape.Server.Core.World;

/// <summary>
/// Loads and serves XTEA map region keys from the binary 1.dat file.
/// Ported from DavidScape/world/mapdata/MapData.java.
///
/// File format (big-endian):
///   Repeat {
///     int32 flag   — if 0, EOF sentinel; otherwise non-zero means a record follows
///     int32 mapId  — region id
///     int32[4] keys — four XTEA keys for the region
///   }
/// </summary>
public sealed class MapDataService
{
    private readonly Dictionary<int, int[]> _mapRegions = new();
    private readonly ILogger<MapDataService> _logger;

    public MapDataService(ILogger<MapDataService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get the 4-element XTEA key array for a region, or null if not found.
    /// </summary>
    public int[]? GetMapData(int regionId)
    {
        _mapRegions.TryGetValue(regionId, out var data);
        return data;
    }

    /// <summary>
    /// Load all map regions from a binary data file.
    /// Call once at startup.
    /// </summary>
    public void LoadRegions(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Map data file not found: {Path}", filePath);
            return;
        }

        using var fs = File.OpenRead(filePath);
        using var reader = new BinaryReader(fs);

        int count = 0;
        while (fs.Position < fs.Length)
        {
            // Read the flag int (big-endian)
            int flag = ReadInt32BigEndian(reader);
            if (flag == 0)
                break;

            int mapId = ReadInt32BigEndian(reader);
            int[] keys = new int[4];
            for (int i = 0; i < 4; i++)
                keys[i] = ReadInt32BigEndian(reader);

            _mapRegions[mapId] = keys;
            count++;
        }

        _logger.LogInformation("Loaded {Count} map regions from {Path}", count, filePath);
    }

    /// <summary>
    /// Read a big-endian 32-bit integer (Java DataInputStream format).
    /// </summary>
    private static int ReadInt32BigEndian(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }
}
