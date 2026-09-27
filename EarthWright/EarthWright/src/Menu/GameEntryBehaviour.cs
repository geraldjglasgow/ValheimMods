using System.Collections.Generic;
using EarthWright.Actions;
using EarthWright.Terrain;

namespace EarthWright.Menu
{
    /// <summary>
    /// How the game's paved road and cultivate entries behave: level and paint as in the unmodded game, or paint only
    /// (the settings "Paved Road Levels" and "Cultivate Levels"). Applied to their <see cref="ToolAction"/>s at start and
    /// whenever a setting changes; a paint-only entry paints the whole brush instead of the game's smaller paint circle.
    /// </summary>
    public static class GameEntryBehaviour
    {
        private sealed class Original
        {
            public HeightOp Height;
            public bool UsesTarget;
            public float PaintRatio;
        }

        private static readonly Dictionary<string, Original> originals = new Dictionary<string, Original>();

        /// <summary>Whether the entry changes the height now (true for every entry without such a setting).</summary>
        public static bool Levels(string id)
        {
            if (id == "paved_road_v2")
                return MenuSettings.PavedRoadLevels.Value;
            if (id == "cultivate_v2")
                return MenuSettings.CultivateLevels.Value;
            return true;
        }

        public static void Apply()
        {
            Set("paved_road_v2");
            Set("cultivate_v2");
        }

        private static void Set(string id)
        {
            ToolAction action = ActionCatalog.ById(id);
            if (action == null)
                return;
            if (!originals.TryGetValue(id, out Original original))
            {
                original = new Original { Height = action.Height, UsesTarget = action.UsesTarget, PaintRatio = action.PaintRatio };
                originals[id] = original;
            }
            bool levels = Levels(id);
            action.Height = levels ? original.Height : HeightOp.None;
            action.UsesTarget = levels && original.UsesTarget;
            action.PaintRatio = levels ? original.PaintRatio : 1f;
        }
    }
}
