using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The two outlines an opening's frame is drawn between (<see cref="KwOpeningFrames"/>): the cut and the frame's outer
    /// edge, in the wall's (u, z), each edge cut into steps of at most <see cref="Step"/> (both outlines alike, so a step of
    /// one faces the same step of the other), so a frame bends with a curving wall instead of spanning it on a chord. Each
    /// step knows its board (<see cref="Board"/>): a frame is a board along each straight side and one bent along a curve
    /// (a round port's ring, an arch's head), a new board starting at a corner or where a straight side meets a curve. A
    /// doorway's steps along its floor are marked (<see cref="Floor"/>): nothing is drawn there. Pure.
    /// </summary>
    public sealed class KwFrameRing
    {
        /// <summary>The longest step along an edge.</summary>
        public const float Step = 0.08f;

        /// <summary>How much an outline turns at a corner, at least (degrees); less is a curve's bend.</summary>
        private const float Corner = 30f, Bend = 0.5f;

        private KwFrameRing(List<Vector2> inner, List<Vector2> outer, List<int> board, List<bool> floor, int first)
        {
            (Inner, Outer, Board, Floor, First) = (inner, outer, board, floor, first);
        }

        /// <summary>The cut's points and the outer edge's, as many each; step i runs from point i to point i + 1 (round).</summary>
        public List<Vector2> Inner { get; }
        public List<Vector2> Outer { get; }

        /// <summary>Each step's board, numbered round from <see cref="First"/>.</summary>
        public List<int> Board { get; }

        /// <summary>Whether a step lies along a doorway's floor.</summary>
        public List<bool> Floor { get; }

        /// <summary>The step a board starts at (0 for a ring that is one board all round).</summary>
        public int First { get; }

        public int Count => Inner.Count;

        /// <summary>The steps in order from the first board's start.</summary>
        public IEnumerable<int> Steps()
        {
            for (int k = 0; k < Count; k++)
                yield return (First + k) % Count;
        }

        /// <summary>A frame's rings from its cut and outer outline (as many points each); a doorway's floor at its sill.</summary>
        public static KwFrameRing Of(List<Vector2> inner, List<Vector2> outer, bool door, float sill)
        {
            List<Vector2> a = new List<Vector2>(), c = new List<Vector2>();
            List<int> edge = new List<int>();
            for (int i = 0; i < inner.Count; i++)
            {
                int j = (i + 1) % inner.Count;
                int n = Steps(inner[i], inner[j], outer[i], outer[j]);
                for (int k = 0; k < n; k++)
                {
                    a.Add(Vector2.Lerp(inner[i], inner[j], (float)k / n));
                    c.Add(Vector2.Lerp(outer[i], outer[j], (float)k / n));
                    edge.Add(i);
                }
            }
            (List<int> boards, int first) = Boards(inner, edge);
            List<bool> floor = edge.ConvertAll(i => door && inner[i].y <= sill + 1e-4f && inner[(i + 1) % inner.Count].y <= sill + 1e-4f);
            return new KwFrameRing(a, c, boards, floor, first);
        }

        /// <summary>How many steps an edge of the cut and the same edge of the outer outline are drawn in.</summary>
        public static int Steps(Vector2 a0, Vector2 a1, Vector2 c0, Vector2 c1) =>
            Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(Vector2.Distance(a0, a1), Vector2.Distance(c0, c1)) / Step));

        // Each step's board from its edge of the outline: numbered from the first edge a board starts at.
        private static (List<int> Boards, int First) Boards(List<Vector2> ring, List<int> edge)
        {
            List<bool> starts = Starts(ring);
            int firstEdge = Mathf.Max(0, starts.IndexOf(true)), first = Mathf.Max(0, edge.IndexOf(firstEdge));
            List<int> boards = new List<int>(new int[edge.Count]);
            int board = 0;
            for (int k = 0; k < edge.Count; k++)
            {
                int s = (first + k) % edge.Count, e = edge[s];
                if (k > 0 && starts[e] && edge[(s - 1 + edge.Count) % edge.Count] != e)
                    board++;
                boards[s] = board;
            }
            return (boards, first);
        }

        // Whether a board starts at each edge: at a corner, or where a straight side meets a curve.
        private static List<bool> Starts(List<Vector2> ring)
        {
            int n = ring.Count;
            List<float> turn = new List<float>();
            for (int i = 0; i < n; i++)
                turn.Add(Vector2.Angle(ring[i] - ring[(i - 1 + n) % n], ring[(i + 1) % n] - ring[i]));
            List<bool> curved = new List<bool>(), starts = new List<bool>();
            for (int i = 0; i < n; i++)
                curved.Add(Bends(turn[i]) && Bends(turn[(i + 1) % n]));
            for (int i = 0; i < n; i++)
                starts.Add(turn[i] > Corner || curved[i] != curved[(i - 1 + n) % n]);
            return starts;
        }

        private static bool Bends(float turn) => turn > Bend && turn <= Corner;
    }
}
