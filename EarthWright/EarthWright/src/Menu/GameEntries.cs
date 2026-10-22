using System.Collections.Generic;
using System.Linq;
using EarthWright.Actions;

namespace EarthWright.Menu
{
    /// <summary>
    /// One of the game's own terrain entries: its toggle label and the keys of EarthWright's description of what it does
    /// now (words in translations/English.yml).
    /// </summary>
    public sealed class GameEntry
    {
        public string Id;
        public string Label;
        public ToolFamily Tool;

        /// <summary>The entry has a second description for while it does not change the height (paved road and cultivate can be set to paint only).</summary>
        public bool HasFlatDescription;

        public string DescriptionKey => "ew_menu_game_" + Id + "_desc";

        public string FlatDescriptionKey => "ew_menu_game_" + Id + "_flat_desc";
    }

    /// <summary>
    /// The game's hoe and cultivator terrain entries in their menu order (read from the game's tables). EarthWright can
    /// switch each off, and while it is on it replaces their descriptions with what they do under EarthWright.
    /// </summary>
    public static class GameEntries
    {
        public static readonly IReadOnlyList<GameEntry> All = HoeEntries().Concat(CultivatorEntries()).ToList();

        private static readonly Dictionary<string, GameEntry> byId = All.ToDictionary(e => e.Id);

        public static GameEntry Get(string id) => id != null && byId.TryGetValue(id, out GameEntry entry) ? entry : null;

        public static bool Is(string id) => Get(id) != null;

        public static IEnumerable<GameEntry> For(ToolFamily tool) => All.Where(e => e.Tool == tool);

        private static IEnumerable<GameEntry> HoeEntries()
        {
            yield return Entry("mud_road_v2", "Level Ground", ToolFamily.Hoe);
            yield return Entry("raise_v2", "Raise Ground", ToolFamily.Hoe);
            yield return Entry("path_v2", "Pathen", ToolFamily.Hoe);
            yield return Entry("paved_road_v2", "Paved Road", ToolFamily.Hoe, flat: true);
        }

        private static IEnumerable<GameEntry> CultivatorEntries()
        {
            yield return Entry("cultivate_v2", "Cultivate", ToolFamily.Cultivator, flat: true);
            yield return Entry("replant_v2", "Replant", ToolFamily.Cultivator);
        }

        private static GameEntry Entry(string id, string label, ToolFamily tool, bool flat = false)
        {
            return new GameEntry { Id = id, Label = label, Tool = tool, HasFlatDescription = flat };
        }
    }
}
