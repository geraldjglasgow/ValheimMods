using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How damage at a point holes a launched ship's hull (decided on its owner): the hull part nearest the hit takes it
    /// (<see cref="KwHullPartMap"/>). A hit within <see cref="Reach"/> of a hole already in that part widens the hole; any
    /// other adds to the part's wear, and when that reaches the threshold (<see cref="Threshold"/>: 6% of the ship's full
    /// health, at least 25) the part is holed there, the harder the hit the wider (<see cref="Smallest"/> to
    /// <see cref="FirstWidest"/> across its radius), and its wear starts again. A heavy shot going through a wall holes it
    /// at once (<see cref="Pierce"/>, <see cref="KwShipHitRouter"/>): its hole as wide as the ball's blow
    /// (<see cref="Through"/>), an exit wider than an entry, or the hole already there widened. Holes widen to at most
    /// <see cref="Widest"/>; a ship has at most <see cref="Most"/>, past which the nearest hole widens instead. The ship's
    /// own health falls as the game's does, apart from all this.
    /// </summary>
    public static class KwHullDamage
    {
        public const int Most = 32;
        public const float Smallest = 0.12f, FirstWidest = 0.35f, Widest = 0.45f;
        private const float Reach = 0.4f, WidenPer = 0.04f, FirstPer = 0.08f;

        /// <summary>The damage a part takes before it is holed, from the ship's full health.</summary>
        public static float Threshold(float health) => Mathf.Max(25f, 0.06f * health);

        /// <summary>
        /// Damage at a point of the ship's frame, coming along way (zero when not known; with face, the struck face's
        /// normal, <see cref="KwHullPartMap.Struck"/>), into its holes and wear (both changed); false if no part is near it.
        /// </summary>
        public static bool Take(KwHullPartMap map, List<KwHullHole> holes, Dictionary<int, float> wear, Vector3 local, Vector3 way, float damage, float health,
            bool face = false)
        {
            bool found = face ? map.Struck(local, way, out int part, out Vector3 place, out Vector3 normal) : map.Nearest(local, way, out part, out place, out normal);
            if (!found)
                return false;
            float threshold = Threshold(health);
            int near = Near(holes, part, place);
            if (near >= 0)
                return Widen(holes, near, damage / threshold);
            float worn = (wear.TryGetValue(part, out float before) ? before : 0f) + damage;
            if (worn < threshold)
            {
                wear[part] = worn;
                return true;
            }
            wear.Remove(part);
            if (holes.Count >= Most)
                return Widen(holes, Nearest(holes, place), worn / threshold);
            holes.Add(new KwHullHole(part, place, normal, Mathf.Clamp(Smallest + FirstPer * (worn / threshold - 1f), Smallest, FirstWidest)));
            return true;
        }

        /// <summary>
        /// A heavy shot through a part at a point of the ship's frame, the struck face's normal way (<see cref="KwHullPartMap.Struck"/>): holed there at once, the radius given, or
        /// the hole already within reach widened to it (and by a share more); false if no part is near it.
        /// </summary>
        public static bool Pierce(KwHullPartMap map, List<KwHullHole> holes, Dictionary<int, float> wear, Vector3 local, Vector3 way, float radius)
        {
            if (!map.Struck(local, way, out int part, out Vector3 place, out Vector3 normal))
                return false;
            int near = Near(holes, part, place);
            if (near >= 0)
                holes[near] = holes[near].Grown(Mathf.Min(Widest, Mathf.Max(radius, holes[near].Radius + WidenPer)));
            else if (holes.Count >= Most)
                Widen(holes, Nearest(holes, place), 1f);
            else
                holes.Add(new KwHullHole(part, place, normal, Mathf.Clamp(radius, Smallest, Widest)));
            wear.Remove(part);
            return true;
        }

        /// <summary>
        /// A heavy shot's hole through a wall, from its structure damage (a small iron ball's 108 about 0.15 m across its
        /// radius, a large ball's 396 about 0.31 m): an exit (the far side, the wood torn outward) a third wider.
        /// </summary>
        public static float Through(float damage, bool exit)
        {
            float entry = Mathf.Lerp(Smallest, FirstWidest, Mathf.Clamp01((damage - 60f) / 400f));
            return Mathf.Min(Widest, exit ? entry * 1.33f : entry);
        }

        // The hole of a part whose edge is within Reach of a place, nearest first; -1 with none.
        private static int Near(List<KwHullHole> holes, int part, Vector3 place)
        {
            int found = -1;
            float best = Reach;
            for (int i = 0; i < holes.Count; i++)
            {
                float edge = Vector3.Distance(holes[i].Place, place) - holes[i].Radius;
                if (holes[i].Part == part && edge < best)
                    (best, found) = (edge, i);
            }
            return found;
        }

        private static int Nearest(List<KwHullHole> holes, Vector3 place)
        {
            int found = 0;
            for (int i = 1; i < holes.Count; i++)
            {
                if ((holes[i].Place - place).sqrMagnitude < (holes[found].Place - place).sqrMagnitude)
                    found = i;
            }
            return found;
        }

        // A hole made wider by a share of WidenPer for every threshold's worth of damage, to Widest.
        private static bool Widen(List<KwHullHole> holes, int i, float thresholds)
        {
            holes[i] = holes[i].Grown(Mathf.Min(Widest, holes[i].Radius + WidenPer * thresholds));
            return true;
        }
    }
}
