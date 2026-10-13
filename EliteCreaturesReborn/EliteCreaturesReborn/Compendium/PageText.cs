using System.Text;

namespace EliteCreaturesReborn.Compendium
{
    /// <summary>
    /// The compendium pages' shared formatting: headings in the orange the game's own pages use, table columns placed
    /// with TextMeshPro's pos tag, biome names in the player's language, and multipliers, percents and seconds written
    /// one way.
    /// </summary>
    internal static class PageText
    {
        /// <summary>U+2605, the black star, written as an escape so the file stays plain ASCII.</summary>
        public const char Star = '\u2605';

        /// <summary>The grey of the altar's side notes (<see cref="Display.AspectText"/>).</summary>
        private const string GreyHex = "#A0A0A0";

        /// <summary>The biomes in the order a group reaches them, the Ocean last.</summary>
        public static readonly Heightmap.Biome[] Biomes =
        {
            Heightmap.Biome.Meadows, Heightmap.Biome.BlackForest, Heightmap.Biome.Swamp, Heightmap.Biome.Mountain,
            Heightmap.Biome.Plains, Heightmap.Biome.Mistlands, Heightmap.Biome.AshLands, Heightmap.Biome.DeepNorth,
            Heightmap.Biome.Ocean,
        };

        /// <summary>A heading, with a blank line above it unless it opens the page.</summary>
        public static void Heading(StringBuilder text, string title) =>
            text.Append(text.Length > 0 ? "\n<color=orange>" : "<color=orange>").Append(title).Append("</color>\n");

        /// <summary>One table row: each cell starts at its edge, in percent of the text area's width.</summary>
        public static void Row(StringBuilder text, int[] edges, params string[] cells)
        {
            for (int i = 0; i < cells.Length && i < edges.Length; i++)
            {
                text.Append("<pos=").Append(edges[i]).Append("%>").Append(cells[i]);
            }
            text.Append('\n');
        }

        public static string Grey(string words) => $"<color={GreyHex}>{words}</color>";

        public static string Times(float value) => "x" + value.ToString("0.##");

        public static string Percent(float value) => value.ToString("0") + "%";

        /// <summary>"1 second", "1.5 seconds", "10 seconds".</summary>
        public static string Seconds(float value)
        {
            string number = value.ToString("0.#");
            return number == "1" ? "1 second" : number + " seconds";
        }

        /// <summary>The share a multiplier adds or takes away, as a whole percent: 1.6 and 0.4 are both "60%".</summary>
        public static string Change(float multiplier) => Percent(System.Math.Abs(multiplier - 1f) * 100f);

        /// <summary>The name the game's own minimap shows for a biome.</summary>
        public static string BiomeName(Heightmap.Biome biome) =>
            Localization.instance.Localize("$biome_" + biome.ToString().ToLowerInvariant());

        /// <summary>The biome the local player stands in; the Meadows when there is none to read.</summary>
        public static Heightmap.Biome Here()
        {
            Player player = Player.m_localPlayer;
            Heightmap.Biome biome = player != null ? player.GetCurrentBiome() : Heightmap.Biome.None;
            return biome == Heightmap.Biome.None ? Heightmap.Biome.Meadows : biome;
        }
    }
}
