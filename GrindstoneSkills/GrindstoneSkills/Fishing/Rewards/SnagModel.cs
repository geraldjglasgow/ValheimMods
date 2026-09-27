using System;
using System.Collections.Generic;
using System.Linq;
using YamlConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// The parsed GrindstoneSkills.Snags*.yml files: under <c>biomes:</c> a table of snags per biome, each read exactly
    /// as a table of finds (<see cref="FindReader"/>: name, weight, items). A later file replaces an earlier file's table
    /// for the same biome. Parsed on every machine; it holds only names.
    /// </summary>
    public sealed class SnagModel : YamlModel
    {
        private static readonly string BiomeNames = string.Join(", ", FindModel.Biomes.Select(b => b.ToString()));

        public Dictionary<Heightmap.Biome, List<FindEntry>> ByBiome { get; } = new Dictionary<Heightmap.Biome, List<FindEntry>>();

        /// <summary>How many snags all tables hold together.</summary>
        public int Count => ByBiome.Values.Sum(t => t.Count);

        protected override void Read(YamlNode root)
        {
            YamlNode biomes = root.Get("biomes");
            if (biomes.Kind == YamlNodeKind.Missing || biomes.Kind == YamlNodeKind.Null)
                return;
            foreach (KeyValuePair<string, YamlNode> entry in biomes.Entries)
                ReadBiome(entry.Key, entry.Value);
        }

        protected override void Verify()
        {
            if (Errors.Count == 0 && Count == 0)
                Warnings.Add("no snags are listed under 'biomes'; a snagged line brings in only weeds");
        }

        private void ReadBiome(string name, YamlNode node)
        {
            if (!FindModel.TryParseBiome(name, out Heightmap.Biome biome))
            {
                node.Error($"'{name}' is not a biome; use one of {BiomeNames}");
                return;
            }
            ByBiome[biome] = FindReader.ReadTable(node);
        }
    }
}
