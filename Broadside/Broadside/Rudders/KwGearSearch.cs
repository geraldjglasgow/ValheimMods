using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The places of <see cref="KwGearGrid"/>'s grid: the shaft's height at each (the wheel's deck's top less
    /// <see cref="KwGearTrain.Below"/>), and whether each stretch between two neighbours is clear and a bevel box fits at
    /// each, worked out the first time they are asked and kept; a stretch across several lanes is clear when each one
    /// between neighbouring lanes is.
    /// </summary>
    internal sealed class KwGearNodes
    {
        private readonly float[,] heights;
        private readonly sbyte[,] along, across, corner;

        public KwGearNodes(IKwGearWay line, float[] places, float[] ys)
        {
            (Line, Places, Ys) = (line, places, ys);
            int n = places.Length, m = ys.Length;
            (heights, along, across, corner) = (new float[n, m], new sbyte[n, m], new sbyte[n, m], new sbyte[n, m]);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                    heights[i, j] = line.At(places[i], ys[j]) ?? float.NaN;
            }
        }

        public IKwGearWay Line { get; }
        public float[] Places { get; }
        public float[] Ys { get; }

        /// <summary>The lane nearest a place across.</summary>
        public int Lane(float y)
        {
            int best = 0;
            for (int j = 1; j < Ys.Length; j++)
            {
                if (Mathf.Abs(Ys[j] - y) < Mathf.Abs(Ys[best] - y))
                    best = j;
            }
            return best;
        }

        /// <summary>Whether a place has the wheel's deck over it.</summary>
        public bool Valid(int i, int j) => !float.IsNaN(heights[i, j]);

        public Vector3 Point(int i, int j) => new Vector3(Places[i], Ys[j], heights[i, j]);

        /// <summary>Whether the stretch from a place to the next one forward is clear.</summary>
        public bool Along(int i, int j) =>
            i + 1 < Places.Length && Known(along, i, j, () => Valid(i, j) && Valid(i + 1, j) && Line.Run(Point(i, j), Point(i + 1, j)));

        /// <summary>Whether the stretch across from one lane to another at a place is clear and long enough.</summary>
        public bool Across(int i, int j, int k, float shortest)
        {
            if (k == j || Mathf.Abs(Ys[k] - Ys[j]) < shortest)
                return false;
            for (int l = Mathf.Min(j, k); l < Mathf.Max(j, k); l++)
            {
                if (!Known(across, i, l, () => Valid(i, l) && Valid(i, l + 1) && Line.Run(Point(i, l), Point(i, l + 1))))
                    return false;
            }
            return true;
        }

        /// <summary>Whether a bevel box fits at a place.</summary>
        public bool Corner(int i, int j) => Known(corner, i, j, () => Valid(i, j) && Line.Corner(Point(i, j)));

        private static bool Known(sbyte[,] cache, int i, int j, System.Func<bool> work)
        {
            if (cache[i, j] == 0)
                cache[i, j] = work() ? (sbyte)1 : (sbyte)2;
            return cache[i, j] == 1;
        }
    }

    /// <summary>
    /// Dijkstra over <see cref="KwGearNodes"/>: a state is a place, a lane and the heading it was reached on (0 along the
    /// ship, 1 across); a step along or across costs its length, and a change of heading a bend's cost more, where a box fits.
    /// </summary>
    internal sealed class KwGearSearch
    {
        private readonly KwGearNodes nodes;
        private readonly float bend, shortest;
        private readonly int lanes;
        private readonly float[] cost;
        private readonly int[] prev;
        private readonly SortedSet<(float Cost, int State)> open = new SortedSet<(float Cost, int State)>();

        public KwGearSearch(KwGearNodes nodes, float bend, float shortest)
        {
            (this.nodes, this.bend, this.shortest, lanes) = (nodes, bend, shortest, nodes.Ys.Length);
            int states = nodes.Places.Length * lanes * 2;
            (cost, prev) = (new float[states], new int[states]);
            for (int s = 0; s < states; s++)
                (cost[s], prev[s]) = (float.MaxValue, -1);
        }

        /// <summary>A state to start from, at a cost.</summary>
        public void Seed(int i, int j, int h, float c) => Offer(State(i, j, h), c, -1);

        /// <summary>The cheapest way to a place and lane (either heading), as its places in order; null with none.</summary>
        public List<(int I, int J)>? Run(int goalI, int goalJ)
        {
            while (open.Count > 0)
            {
                (float c, int s) = open.Min;
                open.Remove(open.Min);
                if (c > cost[s])
                    continue;
                (int i, int j, int h) = (s / 2 / lanes, s / 2 % lanes, s % 2);
                if (i == goalI && j == goalJ)
                    return Back(s);
                Expand(s, i, j, h, c);
            }
            return null;
        }

        private void Expand(int s, int i, int j, int h, float c)
        {
            if (nodes.Along(i, j))
                Offer(State(i + 1, j, 0), c + nodes.Places[i + 1] - nodes.Places[i] + Turn(i, j, h, 0), s);
            for (int k = 0; k < lanes; k++)
            {
                if (nodes.Across(i, j, k, shortest))
                    Offer(State(i, k, 1), c + Mathf.Abs(nodes.Ys[k] - nodes.Ys[j]) + Turn(i, j, h, 1), s);
            }
        }

        // A change of heading costs a bend, where a box fits; none where it does not.
        private float Turn(int i, int j, int from, int to) => from == to ? 0f : nodes.Corner(i, j) ? bend : float.PositiveInfinity;

        private void Offer(int s, float c, int from)
        {
            if (float.IsInfinity(c) || c >= cost[s])
                return;
            (cost[s], prev[s]) = (c, from);
            open.Add((c, s));
        }

        private int State(int i, int j, int h) => (i * lanes + j) * 2 + h;

        private List<(int I, int J)> Back(int s)
        {
            List<(int I, int J)> path = new List<(int I, int J)>();
            for (int at = s; at >= 0; at = prev[at])
                path.Add((at / 2 / lanes, at / 2 % lanes));
            path.Reverse();
            return path;
        }
    }
}
