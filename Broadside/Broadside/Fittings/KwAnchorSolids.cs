using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship piece's place and turn in an anchor windlass's frame (Unity axes, from the windlass's origin), and points and
    /// ways moved between the windlass's kit axes (x forward, y left, z up) and the piece's own. Plain arithmetic only
    /// (Unity's quaternion inverse is native code), so a plan is worked out the same on every peer and away from the game.
    /// </summary>
    public readonly struct KwAnchorPose
    {
        public KwAnchorPose(Vector3 position, Quaternion turn) => (Position, Turn) = (position, turn);

        public Vector3 Position { get; }
        public Quaternion Turn { get; }

        /// <summary>A piece's pose in the windlass's frame, both placed in one frame.</summary>
        public static KwAnchorPose Of(KwBpPiece windlass, KwBpPiece piece)
        {
            Quaternion back = Back(windlass.Rotation);
            return new KwAnchorPose(back * (piece.Position - windlass.Position), back * piece.Rotation);
        }

        /// <summary>A unit rotation's inverse.</summary>
        public static Quaternion Back(Quaternion q) => new Quaternion(-q.x, -q.y, -q.z, q.w);

        /// <summary>A point of the windlass's kit frame in the piece's own kit frame.</summary>
        public Vector3 Local(Vector3 kit) => KwMesh.ToKit(Back(Turn) * (KwMesh.ToUnity(kit) - Position));

        /// <summary>A point of the piece's own kit frame in the windlass's kit frame.</summary>
        public Vector3 Out(Vector3 kit) => KwMesh.ToKit(Turn * KwMesh.ToUnity(kit) + Position);

        /// <summary>A way of the piece's own kit frame in the windlass's kit frame.</summary>
        public Vector3 Way(Vector3 kit) => KwMesh.ToKit(Turn * KwMesh.ToUnity(kit));
    }

    /// <summary>
    /// Something an anchor's cable, fairlead or anchor must keep clear of (<see cref="KwAnchorShip"/>), tested at a point of
    /// the windlass's kit frame with a clearance: on deck (inboard, before the fairlead) or overboard (after it).
    /// </summary>
    public abstract class KwAnchorSolid
    {
        protected KwAnchorSolid(string what) => What = what;

        /// <summary>What it is, for the windlass's hover when it is in the way ("a hatch", "the mast").</summary>
        public string What { get; }

        /// <summary>A ball round all of it (windlass kit), so a run far from it passes it by untested.</summary>
        public Vector3 Middle { get; private set; }
        public float Radius { get; private set; } = float.MaxValue;

        public abstract bool Blocks(Vector3 p, float r, bool outboard);

        protected void Round(Vector3 middle, float radius) => (Middle, Radius) = (middle, radius);

        /// <summary>The ball round a box of a piece's kit frame.</summary>
        protected void Round(KwAnchorPose pose, Vector3 min, Vector3 max) => Round(pose.Out((min + max) / 2f), (max - min).magnitude / 2f);
    }

    /// <summary>A box in a piece's kit frame (a fitting's shape, a keel's post).</summary>
    public sealed class KwAnchorBox : KwAnchorSolid
    {
        private readonly KwAnchorPose pose;
        private readonly Vector3 min, max;

        public KwAnchorBox(string what, KwAnchorPose pose, Vector3 min, Vector3 max) : base(what)
        {
            (this.pose, this.min, this.max) = (pose, min, max);
            Round(pose, min, max);
        }

        public override bool Blocks(Vector3 p, float r, bool outboard)
        {
            Vector3 k = pose.Local(p);
            return k.x > min.x - r && k.x < max.x + r && k.y > min.y - r && k.y < max.y + r && k.z > min.z - r && k.z < max.z + r;
        }
    }

    /// <summary>An upright column (a mast, a ladder): its axis's place seen from above, its foot and top, its radius.</summary>
    public sealed class KwAnchorColumn : KwAnchorSolid
    {
        private readonly Vector2 at;
        private readonly float foot, top, radius;

        public KwAnchorColumn(string what, Vector3 footAt, float height, float radius) : base(what)
        {
            (at, foot, top, this.radius) = (new Vector2(footAt.x, footAt.y), Mathf.Min(footAt.z, footAt.z + height), Mathf.Max(footAt.z, footAt.z + height), radius);
            Round(new Vector3(at.x, at.y, (foot + top) / 2f), Mathf.Sqrt(radius * radius + (top - foot) * (top - foot) / 4f));
        }

        public override bool Blocks(Vector3 p, float r, bool outboard) =>
            p.z > foot - r && p.z < top + r && (new Vector2(p.x, p.y) - at).sqrMagnitude < (radius + r) * (radius + r);
    }

    /// <summary>A round spar between two points of a piece's kit frame (a bowsprit).</summary>
    public sealed class KwAnchorRod : KwAnchorSolid
    {
        private readonly KwAnchorPose pose;
        private readonly Vector3 a, b;
        private readonly float radius;

        public KwAnchorRod(string what, KwAnchorPose pose, Vector3 a, Vector3 b, float radius) : base(what)
        {
            (this.pose, this.a, this.b, this.radius) = (pose, a, b, radius);
            Round(pose.Out((a + b) / 2f), (b - a).magnitude / 2f + radius);
        }

        public override bool Blocks(Vector3 p, float r, bool outboard) => Near(pose.Local(p), a, b) < radius + r;

        /// <summary>How far a point is from the segment a to b.</summary>
        public static float Near(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 run = b - a;
            float t = run.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector3.Dot(p - a, run) / run.sqrMagnitude);
            return (a + run * t - p).magnitude;
        }
    }

    /// <summary>A flat round disc square to the windlass's axle (a drum's flange), in the windlass's own kit frame.</summary>
    public sealed class KwAnchorDisc : KwAnchorSolid
    {
        private readonly float y0, y1, radius;

        public KwAnchorDisc(string what, float y0, float y1, float radius) : base(what)
        {
            (this.y0, this.y1, this.radius) = (Mathf.Min(y0, y1), Mathf.Max(y0, y1), radius);
            Round(new Vector3(0f, (y0 + y1) / 2f, KwAnchorShapes.Axle), radius + (this.y1 - this.y0) / 2f);
        }

        public override bool Blocks(Vector3 p, float r, bool outboard) =>
            p.y > y0 - r && p.y < y1 + r && new Vector2(p.x, p.z - KwAnchorShapes.Axle).sqrMagnitude < (radius + r) * (radius + r);
    }
}
