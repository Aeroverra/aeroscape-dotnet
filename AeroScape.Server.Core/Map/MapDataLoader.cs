using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace AeroScape.Server.Core.Map;

/// <summary>
/// Loads map region decryption keys (XTEA keys) from a binary data file.
/// Based on DavidScape/world/mapdata/MapData.java
/// </summary>
public sealed class MapDataLoader
{
    private readonly Dictionary<int, int[]> _mapRegions = new();
    private readonly ILogger<MapDataLoader> _logger;
    private readonly string _dataFile;

    public MapDataLoader(ILogger<MapDataLoader> logger)
    {
        _logger = logger;
        // In the App context, we can assume the current directory is AeroScape.Server.App
        _dataFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "mapdata", "1.dat");
        
        // Fallback for development/run from project root
        if (!File.Exists(_dataFile))
        {
            _dataFile = Path.Combine(Directory.GetCurrentDirectory(), "data", "mapdata", "1.dat");
        }
    }

    /// <summary>
    /// Loads all region XTEA keys from the map data file into memory.
    /// </summary>
    public void Load()
    {
        if (!File.Exists(_dataFile))
        {
            _logger.LogWarning("Map data file not found at {Path}. Map regions will fail to decrypt on the client.", _dataFile);
            return;
        }

        try
        {
            using var fs = new FileStream(_dataFile, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            while (fs.Position < fs.Length)
            {
                // Read Big-Endian integers from the Java data file
                int i = ReadInt32BigEndian(reader);
                if (i == 0)
                {
                    break;
                }

                int mapId = ReadInt32BigEndian(reader);
                int[] keys = new int[4];
                for (int j = 0; j < 4; j++)
                {
                    keys[j] = ReadInt32BigEndian(reader);
                }

                _mapRegions[mapId] = keys;
            }

            _logger.LogInformation("Loaded {Count} map region XTEA keys.", _mapRegions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load map data from {Path}", _dataFile);
        }
    }

    /// <summary>
    /// Retrieves the 4 XTEA keys for the given map region.
    /// Returns an array of zeros if the region is not found.
    /// </summary>
    public int[] GetMapData(int regionId)
    {
        if (_mapRegions.TryGetValue(regionId, out int[]? keys))
        {
            return keys;
        }

        return new int[] { 0, 0, 0, 0 };
    }

    private static int ReadInt32BigEndian(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        if (bytes.Length < 4) throw new EndOfStreamException();
        
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        return BitConverter.ToInt32(bytes, 0);
    }
}
