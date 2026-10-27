using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How a stem's keel ends: how high it climbs, the top its post allows the hull, the post's depth along the keel, and
    /// where along the keel end (a share of its length, 0 at its start) a part ending there starts: a rib may stand on the
    /// keel end itself, and the part then follows the rest of its curve; and how far under its rib's height the hull's top
    /// edge starts there (<see cref="Low"/>: the level keel line under that rib, <see cref="KwSeat"/>).
    /// </summary>
    public readonly struct KwStemEnd
    {
        public KwStemEnd(float rise, float postTop, float postDepth, float from = 0f, float low = 0f)
        {
            Rise = rise;
            PostTop = postTop;
            PostDepth = postDepth;
            From = from;
            Low = low;
        }

        public float Rise { get; }
        public float PostTop { get; }
        public float PostDepth { get; }
        public float From { get; }

        /// <summary>How far the hull's top edge starts under the rib's height over the rib's foot: as far as the rib stands up its keel (0 unseated).</summary>
        public float Low { get; }

        /// <summary>The whole keel end's length, from a part's length that starts at <see cref="From"/>.</summary>
        public float Whole(float length) => length / Mathf.Max(1f - From, 0.05f);
    }
}
