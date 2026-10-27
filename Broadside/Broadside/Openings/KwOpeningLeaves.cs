using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One leaf of an opening's cover or door, as it hangs: its hinge's place (kit, the part's axes) and turn (Unity, the
    /// part's local: x along the hinge, y from the hinge across the leaf, z out of the wall), the leaf's size along its hinge
    /// (centred on the hinge's place), across it and through it, whether it is round (a round port's lid), how far it
    /// swings open (degrees, its free edge outwards; a door either way, away from who opens it), which way along its hinge
    /// is up the wall (<see cref="Rise"/>: a door's look stands on its foot) and its box to bump into.
    /// </summary>
    public sealed class KwLeafPlan
    {
        public KwLeafPlan(KwOpening opening, Vector3 hinge, Quaternion turn, Vector3 size, float angle, float rise = 1f, bool quiet = false)
        {
            (Opening, Hinge, Turn, Size, Angle, Rise, Quiet) = (opening, hinge, turn, size, angle, rise, quiet);
        }

        public KwOpening Opening { get; }
        public Vector3 Hinge { get; }
        public Quaternion Turn { get; }

        /// <summary>Along the hinge, across from it, through the leaf.</summary>
        public Vector3 Size { get; }
        public float Angle { get; }

        /// <summary>1 when the leaf's own +x (along its hinge) runs up the wall, -1 when it runs down.</summary>
        public float Rise { get; }

        /// <summary>Whether it swings without a sound of its own: the second of a pair (a double door, shutters), whose first sounds for both.</summary>
        public bool Quiet { get; }

        /// <summary>The same leaf, quiet.</summary>
        public KwLeafPlan Quieted() => new KwLeafPlan(Opening, Hinge, Turn, Size, Angle, Rise, true);

        public bool Round => Opening.Cover == KwOpening.Lid;

        /// <summary>Whether it is a door's leaf (a single door's, or one of a double door's two).</summary>
        public bool Door => Opening.HasDoor;

        /// <summary>The leaf's box in its hinge's axes: a door through the wall's middle, a cover just outside the frame.</summary>
        public (Vector3 Centre, Vector3 Size) Box => (new Vector3(0f, Size.y / 2f, Door ? 0f : Size.z / 2f), Size);

        /// <summary>
        /// What it turns about as it swings one way (stage above 0 out, below 0 in), in its hinge's axes: the hinge edge of
        /// the face it swings toward, past anything standing on that face (a door's iron outside, its ledges inside; a
        /// cover's straps ride over it), so no part of it ever swings back past its hinge into the frame or the jamb (a
        /// cover's inner edge lifts off the frame past a quarter turn instead of dipping into it).
        /// </summary>
        public Vector3 Pivot(float stage) => !Door ? new Vector3(0f, 0f, Size.z)
            : new Vector3(0f, 0f, stage >= 0f ? Size.z / 2f + KwOpeningLeaves.DoorIron : -Size.z / 2f - KwDoorMesh.LedgeThick);
    }

    /// <summary>
    /// Where an opening's leaves hang (<see cref="KwLeafPlan"/>): a gunport lid hinged along the top of its frame, falling
    /// over the cut and the inner half of the frame, flapping up and out; a lid hinged along the bottom, flapping down; a
    /// pair of shutters hinged at the sides, each half the width; a round port's lid hinged at its top; a doorway's door
    /// hinged at its jamb nearer the wall's start in the middle of the wall's thickness, a quarter turn either way; a double
    /// door's two leaves each hinged at its own jamb, meeting in the middle, both swinging the same way with the one state.
    /// Each lies flat on the wall's way out at the opening's middle (its tunnel's, <see cref="KwOpeningTunnel"/>). Kit axes of
    /// the part. Pure.
    /// </summary>
    public static class KwOpeningLeaves
    {
        /// <summary>How far a cover reaches over its frame (a share of the frame's width), and its thickness and a door's boards'.</summary>
        private const float Over = 0.6f, CoverThick = 0.04f, DoorThick = 0.06f;

        /// <summary>A door's gap to its jamb, and half the gap where a double door's leaves meet.</summary>
        public const float Jamb = 0.01f, Meet = 0.005f;

        /// <summary>How far a door's iron stands off its outer face at most (a strap and a stud on it).</summary>
        public const float DoorIron = 0.017f;

        /// <summary>Every covered opening's leaves on a part's walls.</summary>
        public static List<KwLeafPlan> Of(List<KwOpeningWall> walls)
        {
            List<KwLeafPlan> leaves = new List<KwLeafPlan>();
            foreach (KwOpeningWall wall in walls)
            {
                foreach (KwOpening o in wall.Openings)
                    leaves.AddRange(For(wall.Surface, o));
            }
            return leaves;
        }

        /// <summary>One opening's leaves on its wall, square to its tunnel where it has one (none for an opening with no cover).</summary>
        public static List<KwLeafPlan> For(KwOpeningSurface wall, KwOpening o)
        {
            KwOpeningSurface s = o.Surface ?? wall;
            if (o.HasDoor)
                return DoorLeaves(s, o);
            if (!o.Covered)
                return new List<KwLeafPlan>();
            float reach = KwOpeningFrames.Width(o.Frame).Wide * Over;
            Vector2 mid = s.Fit(o, new Vector2(o.U, o.Sill + o.Height / 2f));
            Vector3 up = s.Up(mid.x, mid.y), along = s.Along(mid.x, mid.y), n = s.Out(mid.x, mid.y);
            Vector3 centre = Seat(s, o, s.Point(mid.x, mid.y, KwOpeningFrames.Front(s, o)), n, reach);
            float across = o.Width + 2f * reach, high = o.Height + 2f * reach;
            if (o.Cover == KwOpening.Shutters)
                return new List<KwLeafPlan>
                {
                    Leaf(o, centre - along * (across / 2f), along, n, new Vector3(high, across / 2f, CoverThick), 100f),
                    Leaf(o, centre + along * (across / 2f), -along, n, new Vector3(high, across / 2f, CoverThick), 100f).Quieted(),
                };
            bool down = o.Cover == KwOpening.Down;
            Vector3 hinge = centre + up * (down ? -high / 2f : high / 2f);
            return new List<KwLeafPlan> { Leaf(o, hinge, down ? up : -up, n, new Vector3(across, high, CoverThick), down ? 120f : 105f) };
        }

        // A doorway's door, hinged at the jamb at the start of the wall's run, in the middle of the wall, from the sill up,
        // lying straight across to the far jamb; a double door's two, the second hinged at the far jamb, each lying to the
        // opening's middle less the gap where they meet. Each leaf lies flat between its two ends, so on a curving hull side
        // the leaves still close on the jambs and on each other.
        private static List<KwLeafPlan> DoorLeaves(KwOpeningSurface s, KwOpening o)
        {
            Vector2 mid = s.Upright(o.U, o.Sill, o.Height / 2f + 0.005f);
            (float um, float z, float depth) = (mid.x, mid.y, (s.Proud - s.Thick) / 2f);
            float u0 = s.Run(um, z, Jamb - o.Width / 2f), u1 = s.Run(um, z, o.Width / 2f - Jamb);
            Vector3 near = s.Point(u0, z, depth), far = s.Point(u1, z, depth);
            if (o.Cover != KwOpening.DoubleDoor)
                return new List<KwLeafPlan> { DoorLeaf(s, o, near, far, um, z, 0f) };
            Vector3 middle = s.Point(um, z, depth);
            return new List<KwLeafPlan> { DoorLeaf(s, o, near, middle, (u0 + um) / 2f, z, Meet), DoorLeaf(s, o, far, middle, (um + u1) / 2f, z, Meet).Quieted() };
        }

        // One door leaf hinged at a point and lying flat toward another, short of it by a gap, its face square to that line
        // and as near the wall's way out at (u, z) as it can be.
        private static KwLeafPlan DoorLeaf(KwOpeningSurface s, KwOpening o, Vector3 hinge, Vector3 to, float u, float z, float gap)
        {
            Vector3 lie = to - hinge, n = Vector3.ProjectOnPlane(s.Out(u, z), lie).normalized;
            Vector3 size = new Vector3(o.Height - 0.02f, lie.magnitude - gap, DoorThick);
            return Leaf(o, hinge, lie, n, size, 90f, s.Up(u, z));
        }

        // A cover's middle moved out (never in) until its flat leaf clears the frame's face everywhere under it: where the
        // wall curves the other way, the face would stand out of the leaf's plane.
        private static Vector3 Seat(KwOpeningSurface s, KwOpening o, Vector3 centre, Vector3 n, float reach)
        {
            KwFrameRing under = KwFrameRing.Of(KwOpeningFrames.Grown(o, 0f), KwOpeningFrames.Grown(o, reach), false, o.Sill);
            float front = KwOpeningFrames.Front(s, o), lift = 0f;
            for (int i = 0; i < under.Count; i++)
            {
                foreach (Vector2 p in new[] { under.Inner[i], under.Outer[i] })
                    lift = Mathf.Max(lift, Vector3.Dot(s.Point(p.x, p.y, front) - centre, n));
            }
            return centre + n * lift;
        }

        // A leaf hinged at a kit point, lying from it along a kit way, its face out along another; its hinge's way up the
        // wall from a kit way up (none: a cover's, which never asks).
        private static KwLeafPlan Leaf(KwOpening o, Vector3 hinge, Vector3 across, Vector3 outward, Vector3 size, float angle, Vector3 up = default)
        {
            Vector3 lie = Vector3.ProjectOnPlane(across, outward).normalized;
            Quaternion turn = KwTurns.Look(KwMesh.ToUnity(outward), KwMesh.ToUnity(lie));
            float rise = Vector3.Dot(Vector3.Cross(KwMesh.ToUnity(lie), KwMesh.ToUnity(outward)), KwMesh.ToUnity(up)) < 0f ? -1f : 1f;
            return new KwLeafPlan(o, hinge, turn, size, angle, rise);
        }
    }
}
