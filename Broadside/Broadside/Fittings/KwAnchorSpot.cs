using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A place an anchor's cable may go overboard (<see cref="KwAnchorSpots"/>), in the windlass's kit frame: a fairlead
    /// chock on the hull's sheer (on a railing's cap, or on the planking's top edge, or across a flat stern's transom), the
    /// cable over its saddle between its inboard lip and its outboard lip; or a hawse pipe through the hull's side or a
    /// cabin's side at the hull, the cable through it from its inner mouth to its outer one. Its ways are level: out of
    /// the hull square to it, and along it.
    /// </summary>
    public sealed class KwAnchorSpot
    {
        public KwAnchorSpot(KwAnchorHull hull, bool pipe, bool railed, Vector3 at, Vector3 outward, float inset, float outset)
        {
            (Hull, Pipe, Railed, At, Outward) = (hull, pipe, railed, at, outward);
            Along = new Vector3(-outward.y, outward.x, 0f);
            (Inset, Outset) = (inset, outset);
            float rise = pipe ? 0f : Rise, lip = pipe ? PipeLip : Lip;
            Inner = at - outward * (inset + lip) + new Vector3(0f, 0f, rise);
            Outer = at + outward * (outset + lip) + new Vector3(0f, 0f, rise);
            Estimate = (Inner - KwAnchorRoute.DrumTop).magnitude;
        }

        /// <summary>How far a chock's lips stand past the faces it sits over; a pipe's mouths past the wall's faces; how
        /// high over its seat a chock's saddle carries the cable.</summary>
        public const float Lip = 0.07f, PipeLip = 0.07f, Rise = 0.08f;

        public KwAnchorHull Hull { get; }

        /// <summary>A hawse pipe through a side, not a chock over the sheer.</summary>
        public bool Pipe { get; }

        /// <summary>A chock seated on a railing's cap.</summary>
        public bool Railed { get; }

        /// <summary>A chock: the middle of its seat on the cap or top edge; a pipe: the middle of the wall it goes through.</summary>
        public Vector3 At { get; }

        public Vector3 Outward { get; }
        public Vector3 Along { get; }

        /// <summary>How far in from its middle the inner face is, and out to the outer face.</summary>
        public float Inset { get; }
        public float Outset { get; }

        /// <summary>Where the cable comes to it from the windlass, and where it leaves it overboard.</summary>
        public Vector3 Inner { get; }
        public Vector3 Outer { get; }

        /// <summary>The straight way from the drum to its inner lip (no cable to it is shorter), and what it costs besides.</summary>
        public float Estimate { get; }

        /// <summary>Metres of cable it counts for besides its length: a chock beside a shield hook.</summary>
        public float Penalty { get; set; }

        /// <summary>The least it can cost: its straight way and its penalty.</summary>
        public float Least => Estimate + Penalty;
    }
}
