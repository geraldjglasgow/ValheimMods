using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The line shaft's way round what stands under the wheel's deck (<see cref="KwRudderGearRoute"/>), when it cannot run
    /// straight: stretches along the ship, and across it from any lane to any other, on a grid of places (every
    /// <see cref="Station"/> or so along, in lanes across: the bore's, the wheel's and a few either side of the keel), the cheapest by length with
    /// <see cref="Bend"/> more for each bevel box (Dijkstra), each stretch clear (<see cref="KwGearLine.Run"/>), each bend
    /// where a box fits (<see cref="KwGearLine.Corner"/>) and no stretch across shorter than <see cref="Shortest"/>, so two
    /// boxes never meet. Its corners are where it bends, the pinion first and the place under the wheel last; a straight
    /// stretch merged over places whose decks are not level enough for one run keeps them as couplings.
    /// </summary>
    public static class KwGearGrid
    {
        private const float Station = 0.5f, Bend = 2f, Shortest = 0.35f;
        private static readonly float[] Lanes = { 0.5f, 1f, 1.5f, 2f, 2.5f, 3f, 3.5f, 4f, 5f };

        /// <summary>The corners from the pinion to under the wheel, the first bend no further aft than a place along; null with no way.</summary>
        public static List<Vector3>? Search(IKwGearWay line, Vector3 pinion, Vector3 under, float first)
        {
            if (under.x - first < Station / 2f)
                return null;
            KwGearNodes nodes = new KwGearNodes(line, Places(first, under.x), Ys(pinion.y, under.y));
            List<(int I, int J)>? path = Path(nodes, pinion, under);
            return path == null ? null : Corners(line, nodes, path, pinion);
        }

        // Places along, evenly from the first to the wheel.
        private static float[] Places(float first, float last)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt((last - first) / Station));
            float[] xs = new float[n + 1];
            for (int i = 0; i <= n; i++)
                xs[i] = Mathf.Lerp(first, last, i / (float)n);
            return xs;
        }

        // Lanes across: the bore's and the wheel's, and the standard ones not too near either.
        private static float[] Ys(float bore, float wheel)
        {
            List<float> ys = new List<float> { bore };
            if (Mathf.Abs(wheel - bore) > 1e-3f)
                ys.Add(wheel);
            foreach (float lane in Lanes)
            {
                foreach (float y in new[] { lane, -lane })
                {
                    if (Mathf.Abs(y - bore) >= Shortest && Mathf.Abs(y - wheel) >= Shortest)
                        ys.Add(y);
                }
            }
            ys.Sort();
            return ys.ToArray();
        }

        // The cheapest way through the grid, as its places from the first to the wheel's (Dijkstra over place, lane and heading).
        private static List<(int I, int J)>? Path(KwGearNodes nodes, Vector3 pinion, Vector3 under)
        {
            int start = nodes.Lane(pinion.y), goal = nodes.Lane(under.y);
            if (!nodes.Valid(0, start) || !nodes.Line.Run(pinion, nodes.Point(0, start)))
                return null;
            KwGearSearch search = new KwGearSearch(nodes, Bend, Shortest);
            search.Seed(0, start, 0, nodes.Point(0, start).x - pinion.x);
            return search.Run(nodes.Places.Length - 1, goal);
        }

        // The path's corners: the pinion, every place it bends at (the first place too, when it turns there), the wheel's.
        private static List<Vector3> Corners(IKwGearWay line, KwGearNodes nodes, List<(int I, int J)> path, Vector3 pinion)
        {
            List<Vector3> corners = new List<Vector3> { pinion };
            int from = -1;
            for (int k = 0; k < path.Count; k++)
            {
                (int I, int J) before = k == 0 ? (-1, path[0].J) : path[k - 1];
                if (k < path.Count - 1 && Straight(before, path[k], path[k + 1]))
                    continue;
                Stretch(line, nodes, path, from, k, corners);
                from = k;
            }
            return corners;
        }

        // From the last corner to a place as one run when it is clear as one, else through each place between (couplings).
        private static void Stretch(IKwGearWay line, KwGearNodes nodes, List<(int I, int J)> path, int from, int to, List<Vector3> corners)
        {
            Vector3 b = nodes.Point(path[to].I, path[to].J);
            if (!line.Run(corners[corners.Count - 1], b))
            {
                for (int k = from + 1; k < to; k++)
                    corners.Add(nodes.Point(path[k].I, path[k].J));
            }
            corners.Add(b);
        }

        private static bool Straight((int I, int J) a, (int I, int J) b, (int I, int J) c) =>
            (a.I == b.I && b.I == c.I) || (a.J == b.J && b.J == c.J);
    }
}
