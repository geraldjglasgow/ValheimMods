using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A planking part's measures round and along its hull, which its strakes are cut and its texture laid by
    /// (<see cref="KwStrakes"/>). Round a station each point has a measure (<see cref="Measure"/>): on the hull's upright
    /// sides its height over the level keel line, on its bottom the metres round the section, the two blended where the
    /// section turns from one to the other (the turn of the bilge). Strake k lies between k and k + 1 times
    /// <see cref="KwUv.StrakeWidth"/> of that measure, so on the sides every strake runs level fore and aft at the same
    /// height on every part of the ship and ends cleanly where the rising keel or a stem cuts it, the top one where the
    /// sheer does; on the bottom the strakes stay one width round, never growing wide. k counts up from the keel line's
    /// height (strake 0 has its foot there on the sides; the bottom's lowest strakes are below 0), the same on every part.
    /// Along, each strake's length from the part's first station to each station, measured through the strake's middle in
    /// three dimensions; how far along its strake a point lies is counted the way the strake runs at its station
    /// (<see cref="Along(int, int, Vector3)"/>). Two parts meeting at one rib measure it the same, and come out there at
    /// the same place along the texture (<see cref="KwPlankShift"/>): at a station on a rib a point lies as far along as the
    /// station itself (the strake's way not counted, as the neighbour's would differ), and a part between two ribs has
    /// each strake's length to its last station come out at the part's own length (the strake's lengths stretched or
    /// shrunk evenly, a hair on a straight run), so the next part's planks start where these end.
    /// </summary>
    public sealed class KwGirth
    {
        // The section's lean from flat (degrees) where its measure starts turning from round to height, and where it is height alone.
        private const float Bottom = 35f, Side = 55f;

        private readonly List<Station> stations;
        private readonly float[][] round;
        private readonly float[][] along;
        private readonly Vector3[][] middles, ways;
        private readonly bool ribStart, ribEnd;

        /// <summary>
        /// The planking on these stations, its sides' strakes level from the keel line at height level (the part's frame);
        /// ribStart and ribEnd when it meets its neighbour at a rib at its first station and at its last.
        /// </summary>
        public KwGirth(List<Station> stations, float level = 0f, bool ribStart = false, bool ribEnd = false)
        {
            this.stations = stations;
            (this.ribStart, this.ribEnd) = (ribStart, ribEnd);
            round = stations.ConvertAll(s => Measure(s.Half, level)).ToArray();
            (Lowest, Highest) = (int.MaxValue, int.MinValue);
            foreach (float[] m in round)
            {
                Lowest = Mathf.Min(Lowest, Mathf.FloorToInt(m[0] / KwUv.StrakeWidth + 1e-4f));
                Highest = Mathf.Max(Highest, Mathf.CeilToInt(m[m.Length - 1] / KwUv.StrakeWidth - 1e-4f) - 1);
            }
            Highest = Mathf.Max(Highest, Lowest);
            middles = new Vector3[stations.Count][];
            for (int s = 0; s < stations.Count; s++)
                middles[s] = Middles(s);
            along = Lengths();
            ways = Ways();
        }

        /// <summary>A planking part's measures, its ends at ribs as its span says (<see cref="KwDef.RibStart"/>, <see cref="KwDef.RibEnd"/>).</summary>
        public static KwGirth Of(KwDef def) => new KwGirth(def.Stations, def.Level, def.RibStart, def.RibEnd) { Holes = KwOpeningSkin.Holes(def.Openings) };

        /// <summary>
        /// The openings cut out of the strakes (<see cref="KwOpeningRing"/>: their outlines in (x, z), each along its tunnel),
        /// on the port side and on the starboard; null for none.
        /// </summary>
        public (List<KwOpening>? Port, List<KwOpening>? Starboard) Holes { get; set; }

        /// <summary>The openings on one side (1 port, -1 starboard); null for none.</summary>
        public List<KwOpening>? HolesOn(float side) => side > 0f ? Holes.Port : Holes.Starboard;

        /// <summary>The lowest strake any station has (often below 0, on the bottom) and the highest.</summary>
        public int Lowest { get; }
        public int Highest { get; }

        public Station Station(int s) => stations[s];

        /// <summary>Station s's measure at each of its section's points, from its keel to its sheer.</summary>
        public float[] Of(int s) => round[s];

        /// <summary>Station s's measure at its keel and at its sheer.</summary>
        public float Keel(int s) => round[s][0];
        public float Sheer(int s) => round[s][round[s].Length - 1];

        /// <summary>How far strake k runs along the hull from the part's first station to station s, in metres.</summary>
        public float Along(int s, int k) => along[s][k - Lowest];

        /// <summary>
        /// How far along strake k a point of station s's section lies (kit, port side): the strake's length to the station,
        /// and the point's offset from the strake's middle there the way the strake runs, but on a rib's station.
        /// </summary>
        public float Along(int s, int k, Vector3 p) =>
            along[s][k - Lowest] + (OnRib(s) ? 0f : Vector3.Dot(p - middles[s][k - Lowest], ways[s][k - Lowest]));

        // Whether station s stands on a rib a neighbouring part shares.
        private bool OnRib(int s) => (s == 0 && ribStart) || (s == stations.Count - 1 && ribEnd);

        /// <summary>
        /// A section's measure (half width, height) at each of its points, worked down from its sheer: the sheer's height
        /// over the level keel line, less each segment's rise where the section stands steeper than <see cref="Side"/>
        /// degrees, its length round where flatter than <see cref="Bottom"/>, and a blend of the two between.
        /// </summary>
        public static float[] Measure(Vector2[] half, float level)
        {
            float[] m = new float[half.Length];
            m[half.Length - 1] = half[half.Length - 1].y - level;
            for (int i = half.Length - 1; i > 0; i--)
                m[i - 1] = m[i] - Step(half[i] - half[i - 1]);
            return m;
        }

        // One segment's measure: its rise on an upright side, its length on a flat bottom, blended smoothly between.
        private static float Step(Vector2 d)
        {
            float rise = Mathf.Abs(d.y), length = d.magnitude;
            float lean = Mathf.Atan2(rise, Mathf.Abs(d.x)) * Mathf.Rad2Deg;
            float w = KwJoints.Smoothstep(Mathf.Clamp01((lean - Bottom) / (Side - Bottom)));
            return w * rise + (1f - w) * length;
        }

        /// <summary>The point of a section at a measure, kept between its keel and its sheer.</summary>
        public static Vector2 At(Vector2[] half, float[] measure, float g)
        {
            int j = Segment(measure, g);
            float span = measure[j + 1] - measure[j];
            return span < 1e-6f ? half[j] : Vector2.Lerp(half[j], half[j + 1], Mathf.Clamp01((g - measure[j]) / span));
        }

        /// <summary>The section segment (its lower point's index) a measure falls on, the first or last past the ends.</summary>
        public static int Segment(float[] measure, float g)
        {
            int j = 0;
            while (j + 2 < measure.Length && g > measure[j + 1])
                j++;
            return j;
        }

        // Each strake's length along the hull to each station, station by station through its middle; between two ribs,
        // stretched to come out at the part's length.
        private float[][] Lengths()
        {
            int count = Highest - Lowest + 1;
            float[][] lengths = new float[stations.Count][];
            lengths[0] = new float[count];
            for (int s = 1; s < stations.Count; s++)
            {
                lengths[s] = new float[count];
                for (int i = 0; i < count; i++)
                    lengths[s][i] = lengths[s - 1][i] + Vector3.Distance(middles[s - 1][i], middles[s][i]);
            }
            if (ribStart && ribEnd)
                Fit(lengths);
            return lengths;
        }

        // Every strake's lengths stretched evenly so each comes out at the part's own length at its last station.
        private void Fit(float[][] lengths)
        {
            int last = stations.Count - 1;
            float part = stations[last].X - stations[0].X;
            for (int i = 0; i < lengths[last].Length; i++)
            {
                float own = lengths[last][i], f = own > 1e-4f ? part / own : 1f;
                for (int s = 1; s <= last; s++)
                    lengths[s][i] *= f;
            }
        }

        // The way each strake runs at each station: from its middle at the station before to its middle at the one after
        // (the part's ends from their one neighbour).
        private Vector3[][] Ways()
        {
            Vector3[][] result = new Vector3[stations.Count][];
            for (int s = 0; s < stations.Count; s++)
            {
                int before = Mathf.Max(s - 1, 0), after = Mathf.Min(s + 1, stations.Count - 1);
                result[s] = new Vector3[Highest - Lowest + 1];
                for (int i = 0; i < result[s].Length; i++)
                {
                    Vector3 way = middles[after][i] - middles[before][i];
                    result[s][i] = way.sqrMagnitude > 1e-10f ? way.normalized : new Vector3(1f, 0f, 0f);
                }
            }
            return result;
        }

        // Every strake's middle round station s (kit, port side): halfway between its edges as the keel and the sheer cut
        // them; the keel or the sheer for a strake the section does not reach.
        private Vector3[] Middles(int s)
        {
            Vector3[] result = new Vector3[Highest - Lowest + 1];
            float keel = Keel(s), sheer = Sheer(s), w = KwUv.StrakeWidth;
            for (int i = 0; i < result.Length; i++)
            {
                int k = Lowest + i;
                float lo = Mathf.Clamp(k * w, keel, sheer), hi = Mathf.Clamp((k + 1) * w, keel, sheer);
                Vector2 p = At(stations[s].Half, round[s], (lo + hi) / 2f);
                result[i] = new Vector3(stations[s].X, p.x, p.y);
            }
            return result;
        }
    }
}
