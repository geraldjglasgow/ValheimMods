using System;
using System.Collections.Generic;
using UnityEngine;
using YamlConfig;

namespace EarthWright.Terrain
{
    /// <summary>
    /// The parsed EarthWright.Limits*.yml files: per biome, how far the ground may be raised and dug. A biome or value
    /// that is not given uses the global "Raise Limit" and "Dig Limit" settings.
    /// </summary>
    public sealed class LimitsModel : YamlModel
    {
        public const float Min = 0.5f;
        public const float Max = 512f;

        /// <summary>The overrides by biome slot (<see cref="LimitBiomes"/>); <see cref="BiomeLimit.None"/> where a biome is not listed.</summary>
        public BiomeLimit[] Rules { get; } = NewRules();

        /// <summary>How many biomes the files list.</summary>
        public int Count { get; private set; }

        protected override void Read(YamlNode root)
        {
            YamlNode biomes = root.Get("biomes");
            if (biomes.Kind == YamlNodeKind.Map)
            {
                foreach (KeyValuePair<string, YamlNode> entry in biomes.Entries)
                    ReadBiome(entry.Key, entry.Value);
            }
            ReadMisplaced(root);
        }

        /// <summary>A biome uncommented without its indentation lands at the top of the file; read it and say so.</summary>
        private void ReadMisplaced(YamlNode root)
        {
            foreach (KeyValuePair<string, YamlNode> entry in root.Entries)
            {
                if (string.Equals(entry.Key, "biomes", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!LimitBiomes.TryParse(entry.Key, out _))
                {
                    entry.Value.Warn("unknown key");
                    continue;
                }
                entry.Value.Warn("this biome sits at the top of the file; indent it two spaces so it is inside 'biomes:' (applied anyway)");
                ReadBiome(entry.Key, entry.Value);
            }
        }

        private void ReadBiome(string name, YamlNode node)
        {
            if (!LimitBiomes.TryParse(name, out int slot))
            {
                node.Error($"'{name}' is not a biome; use one of {LimitBiomes.Names}");
                return;
            }
            if (node.Kind != YamlNodeKind.Map)
            {
                node.Error("a biome needs its limits as a map, for example { raise: 16, dig: 4 }");
                return;
            }
            if (float.IsNaN(Rules[slot].Raise) && float.IsNaN(Rules[slot].Dig))
                Count++;
            Rules[slot] = new BiomeLimit { Raise = ReadMetres(node.Get("raise")), Dig = ReadMetres(node.Get("dig")) };
        }

        private static BiomeLimit[] NewRules()
        {
            BiomeLimit[] rules = new BiomeLimit[LimitBiomes.Slots];
            for (int i = 0; i < rules.Length; i++)
                rules[i] = BiomeLimit.None;
            return rules;
        }

        /// <summary>A limit in metres, clamped to the allowed range with a warning; NaN when the key is not there.</summary>
        private static float ReadMetres(YamlNode node)
        {
            if (!node.TryFloat(out float value))
                return float.NaN;
            if (value < Min || value > Max)
            {
                node.Warn($"{value} is outside {Min} to {Max} metres; clamped");
                value = Mathf.Clamp(value, Min, Max);
            }
            return value;
        }
    }
}
