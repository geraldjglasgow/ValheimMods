using System.Collections.Generic;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// What holds each blueprint piece up, so a queued roof is never built before the walls and beams it rests on. From
    /// every piece standing on the ground, support spreads through the boxes that touch (<see cref="SmartBox.Touches"/>),
    /// cheapest first: resting on a piece below costs 1, leaning on one beside it <see cref="SideCost"/>, hanging from
    /// one above <see cref="HangCost"/> (the game's support also fades far faster sideways than upwards). Each piece keeps
    /// the piece its support came through and its cost. A queued selection is then built with the unbuilt pieces of its
    /// support paths (down to the ground or to a built piece), cheapest first, so every piece goes up after what holds it.
    /// Worked out once per blueprint.
    /// </summary>
    public static class SiteSupport
    {
        private const float SideCost = 4f;
        private const float HangCost = 6f;
        private const float Touch = 0.15f;
        private const float Resting = 0.3f;
        private const int MaxTrees = 8;

        private sealed class Tree
        {
            public int[] Parent;
            public float[] Cost;
        }

        private static readonly Dictionary<Blueprint, Tree> trees = new Dictionary<Blueprint, Tree>();

        /// <summary>The pieces plus the unbuilt pieces that hold them up, in building order (supports first); built pieces left out.</summary>
        public static List<int> WithSupports(Blueprint bp, IEnumerable<int> pieces, bool[] built) => WithSupports(bp, pieces, built, null);

        /// <summary>The same with the pieces' drawn bounds from <paramref name="sizes"/> (offline tests); null: the game's.</summary>
        public static List<int> WithSupports(Blueprint bp, IEnumerable<int> pieces, bool[] built, System.Func<string, UnityEngine.Bounds?> sizes)
        {
            Tree tree = TreeOf(bp, sizes);
            HashSet<int> wanted = new HashSet<int>();
            foreach (int piece in pieces)
            {
                // Down the support path until the ground, a built piece, or a path already taken.
                int i = piece;
                while (i >= 0 && i < built.Length && !built[i] && wanted.Add(i))
                    i = tree.Parent[i];
            }
            List<int> order = new List<int>(wanted);
            order.Sort((a, b) => tree.Cost[a] != tree.Cost[b] ? tree.Cost[a].CompareTo(tree.Cost[b]) : a.CompareTo(b));
            return order;
        }

        private static Tree TreeOf(Blueprint bp, System.Func<string, UnityEngine.Bounds?> sizes)
        {
            if (trees.TryGetValue(bp, out Tree known) && known.Parent.Length == bp.Pieces.Count)
                return known;
            if (trees.Count >= MaxTrees)
                trees.Clear();
            Tree tree = Grow(SmartSelect.ModelOf(bp, sizes), bp);
            trees[bp] = tree;
            return tree;
        }

        /// <summary>Cheapest support from the ground (Dijkstra over touching boxes); a piece nothing reaches keeps no parent and costs the most.</summary>
        private static Tree Grow(SmartModel model, Blueprint bp)
        {
            int n = bp.Pieces.Count;
            Tree tree = new Tree { Parent = new int[n], Cost = new float[n] };
            SortedSet<(float Cost, int Piece)> open = new SortedSet<(float, int)>();
            for (int i = 0; i < n; i++)
            {
                tree.Parent[i] = -1;
                tree.Cost[i] = OnGround(model, i) ? 0f : float.MaxValue;
                if (tree.Cost[i] == 0f)
                    open.Add((0f, i));
            }
            while (open.Count > 0)
            {
                (float cost, int piece) = open.Min;
                open.Remove(open.Min);
                Spread(model, tree, open, piece, cost);
            }
            return tree;
        }

        private static void Spread(SmartModel model, Tree tree, SortedSet<(float, int)> open, int from, float cost)
        {
            SmartBox box = model.Boxes[from];
            foreach (int to in model.Hash.Near(box.X, box.Z, box.Reach + Touch))
            {
                if (to == from || !box.Touches(model.Boxes[to], Touch))
                    continue;
                float next = cost + Step(box, model.Boxes[to]);
                if (next >= tree.Cost[to])
                    continue;
                if (tree.Cost[to] < float.MaxValue)
                    open.Remove((tree.Cost[to], to));
                tree.Cost[to] = next;
                tree.Parent[to] = from;
                open.Add((next, to));
            }
        }

        /// <summary>What it costs <paramref name="held"/> to take its support from <paramref name="holder"/>: resting on it, beside it or hanging below it.</summary>
        private static float Step(SmartBox holder, SmartBox held)
        {
            if (holder.Top <= held.Bottom + Resting)
                return 1f;
            return holder.Bottom >= held.Top - Resting ? HangCost : SideCost;
        }

        private static bool OnGround(SmartModel model, int i) => model.Boxes[i].Bottom <= model.GroundAt(model.Boxes[i].X, model.Boxes[i].Z) + Resting;
    }
}
