using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One place a rope may tie on to its ship (<see cref="KwRigEdges"/>), in the rigged piece's kit frame: on the top of the
    /// hull's side (the planking's top edge at its outer face, the sheer, a railing's cap on it where one stands) or on a
    /// cabin roof's outer edge (its roof railing's where one stands); which way is out of the ship there (level); the side's
    /// outer face a little under it, where a chainplate is bolted on; and its place along the ship and its side.
    /// </summary>
    public sealed class KwRigEdge
    {
        public KwRigEdge(Vector3 at, Vector3 outward, Vector3 below, float face, bool railed, KwRigCabin? roof, KwBiomeTier tier)
        {
            (At, Out, Below, Face, Railed, Roof, Tier) = (at, outward, below, face, railed, roof, tier);
        }

        /// <summary>The edge's top: the planking's top edge at its outer face, or the roof's top at its outline.</summary>
        public Vector3 At { get; }

        /// <summary>Level, out of the ship, square to the edge.</summary>
        public Vector3 Out { get; }

        /// <summary>Level, along the edge.</summary>
        public Vector3 Way => new Vector3(-Out.y, Out.x, 0f);

        /// <summary>The side's outer face <see cref="KwRigEdges.Drop"/> under the edge.</summary>
        public Vector3 Below { get; }

        /// <summary>How far out of <see cref="At"/> the side's outer face is at the top: the laps, a raised side's planks, a railing's cap.</summary>
        public float Face { get; }

        /// <summary>A railing stands on it here (the hull's on the sheer, or a roof railing on the roof's edge).</summary>
        public bool Railed { get; }

        /// <summary>The cabin whose roof's edge it is; null on the hull's side.</summary>
        public KwRigCabin? Roof { get; }

        public KwBiomeTier Tier { get; }

        /// <summary>Its place along the ship (the ship's x) and its side (+1 the ship's left), set by <see cref="KwRigEdges.Of"/>.</summary>
        public float Along { get; set; }
        public float Side { get; set; }
    }

    /// <summary>
    /// Every place a rope may tie on to a ship (<see cref="KwRigEdge"/>), from its pieces' saved shapes
    /// (<see cref="KwRigShip"/>): every <see cref="Step"/> along each side of each planking part's sheer, as far as the hull is
    /// <see cref="Narrowest"/> wide, and along each side of each cabin's roof. None where something stands over it: a cabin
    /// (its raised side, or a cabin on that roof) or a higher planking part (an upper level carried on above it).
    /// </summary>
    public static class KwRigEdges
    {
        /// <summary>The step along an edge; the narrowest half width tied to; how far down a chainplate reaches.</summary>
        public const float Step = 0.1f, Narrowest = 0.3f, Drop = 0.45f;

        private static readonly float[] Sides = { 1f, -1f };

        public static List<KwRigEdge> Of(KwRigShip ship)
        {
            List<KwRigEdge> edges = new List<KwRigEdge>();
            foreach (KwAnchorHull hull in ship.Hull.Hulls)
            {
                foreach (float side in Sides)
                    Sheer(ship, hull, side, edges);
            }
            foreach (KwRigCabin cabin in ship.Cabins)
            {
                foreach (float side in Sides)
                    Roof(ship, cabin, side, edges);
            }
            foreach (KwRigEdge edge in edges)
            {
                Vector3 k = ship.Frame.Local(edge.At);
                (edge.Along, edge.Side) = (k.x, k.y >= 0f ? 1f : -1f);
            }
            return edges;
        }

        // Along one side of a planking part's sheer.
        private static void Sheer(KwRigShip ship, KwAnchorHull hull, float side, List<KwRigEdge> edges)
        {
            KwBiomeTier tier = KwBiomeTiers.Of(hull.Piece.Data);
            for (float x = hull.From + Step / 2f; x < hull.To; x += Step)
            {
                Vector2 e = hull.Sheer(x);
                Vector3 at = hull.Pose.Out(new Vector3(x, side * e.x, e.y));
                if (e.x < Narrowest || Covered(ship, hull, null, at))
                    continue;
                Vector3 outward = Level(hull.Pose.Way(new Vector3(-hull.Slope(x), side, 0f)));
                float under = hull.Outside(x, e.y - Drop);
                Vector3 below = hull.Pose.Out(new Vector3(x, side * (under > 0.1f ? under : e.x + KwPlanks.Lap), e.y - Drop));
                bool railed = ship.Hull.Rails.Exists(rail => rail.Covers(at, out _, out _));
                edges.Add(new KwRigEdge(at, outward, below, railed ? KwRailing.Half : KwPlanks.Lap, railed, null, tier));
            }
        }

        // Along one side of a cabin roof's outline.
        private static void Roof(KwRigShip ship, KwRigCabin cabin, float side, List<KwRigEdge> edges)
        {
            if (cabin.Roof.Count < 2)
                return;
            float top = cabin.Spec.Top, x1 = cabin.Roof[cabin.Roof.Count - 1].X;
            for (float x = cabin.Roof[0].X + Step / 2f; x < x1; x += Step)
            {
                float reach = Reach(cabin, x, side);
                Vector3 at = cabin.Pose.Out(new Vector3(x, side * reach, top));
                if (reach < 0.15f || Covered(ship, null, cabin, at))
                    continue;
                float slope = (Reach(cabin, x + 0.05f, side) - Reach(cabin, x - 0.05f, side)) / 0.1f;
                Vector3 outward = Level(cabin.Pose.Way(new Vector3(-slope, side, 0f)));
                (float face, Vector3 below) = Face(cabin, x, side, reach);
                bool railed = ship.RoofRails.Exists(rail => rail.Covers(at));
                edges.Add(new KwRigEdge(at, outward, cabin.Pose.Out(below), face, railed, cabin, cabin.Tier));
            }
        }

        private static float Reach(KwRigCabin cabin, float x, float side)
        {
            (float Left, float Right)? reach = cabin.Reach(x);
            return reach == null ? 0f : side > 0f ? reach.Value.Left : reach.Value.Right;
        }

        // How far out of the roof's edge the cabin's side is at its top (a side at the hull: the raised planking), and its
        // outer face a drop under the roof's top (cabin kit).
        private static (float Face, Vector3 Below) Face(KwRigCabin cabin, float x, float side, float reach)
        {
            KwCabinSpec spec = cabin.Spec;
            if (!(side > 0f ? spec.Left : spec.Right).AtHull)
                return (0f, new Vector3(x, side * reach, spec.Top - Drop));
            Vector2[] half = KwCabinOutline.Up(spec, Mathf.Clamp(x, spec.X0, spec.X1)).Half;
            float outer = KwShapes.WidthAt(half, spec.Top) + KwPlanks.Lap, under = KwShapes.WidthAt(half, spec.Top - Drop) + KwPlanks.Lap;
            return (Mathf.Max(0f, outer - reach), new Vector3(x, side * under, spec.Top - Drop));
        }

        // Something stands over an edge: a cabin (its raised side, or one on that roof), or a higher planking part.
        private static bool Covered(KwRigShip ship, KwAnchorHull? hull, KwRigCabin? roof, Vector3 at)
        {
            Vector3 over = at + new Vector3(0f, 0f, 0.3f);
            if (ship.Cabins.Exists(c => c != roof && c.Blocks(over, 0f, false)))
                return true;
            foreach (KwAnchorHull other in ship.Hull.Hulls)
            {
                if (other == hull)
                    continue;
                Vector3 k = other.Pose.Local(at);
                if (k.x > other.From && k.x < other.To && other.Sheer(k.x).y > k.z + 0.05f && other.Outside(k.x, k.z) > Mathf.Abs(k.y) - 0.15f)
                    return true;
            }
            return false;
        }

        private static Vector3 Level(Vector3 way) => new Vector3(way.x, way.y, 0f).normalized;
    }
}
