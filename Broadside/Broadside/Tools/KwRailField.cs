using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The railing tool's own rows: "Post spacing" in metres (<see cref="KwRailText"/>: 0.4 to 3 m, about a metre at
    /// first), in the hull railing's window (<see cref="KwSpanForm"/>, beside its material) and the roof railing's
    /// (<see cref="KwRoofRailTool"/>), where each of the roof's four edges is also on or off; and in the hull railing's,
    /// "Sides": both, port or starboard, each part given its own left or right for it by the way it runs
    /// (<see cref="KwRailBow"/>). The windows open on the spacing and edges last picked, and on both sides (a railing on
    /// one side is the exception: a cabin drawn in on one side keeps it); an edit opens them on the railing's own
    /// (<see cref="KwRailEdit"/>). Local to this player.
    /// </summary>
    public static class KwRailField
    {
        /// <summary>The spacing's key, and the roof railing edit's choice of keeping or closing its ladder gaps.</summary>
        public const string Posts = "posts", Gaps = "gaps";

        /// <summary>A hull railing's Sides row's key, and an edit's value of which way the part runs (+1 to the bow, -1 back), never a row.</summary>
        public const string HullSides = "sides", Runs = "runs";

        private static List<KwPlan>? ranFor;
        private static List<float> runs = new List<float>();

        private static readonly string[] OnOff = { "On", "Off" };

        /// <summary>The spacing last picked; about a metre, as railings were first built, before any.</summary>
        public static float LastSpacing { get; private set; } = KwRailText.Usual;

        /// <summary>The roof's edges last given a rail, as letters ("faps": all four before any).</summary>
        public static string LastSides { get; private set; } = new string(KwRoofRail.Letters);

        /// <summary>The spacing row, opened on a value.</summary>
        public static KwField Spacing(float value) =>
            KwField.Number(Posts, "Post spacing", value, KwRailText.Least, KwRailText.Most, 0.05f);

        /// <summary>The Sides row (both, port, starboard), opened on one of them.</summary>
        public static KwField SidesRow(int index) => KwField.Choice(HullSides, "Sides", KwRailBow.Labels, index);

        /// <summary>The rows a tool's span window adds: the spacing and the sides (both) for the railing tool, none for another.</summary>
        public static List<KwField> Fields(string tool) =>
            tool == KwBiomeCost.Rail ? new List<KwField> { Spacing(LastSpacing), SidesRow(0) } : new List<KwField>();

        /// <summary>
        /// The window's spacing and sides given to the hull railings planned (their text's railing segment, each part its
        /// own left or right for port or starboard), so the preview and the parts built show them; the spacing kept as the
        /// last picked; nothing for another tool.
        /// </summary>
        public static void Apply(KwFormValues values, string tool, List<KwPlan> plans)
        {
            if (tool != KwBiomeCost.Rail || !values.Has(Posts))
                return;
            LastSpacing = values.Number(Posts);
            string name = values.Has(HullSides) ? KwRailBow.Names[Mathf.Clamp(values.ChoiceIndex(HullSides), 0, 2)] : KwRailBow.Names[0];
            List<float> ways = name == KwRailBow.Names[0] ? new List<float>() : Ways(plans);
            for (int i = 0; i < plans.Count; i++)
                plans[i].Span.Rail = KwRailText.Write(LastSpacing, i < ways.Count ? KwRailBow.Letters(name, ways[i]) : KwRailText.Both);
        }

        // Which way each planned part runs to the bow, found once for the window's plans (the pieces round them read once).
        private static List<float> Ways(List<KwPlan> plans)
        {
            if (ranFor == plans && runs.Count == plans.Count)
                return runs;
            ranFor = plans;
            float first = plans.Count > 0 ? KwRailBow.Runs(plans[0].Position, plans[0].Direction) : 1f;
            runs = plans.ConvertAll(plan => Vector3.Dot(plan.Direction, plans[0].Direction) >= 0f ? first : -first);
            return runs;
        }

        /// <summary>Hull railings hovered before their window opens, given the spacing last picked; another tool's plans as they are.</summary>
        public static List<KwPlan> Spaced(string tool, List<KwPlan> plans)
        {
            if (tool == KwBiomeCost.Rail)
                plans.ForEach(plan => plan.Span.Rail = KwRailText.Write(LastSpacing));
            return plans;
        }

        /// <summary>A roof's four edges as rows, each on or off as the letters say.</summary>
        public static List<KwField> SideRows(string on) =>
            KwRoofRail.Sides.Select(side => KwField.Choice(side, Label(side), OnOff, on.IndexOf(side[0]) >= 0 ? 0 : 1)).ToList();

        /// <summary>The edges the window has on, as letters ("" for none, or a window without the rows).</summary>
        public static string Sides(KwFormValues values) =>
            new string(KwRoofRail.Sides.Where(side => values.Has(side) && values.ChoiceIndex(side) == 0).Select(side => side[0]).ToArray());

        /// <summary>The roof railing window's spacing and edges kept as the last picked.</summary>
        public static void Keep(KwFormValues values)
        {
            if (values.Has(Posts))
                LastSpacing = values.Number(Posts);
            LastSides = Sides(values);
        }

        /// <summary>The roof railing edit's row on its ladder gaps: kept, or closed (the rail runs on through them).</summary>
        public static KwField GapRow(int count) =>
            KwField.Choice(Gaps, count == 1 ? "Gap for a ladder" : $"Gaps for ladders ({count})", new[] { "Keep", "Close" }, 0);

        private static string Label(string side) => char.ToUpperInvariant(side[0]) + side.Substring(1) + " edge";
    }
}
