using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How a mast's sails are placed on it (<see cref="KwMastSpec"/>). The square sails stack from the masthead down: the
    /// highest yard 0.45 m under the masthead, each sail's foot on the yard below it (its sheets made fast to that yard's
    /// ends), the course's foot at least 0.15 m over the deck. The sails share the mast's height from the bottom up: the
    /// course gets what it asks for first, then the topsail, and so on, each leaving every sail above it half a metre, so a
    /// sail that does not fit is cut, highest first. A sail along the ship sets from just under the course's yard (or 0.3 m
    /// under the masthead with no square sail) down to its boom, which stays 0.3 m over the deck. A mast too short for its
    /// sails at their least (half a metre each) is raised, to the next half metre, to carry them. What had to change is
    /// said in one or two sentences (<see cref="KwMastSpec.Note"/>). A cut sail's height is rounded down to the centimetre, so a mast
    /// saved and read again places its sails the same. Every peer places them the same from the text.
    /// </summary>
    internal static class KwSailFit
    {
        private const float Head = 0.45f, Foot = 0.15f, Gap = 0.02f, AlongHead = 0.3f, Boom = 0.3f, Under = 0.1f;

        public static KwMastSpec Fit(float asked, IEnumerable<KwSailSpec> sails)
        {
            (List<KwSailSpec> squares, KwSailSpec? along) = Sorted(sails);
            float mast = Mathf.Min(asked, KwSailSpec.MaxMast);
            float needed = Needed(mast, squares.Count, along.HasValue);
            List<string> notes = new List<string>();
            if (needed > mast + 0.001f)
            {
                mast = Mathf.Min(Mathf.Ceil(needed * 2f) / 2f, KwSailSpec.MaxMast);
                notes.Add($"The mast is raised to {mast:0.##} m to carry these sails.");
            }
            List<KwSailSpec> placed = Stack(mast, squares, along.HasValue, notes);
            if (along.HasValue)
                placed.Add(Along(mast, along.Value, placed, notes));
            return new KwMastSpec(mast, placed, string.Join(" ", notes));
        }

        /// <summary>How tall a square sail put on top of the stack may be without cutting another sail (0 with the stack full).</summary>
        public static float Room(KwMastSpec mast)
        {
            int n = mast.Squares;
            if (n >= KwMastSpec.MostSquare)
                return 0f;
            float m = mast.Mast, used = 0f;
            for (int k = 0; k < n; k++)
                used += mast.Sails[k].Height;
            float room = m - Head - Foot - Gaps(m, n + 1) - used;
            if (n > 0 && mast.Sails.Count > n)
                room = Mathf.Min(room, m - Head - Gaps(m, n + 1) - CourseYardLeast(m) - (used - mast.Sails[0].Height));
            return Mathf.Max(0f, room);
        }

        /// <summary>How tall a sail along the ship may be under the mast's square sails (or its head with none).</summary>
        public static float AlongRoom(KwMastSpec mast)
        {
            float head = mast.Squares > 0 ? mast.Sails[0].Top - Spar(mast.Mast, 0) - Under : mast.Mast - AlongHead;
            return Mathf.Max(0f, head - Boom);
        }

        /// <summary>A yard's (or a boom's) radius: the mast's radius scaled down, a little thinner for each sail up the stack.</summary>
        public static float Spar(float mast, int tier) => KwSailLayout.RadiusOf(mast) * 0.62f * Mathf.Pow(0.85f, tier);

        // The square sails in their order (at most the stack's four) and the first sail along the ship; any others go.
        private static (List<KwSailSpec>, KwSailSpec?) Sorted(IEnumerable<KwSailSpec> sails)
        {
            List<KwSailSpec> squares = new List<KwSailSpec>();
            KwSailSpec? along = null;
            foreach (KwSailSpec sail in sails)
            {
                if (sail.Square && squares.Count < KwMastSpec.MostSquare)
                    squares.Add(sail);
                else if (!sail.Square && !along.HasValue)
                    along = sail;
            }
            return (squares, along);
        }

        // The least mast that carries these sails at their least height.
        private static float Needed(float mast, int squares, bool along)
        {
            if (squares == 0)
                return AlongHead + Boom + KwSailSpec.Least;
            float stack = Head + Foot + squares * KwSailSpec.Least + Gaps(mast, squares);
            float upper = Head + (squares - 1) * KwSailSpec.Least + Gaps(mast, squares);
            return along ? Mathf.Max(stack, upper + CourseYardLeast(mast)) : stack;
        }

        // The room the yards and the gaps between the stacked sails take up.
        private static float Gaps(float mast, int squares)
        {
            float gaps = 0f;
            for (int k = 1; k < squares; k++)
                gaps += Spar(mast, k) + Gap + Spar(mast, k - 1);
            return gaps;
        }

        // How high the course's yard must be at least for a sail along the ship to set under it.
        private static float CourseYardLeast(float mast) => Spar(mast, 0) + Under + Boom + KwSailSpec.Least;

        // The square sails' heights from the course up (the course first, each leaving the sails above it their least),
        // then their yards from the masthead down. With a sail along the ship, the sails over the course leave its yard
        // high enough for that sail.
        private static List<KwSailSpec> Stack(float mast, List<KwSailSpec> squares, bool along, List<string> notes)
        {
            int n = squares.Count;
            float[] heights = new float[n];
            if (n == 0)
                return new List<KwSailSpec>();
            float room = mast - Head - Foot - Gaps(mast, n);
            heights[0] = Cut(squares[0].Height, room - (n - 1) * KwSailSpec.Least);
            float upper = room - heights[0];
            if (along)
                upper = Mathf.Min(upper, mast - Head - Gaps(mast, n) - CourseYardLeast(mast));
            for (int k = 1; k < n; k++)
            {
                heights[k] = Cut(squares[k].Height, upper - (n - 1 - k) * KwSailSpec.Least);
                upper -= heights[k];
            }
            Told(squares, heights, notes);
            return Yards(mast, squares, heights);
        }

        // What the stack had to cut, in one sentence: the lowest sail cut, and the ones above it when they were cut too.
        private static void Told(List<KwSailSpec> squares, float[] heights, List<string> notes)
        {
            int first = -1, cut = 0;
            for (int k = 0; k < heights.Length; k++)
            {
                if (heights[k] >= squares[k].Height - 0.0005f)
                    continue;
                first = first < 0 ? k : first;
                cut++;
            }
            if (first >= 0)
                notes.Add($"{KwMastSpec.SquareName(first)} cut to {heights[first]:0.##} m{(cut > 1 ? " and the sails above it with it" : "")}: the sails fill the mast up to 0.45 m under its head.");
        }

        // Each square sail at its height on its yard: the highest under the masthead, each lower one under the sail above.
        private static List<KwSailSpec> Yards(float mast, List<KwSailSpec> squares, float[] heights)
        {
            KwSailSpec[] placed = new KwSailSpec[squares.Count];
            float yard = mast - Head;
            for (int k = squares.Count - 1; k >= 0; k--)
            {
                placed[k] = squares[k].Placed(mast, heights[k], yard, k);
                if (k > 0)
                    yard -= Spar(mast, k) + Gap + heights[k] + Spar(mast, k - 1);
            }
            return new List<KwSailSpec>(placed);
        }

        // The sail along the ship: its head under the course's yard (or under the masthead), its boom over the deck.
        private static KwSailSpec Along(float mast, KwSailSpec sail, List<KwSailSpec> squares, List<string> notes)
        {
            float head = squares.Count > 0 ? squares[0].Top - Spar(mast, 0) - Under : mast - AlongHead;
            float height = Cut(sail.Height, head - Boom);
            if (height < sail.Height - 0.0005f)
                notes.Add($"{(sail.Shape == 1 ? "Triangle sail" : "Gaff sail")} cut to {height:0.##} m {(squares.Count > 0 ? "to set under the course's yard" : "to keep its boom over the deck")}.");
            return sail.Placed(mast, height, head, 0);
        }

        // A sail's height within the room it has (at least the least a sail may be), rounded down to the centimetre when cut.
        private static float Cut(float height, float most) =>
            height <= most + 0.0005f ? Mathf.Max(height, KwSailSpec.Least) : Mathf.Max(Mathf.Floor(most * 100f + 0.001f) / 100f, KwSailSpec.Least);
    }
}
