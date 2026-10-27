using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The room round one doorway or window that a climbing steering shaft keeps out of (<see cref="KwGearRooms"/>): across
    /// the opening and its frame with a hand to spare, from under its sill to over its head, and in from its wall's outer
    /// face through the wall and on as far as a door's leaf swings (its width: a door swings a quarter turn either way,
    /// <see cref="KwOpeningLeaves"/>) or a window's frame, cover and the hands at it reach. A box in the rudder's kit frame,
    /// square to the wall at the opening's middle (a doorway, a cabin wall's window, a gunport in a raised side or the hull's
    /// planking alike).
    /// </summary>
    internal readonly struct KwGearKeepOut
    {
        // A hand beyond the frame; how far in a window's room reaches past its wall; how far past a door's swing.
        private const float Spare = 0.05f, Window = 0.3f, Swing = 0.1f;

        private KwGearKeepOut(Vector3 middle, Vector3 along, Vector3 up, Vector3 outward, float halfAlong, float halfUp, float inward)
        {
            (Middle, Along, Up, Out) = (middle, along, up, outward);
            (HalfAlong, HalfUp, In) = (halfAlong, halfUp, inward);
        }

        public Vector3 Middle { get; }
        public Vector3 Along { get; }
        public Vector3 Up { get; }
        public Vector3 Out { get; }
        public float HalfAlong { get; }
        public float HalfUp { get; }
        public float In { get; }

        /// <summary>Whether a point of the rudder's frame lies in it, or within a margin of it.</summary>
        public bool Holds(Vector3 p, float margin)
        {
            Vector3 d = p - Middle;
            float depth = Vector3.Dot(d, Out);
            return Mathf.Abs(Vector3.Dot(d, Along)) <= HalfAlong + margin && Mathf.Abs(Vector3.Dot(d, Up)) <= HalfUp + margin
                && depth <= Spare + margin && depth >= -(In + margin);
        }

        /// <summary>Every opening's room on a part's walls (a cabin's, or a planking part's), the part placed in the rudder's frame.</summary>
        public static void Of(KwGearPose pose, List<KwOpeningWall> walls, List<KwGearKeepOut> into)
        {
            foreach (KwOpeningWall wall in walls)
            {
                foreach (KwOpening o in wall.Openings)
                    into.Add(One(pose, wall.Surface, o));
            }
        }

        // One opening's room, square to its wall at its middle.
        private static KwGearKeepOut One(KwGearPose pose, KwOpeningSurface s, KwOpening o)
        {
            float frame = KwOpeningFrames.Width(o.Frame).Wide + Spare;
            Vector2 mid = s.Fit(o, new Vector2(o.U, o.Sill + o.Height / 2f));
            Vector3 at = s.Point(mid.x, mid.y, 0f);
            float inward = s.Thick + (o.IsDoorway ? o.Width + Swing : Window);
            return new KwGearKeepOut(pose.Out(at), Turned(pose, s.Along(mid.x, mid.y)), Turned(pose, s.Up(mid.x, mid.y)), Turned(pose, s.Out(mid.x, mid.y)),
                o.Width / 2f + frame, o.Height / 2f + frame, inward);
        }

        // A way of a part's kit frame as the rudder's.
        private static Vector3 Turned(KwGearPose pose, Vector3 way) => (pose.Out(way) - pose.Out(Vector3.zero)).normalized;
    }
}
