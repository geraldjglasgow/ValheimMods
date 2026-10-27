using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The painted bands in the planking tool's window (<see cref="KwSpanForm"/>): for each of two bands the strake it
    /// starts at, counted up from 1, the strake whose foot is level with the keel line on the hull's sides (0 for no band;
    /// the unit says the highest strake the parts planned reach, <see cref="KwGirth"/>), how many strakes wide it is, and
    /// its colour (<see cref="KwPaint"/>). The side strakes run level, so a strake counted from the keel line is the same
    /// height on every part of the hull and a band runs on unbroken from part to part; the bottom's strakes under the
    /// first are not painted. The window opens on the bands last picked; OK saves them in each part's text as its paint
    /// segment. An edit of planking opens the rows on the part's own bands (<see cref="KwPlankingEdit"/>). Local to this
    /// player.
    /// </summary>
    public static class KwBandField
    {
        /// <summary>How many bands the window offers.</summary>
        public const int Bands = 2;

        private const int Most = 40;
        private static readonly (int Strake, int Wide, int Colour)[] last = { (0, 1, 0), (0, 1, 1) };

        /// <summary>The band rows for a tool's window: two bands for planking, none for any other tool.</summary>
        public static List<KwField> Fields(string tool, List<KwPlan> plans) =>
            tool == "planking" ? Rows(Strakes(plans), last) : new List<KwField>();

        /// <summary>The rows for these bands (strake from 1, 0 for none; strakes wide; colour), the unit naming the highest strake.</summary>
        public static List<KwField> Rows(int strakes, IReadOnlyList<(int Strake, int Wide, int Colour)> bands)
        {
            List<KwField> fields = new List<KwField>();
            string of = $"of {strakes}";
            for (int i = 0; i < bands.Count; i++)
            {
                fields.Add(KwField.Number(Key(i, "at"), $"Band {i + 1} at strake", bands[i].Strake, 0f, Most, 1f, of));
                fields.Add(KwField.Number(Key(i, "wide"), $"Band {i + 1} strakes wide", bands[i].Wide, 1f, KwPaint.MaxWide, 1f, ""));
                fields.Add(KwField.Choice(Key(i, "colour"), $"Band {i + 1} colour", KwPaint.Labels, bands[i].Colour));
            }
            return fields;
        }

        /// <summary>The paint segment the window's bands make, kept as the ones last picked; "" for no band, or another tool.</summary>
        public static string Picked(KwFormValues values, string tool)
        {
            if (tool != "planking")
                return "";
            List<KwBand> bands = new List<KwBand>();
            for (int i = 0; i < last.Length; i++)
            {
                int at = Mathf.RoundToInt(values.Number(Key(i, "at"))), wide = Mathf.RoundToInt(values.Number(Key(i, "wide")));
                last[i] = (at, wide, values.ChoiceIndex(Key(i, "colour")));
                if (at > 0)
                    bands.Add(new KwBand(at - 1, at - 2 + Mathf.Max(1, wide), last[i].Colour));
            }
            return KwPaint.Write(bands);
        }

        /// <summary>A band row's key: "band0at", "band1colour".</summary>
        public static string Key(int band, string what) => $"band{band}{what}";

        // The highest strake the planned parts reach, counted from 1 at the keel line.
        private static int Strakes(List<KwPlan> plans)
        {
            int most = 1;
            foreach (KwPlan plan in plans)
                most = Mathf.Max(most, Strakes(plan.Span));
            return most;
        }

        /// <summary>The highest strake a planking span reaches, counted from 1 at the keel line.</summary>
        public static int Strakes(KwSpan span)
        {
            KwDef def = span.Def("planking");
            return def.Stations.Count > 1 ? new KwGirth(def.Stations, def.Level).Highest + 1 : 1;
        }
    }
}
