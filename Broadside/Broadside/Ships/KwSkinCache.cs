using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The hull boxes (<see cref="KwHullSkin"/>) worked out once for each part's shape: a ship's mirrored bow and stern
    /// parts, its repeated middle parts and a ship loaded again cost nothing more. Keyed by the part's stations (to the
    /// millimetre), which of its edges go on and its openings; at most <see cref="Kept"/> shapes, all let go when full. The
    /// lists handed out are shared and never changed.
    /// </summary>
    public static class KwSkinCache
    {
        private const int Kept = 128;
        private static readonly Dictionary<string, IReadOnlyList<KwSkinBox>> made = new Dictionary<string, IReadOnlyList<KwSkinBox>>();

        /// <summary>The boxes for a key, made the first time.</summary>
        public static IReadOnlyList<KwSkinBox> Of(string key, System.Func<List<KwSkinBox>> make)
        {
            if (made.TryGetValue(key, out IReadOnlyList<KwSkinBox> boxes))
                return boxes;
            if (made.Count >= Kept)
                made.Clear();
            return made[key] = make();
        }

        /// <summary>A part's shape as a key: what it is, its edges that go on, its stations and its openings.</summary>
        public static string Key(string kind, int edges, List<Station> stations, List<KwOpening> openings)
        {
            StringBuilder key = new StringBuilder(kind).Append(edges);
            foreach (Station s in stations)
            {
                key.Append('|').Append(Mm(s.X));
                foreach (Vector2 p in s.Half)
                    key.Append(',').Append(Mm(p.x)).Append(',').Append(Mm(p.y));
            }
            foreach (KwOpening o in openings)
            {
                key.Append('#').Append(o.Id).Append(o.Wall).Append(o.Shape).Append(Mm(o.U)).Append(',').Append(Mm(o.Sill));
                key.Append(',').Append(Mm(o.Width)).Append(',').Append(Mm(o.Height));
            }
            return key.ToString();
        }

        private static int Mm(float metres) => Mathf.RoundToInt(metres * 1000f);
    }
}
