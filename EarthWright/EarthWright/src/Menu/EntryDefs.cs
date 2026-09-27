using System.Collections.Generic;
using System.Linq;
using EarthWright.Actions;
using EarthWright.Terrain;

namespace EarthWright.Menu
{
    /// <summary>
    /// EarthWright's own entries in menu order: the hoe's (after the game's four terrain entries), the cultivator's (after
    /// Cultivate and Replant). Starting values (radius, amount, hardness) are what the brush uses the
    /// first time an entry is selected; the player changes them with the brush keys.
    /// </summary>
    public static class EntryDefs
    {
        public static readonly IReadOnlyList<EntryDef> All = HoeBrushEntries().Concat(HoeSpecialEntries()).Concat(OtherToolEntries()).ToList();

        private static readonly Dictionary<string, EntryDef> byId = All.ToDictionary(d => d.Id);

        public static EntryDef Get(string id) => id != null && byId.TryGetValue(id, out EntryDef def) ? def : null;

        public static IEnumerable<EntryDef> For(ToolFamily tool) => All.Where(d => d.Tool == tool);

        private static IEnumerable<EntryDef> HoeBrushEntries()
        {
            yield return Def("ew_lower", "Lower", "raise_v2", "Lower ground",
                "Lowers the ground inside the brush by the set amount. Repeated swings dig down to the dig limit.",
                Lower("ew_lower", ToolFamily.Hoe, 2f));
            yield return Def("ew_smooth", "Smooth", "mud_road_v2", "Smooth ground",
                "Evens out bumps and ridges inside the brush without flattening the slope. The strength sets how much each swing smooths.",
                new ToolAction { Id = "ew_smooth", Height = HeightOp.Smooth, Strength = 0.5f, BaseRadius = 3f, Hardness = 0.2f });
            yield return Def("ew_paint", "Paint", "path_v2", "Paint ground",
                "Paints the ground inside the brush without changing its height. The paint key picks the paint: dirt, paved, tilled, grass, the biome's own ground or vegetation.",
                new ToolAction { Id = "ew_paint", Paint = PaintOp.Paved, BaseRadius = 2f, Hardness = 1f });
            yield return Def("ew_reset", "Reset", "mud_road_v2", "Reset ground",
                "Returns the ground inside the brush to the world's original height and texture. Buildings, trees and rocks are left alone.",
                new ToolAction { Id = "ew_reset", Height = HeightOp.Reset, Paint = PaintOp.Original, BaseRadius = 3f, Hardness = 0.8f });
        }

        private static IEnumerable<EntryDef> HoeSpecialEntries()
        {
            yield return Def("ew_ramp", "Ramp", "path_v2", "Ramp",
                "Builds a ramp: click the start, click the end, then click a third time to cut or fill it. The ramp is as wide as the brush.",
                Special("ew_ramp", "ramp", ToolFamily.Hoe, 2f), HintKind.Profile, HintKind.Adjust, HintKind.Back, HintKind.QuickRamp, HintKind.Paint);
            yield return Def("ew_road", "Road", "path_v2", "Road",
                "Plans a road: each click places a waypoint, and the carve key cuts a smooth road through them, as wide as the brush. The line is coloured by how steep it is for carts.",
                Special("ew_road", "road", ToolFamily.Hoe, 2f), HintKind.Carve, HintKind.CarvePaved, HintKind.Adjust, HintKind.Back, HintKind.Paint);
            yield return Def("ew_groundbreaker", "Groundbreaker", "mud_road_v2", "Groundbreaker",
                "One swing does it all: clears trees, rocks and shrubs inside the brush (while clearing is switched on), levels the ground toward the target height and paves it.",
                Groundbreaker());
            yield return Def("ew_clear", "Clear Objects", "path_v2", "Clear objects",
                "Removes trees, stumps, logs, shrubs, rocks and pickables inside the brush. Warded ground is left alone.",
                Special("ew_clear", "clear", ToolFamily.Hoe, 4f));
            yield return Def("ew_terraform", "Terraform", "mud_road_v2", "Terraform (admin)",
                "Admins only: sets every point inside the brush to the target height in one swing, past the height limits when the server allows it.",
                Terraform());
        }

        private static IEnumerable<EntryDef> OtherToolEntries()
        {
            yield return Def("ew_till", "Till", "cultivate_v2", "Till",
                "Tills the ground inside the brush for planting without changing its height.",
                new ToolAction { Id = "ew_till", Family = ToolFamily.Cultivator, Paint = PaintOp.Cultivated, BaseRadius = 3f, Hardness = 1f });
            yield return Def("ew_uproot", "Uproot", "replant_v2", "Uproot",
                "Pulls up natural pickables inside the brush: berry bushes, mushrooms, flowers, thistle and the like.",
                Special("ew_uproot", "uproot", ToolFamily.Cultivator, 3f));
        }

        /// <summary>An entry; its icon is named after the id without "ew_" (menu_lower.png for ew_lower).</summary>
        private static EntryDef Def(string id, string label, string basePrefab, string name, string description,
            ToolAction action, params HintKind[] hints)
        {
            return new EntryDef
            {
                Id = id, Label = label, BasePrefab = basePrefab, Icon = id.Substring(3), EnglishName = name,
                EnglishDescription = description, Action = action, Hints = hints,
            };
        }

        /// <summary>Lowering digs with a soft edge (a smooth hollow rather than the raise's flat top) and paints dirt a little wider than the dip.</summary>
        private static ToolAction Lower(string id, ToolFamily tool, float radius)
        {
            return new ToolAction
            {
                Id = id, Family = tool, Height = HeightOp.Lower, Paint = PaintOp.Dirt, BaseRadius = radius,
                PaintRatio = 1.25f, Amount = 1f, UsesAmount = true, Hardness = 0f,
            };
        }

        private static ToolAction Special(string id, string key, ToolFamily tool, float radius)
        {
            return new ToolAction { Id = id, Family = tool, Special = key, BaseRadius = radius, Hardness = 1f };
        }

        /// <summary>The Clearing module's handler clears, then levels and paves with this action's values.</summary>
        private static ToolAction Groundbreaker()
        {
            return new ToolAction
            {
                Id = "ew_groundbreaker", Special = "groundbreaker", Height = HeightOp.Level, Paint = PaintOp.Paved,
                UsesTarget = true, BaseRadius = 3f, MaxStep = 1f, Hardness = 0.5f,
            };
        }

        private static ToolAction Terraform()
        {
            return new ToolAction
            {
                Id = "ew_terraform", Height = HeightOp.Level, Style = LevelStyle.Instant, UsesTarget = true,
                AdminOnly = true, BaseRadius = 5f, MaxStep = 1000f, Hardness = 0.8f,
            };
        }
    }
}
