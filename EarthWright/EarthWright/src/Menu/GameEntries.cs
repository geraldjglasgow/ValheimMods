using System.Collections.Generic;
using System.Linq;
using EarthWright.Actions;

namespace EarthWright.Menu
{
    /// <summary>One of the game's own terrain entries: its toggle label and EarthWright's description of what it does now.</summary>
    public sealed class GameEntry
    {
        public string Id;
        public string Label;
        public ToolFamily Tool;
        public string EnglishDescription;
        public HintKind[] Hints;

        /// <summary>The description while the entry does not change the height (paved road and cultivate can be set to paint only).</summary>
        public string EnglishFlatDescription;

        public HintKind[] FlatHints;

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
            yield return Entry("mud_road_v2", "Level Ground", ToolFamily.Hoe,
                "Levels the ground inside the brush toward the target height: your feet, the aimed point while Shift is held, or a locked or exact height. The level style decides how far each swing goes.",
                HintKind.Adjust, HintKind.Next, HintKind.Style, HintKind.Lock, HintKind.Hard);
            yield return Entry("raise_v2", "Raise Ground", ToolFamily.Hoe,
                "Raises the ground inside the brush by the set amount. Repeated swings add up to the raise limit.",
                HintKind.Adjust, HintKind.Next, HintKind.Shape, HintKind.Hard);
            yield return Entry("path_v2", "Pathen", ToolFamily.Hoe,
                "Paints a dirt path inside the brush without changing the height.",
                HintKind.Adjust, HintKind.Shape, HintKind.Paint);
            GameEntry paved = Entry("paved_road_v2", "Paved Road", ToolFamily.Hoe,
                "Levels the ground inside the brush toward the target height and paves it.",
                HintKind.Adjust, HintKind.Shape, HintKind.Lock, HintKind.Paint);
            paved.EnglishFlatDescription = "Paves the ground inside the brush without changing its height.";
            paved.FlatHints = new[] { HintKind.Adjust, HintKind.Shape, HintKind.Paint };
            yield return paved;
        }

        private static IEnumerable<GameEntry> CultivatorEntries()
        {
            GameEntry cultivate = Entry("cultivate_v2", "Cultivate", ToolFamily.Cultivator,
                "Evens out the ground inside the brush and tills it for planting.",
                HintKind.Adjust, HintKind.Shape, HintKind.Undo);
            cultivate.EnglishFlatDescription = "Tills the ground inside the brush for planting without changing its height.";
            yield return cultivate;
            yield return Entry("replant_v2", "Replant", ToolFamily.Cultivator,
                "Brings the grass back inside the brush: removes dirt, paving and tilled soil.",
                HintKind.Adjust, HintKind.Shape, HintKind.Undo);
        }

        private static GameEntry Entry(string id, string label, ToolFamily tool, string description, params HintKind[] hints)
        {
            return new GameEntry { Id = id, Label = label, Tool = tool, EnglishDescription = description, Hints = hints };
        }
    }
}
