using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A cabin as an anchor's cable sees it (<see cref="KwAnchorShip"/>), from its saved text (<see cref="KwCabinSpec"/>), in
    /// its kit frame: its whole room from its floor to over its roof, out to its walls' outer faces (a side at the hull: the
    /// hull's outside, carried up to the roof). A windlass standing inside it leads its cable inside its walls to a hawse
    /// pipe through a side at the hull; one outside keeps its cable out of the cabin altogether.
    /// </summary>
    public sealed class KwAnchorCabin : KwAnchorSolid
    {
        // A wall's thickness, the roof's slab and what stands on it (a roof railing), and a margin under the floor.
        private const float Wall = 0.1f, Roof = 1f, Floor = 0.15f;

        private readonly KwAnchorPose pose;

        public KwAnchorCabin(KwAnchorPose pose, KwCabinSpec spec) : base("a cabin")
        {
            (this.pose, Spec) = (pose, spec);
            float wide = KwAnchorHull.Widest(spec.Stations) + Wall;
            Round(pose, new Vector3(spec.X0 - Wall, -wide, spec.Floor - Floor), new Vector3(spec.X1 + Wall, wide, spec.Top + Roof));
        }

        public KwCabinSpec Spec { get; }

        /// <summary>Whether the windlass stands inside it (set once the ship is read).</summary>
        public bool Holds { get; set; }

        /// <summary>Whether a point is in its room, clear of its walls and roof by r.</summary>
        public bool Inside(Vector3 p, float r)
        {
            Vector3 k = pose.Local(p);
            if (k.x < Spec.X0 + Wall + r || k.x > Spec.X1 - Wall - r || k.z < Spec.Floor - 0.05f || k.z > Spec.Top - Wall - r)
                return false;
            KwCabinSide side = k.y >= 0f ? Spec.Left : Spec.Right;
            float reach = KwCabinOutline.Reach(Spec, side, k.x, k.z) - (side.AtHull ? 0f : KwCabinWall.Thick);
            return Mathf.Abs(k.y) < reach - r;
        }

        /// <summary>Whether a point is within the cabin's outer faces (its walls, its room or its roof), widened by r.</summary>
        public bool Within(Vector3 p, float r)
        {
            Vector3 k = pose.Local(p);
            if (k.x < Spec.X0 - Wall - r || k.x > Spec.X1 + Wall + r || k.z < Spec.Floor - Floor - r || k.z > Spec.Top + Roof + r)
                return false;
            KwCabinSide side = k.y >= 0f ? Spec.Left : Spec.Right;
            float reach = side.AtHull ? KwShapes.WidthAt(KwCabinOutline.Up(Spec, Mathf.Clamp(k.x, Spec.X0, Spec.X1)).Half, k.z) + KwPlanks.Lap
                : KwCabinOutline.Reach(Spec, side, Mathf.Clamp(k.x, Spec.X0, Spec.X1), k.z);
            return Mathf.Abs(k.y) < reach + r;
        }

        /// <summary>Whether a side at the hull stands over the sheer at a point (windlass kit): the cabin's raised side is there.</summary>
        public bool Raises(Vector3 sheer)
        {
            Vector3 k = pose.Local(sheer);
            KwCabinSide side = k.y >= 0f ? Spec.Left : Spec.Right;
            return side.AtHull && k.x > Spec.X0 - Wall && k.x < Spec.X1 + Wall && Spec.Top > k.z + 0.1f;
        }

        /// <summary>The cabin's top over the keel line (its kit z) and a point's height in its kit frame.</summary>
        public float Top => Spec.Top;

        public float Height(Vector3 p) => pose.Local(p).z;

        /// <summary>
        /// Its side at the hull where a point is (along and height): the inner and outer faces there, square across its frame
        /// (windlass kit); null on a drawn-in side, past its ends, or outside its floor and roof.
        /// </summary>
        public (Vector3 Inner, Vector3 Outer)? Faces(Vector3 near)
        {
            Vector3 k = pose.Local(near);
            KwCabinSide side = k.y >= 0f ? Spec.Left : Spec.Right;
            if (!side.AtHull || k.x < Spec.X0 || k.x > Spec.X1 || k.z < Spec.Floor || k.z > Spec.Top)
                return null;
            float s = k.y >= 0f ? 1f : -1f;
            Vector2[] half = KwCabinOutline.Up(Spec, k.x).Half;
            float outer = KwShapes.WidthAt(half, k.z) + KwPlanks.Lap, inner = KwShapes.InsideWidth(half, k.z);
            return (pose.Out(new Vector3(k.x, s * inner, k.z)), pose.Out(new Vector3(k.x, s * outer, k.z)));
        }

        public override bool Blocks(Vector3 p, float r, bool outboard) => Holds && !outboard ? !Inside(p, r) : Within(p, r);
    }
}
