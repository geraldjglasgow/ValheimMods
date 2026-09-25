using System.Collections.Generic;
using EliteCrafting.Core;
using YamlDotNet.RepresentationModel;

namespace EliteCrafting.Rules
{
    /// <summary>Reads <c>item_tiers:</c> (item-tier.md) and <c>biomes:</c>. Prefab names are checked against the game later, as warnings.</summary>
    internal static class TierMapParser
    {
        public static ItemTierMaps ParseItemTiers(MapReader root)
        {
            MapReader? sub = root.Sub("item_tiers");
            if (sub == null)
            {
                return new ItemTierMaps();
            }
            MapReader r = sub.Value;
            r.Unknown("items", "materials", "material_depth", "stations", "station_levels", "fallback_tier");
            return new ItemTierMaps
            {
                Items = YamlLists.IntMap(r, "items", 1, 7),
                Materials = YamlLists.IntMap(r, "materials", 1, 7),
                MaterialDepth = r.Int("material_depth", 3, 0, 10),
                Stations = YamlLists.IntMap(r, "stations", 1, 7),
                StationLevels = ReadStationLevels(r),
                FallbackTier = r.Int("fallback_tier", 1, 1, 7),
            };
        }

        private static Dictionary<string, IReadOnlyDictionary<int, int>> ReadStationLevels(MapReader r)
        {
            Dictionary<string, IReadOnlyDictionary<int, int>> result = new Dictionary<string, IReadOnlyDictionary<int, int>>();
            MapReader? sub = r.Sub("station_levels");
            if (sub == null)
            {
                return result;
            }
            foreach (KeyValuePair<string, YamlNode> pair in YamlLists.Pairs(sub.Value.Map))
            {
                result[pair.Key] = LevelRow(sub.Value, pair.Key);
            }
            return result;
        }

        private static Dictionary<int, int> LevelRow(MapReader sub, string station)
        {
            Dictionary<int, int> row = new Dictionary<int, int>();
            foreach (KeyValuePair<string, int> level in YamlLists.IntMap(sub, station, 1, 7))
            {
                if (Numbers.TryInt(level.Key, out int minLevel) && minLevel >= 1)
                {
                    row[minLevel] = level.Value;
                }
                else
                {
                    sub.Issues.Error($"{sub.At(station)}.{level.Key}", sub.Node(station), "keys are station levels (whole numbers from 1)");
                }
            }
            return row;
        }

        public static Dictionary<string, int> ParseBiomes(MapReader root)
        {
            Dictionary<string, int> biomes = YamlLists.IntMap(root, "biomes", 1, 8);
            foreach (KeyValuePair<string, int> biome in new List<KeyValuePair<string, int>>(biomes))
            {
                if (biome.Value > 7)
                {
                    biomes[biome.Key] = 7;   // tier 8 (deep_north) is reserved: clamped until it ships
                }
            }
            return biomes;
        }
    }
}
