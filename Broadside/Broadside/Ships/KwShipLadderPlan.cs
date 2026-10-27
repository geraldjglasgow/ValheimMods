using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a launched ship's boarding ladder hangs (<see cref="KwShipLadderFind"/>), in its blueprint's frame as kit axes
    /// (x forward, y to port, z up): its place along the ship and its side, its foot a little under the waterline and its
    /// top (the sheer, a doorway's sill or a cabin roof's edge; over a railing's cap when it hooks over one), the hull's
    /// outside half width at each height down it under its middle (<see cref="Profile"/>, where its supports meet the hull),
    /// how far out it hangs (<see cref="Out"/>, clear of the hull at its widest across its width), and
    /// where the climb puts the player (<see cref="Target"/>, on a deck, a hold floor, inside a doorway or on a roof). Plain
    /// numbers, the same on every peer.
    /// </summary>
    public sealed class KwShipLadderPlan
    {
        /// <summary>What it climbs to: a deck inside the rail, a doorway in a side, a cabin's roof, or (nothing else found) the widest section's sheer.</summary>
        public const string OnDeck = "deck", AtDoorway = "doorway", OnRoof = "roof", Widest = "widest";

        /// <summary>How far the ladder's line stands off the hull at its widest between its foot and top.</summary>
        public const float Clear = 0.06f;

        public KwShipLadderPlan(string kind, float along, float side, float foot, float top, float water, List<Vector2> profile, float widest, Vector3 target, float slope = 0f)
        {
            (Kind, Along, Side, Foot, Top, Water, Profile, Target) = (kind, along, side, foot, top, water, profile, target);
            Out = widest + Clear;
            Slope = Mathf.Abs(slope) > 0.06f ? slope : 0f;
        }

        /// <summary>How fast the side runs out along the ship where it hangs (<see cref="KwShipLadderFind.Slope"/>); 0 on a straight side, where it hangs square to the ship.</summary>
        public float Slope { get; }

        public string Kind { get; }

        /// <summary>Its place along the ship and its side (+1 port, -1 starboard).</summary>
        public float Along { get; }
        public float Side { get; }

        /// <summary>Its foot and top, and the waterline (heights, kit z).</summary>
        public float Foot { get; }
        public float Top { get; }
        public float Water { get; }

        /// <summary>The hull's outside half width (y) under the ladder's middle at each height (x) from the foot to the top.</summary>
        public List<Vector2> Profile { get; }

        /// <summary>The ladder's line out from the centre line: the hull's widest across the ladder's width between its foot and top, and <see cref="Clear"/>.</summary>
        public float Out { get; }

        /// <summary>Where the climb puts the player (kit), a little over the floor there.</summary>
        public Vector3 Target { get; }

        /// <summary>The ladder's top, in the blueprint's Unity axes (a launched ship's: its fittings' frame, where its pieces stand).</summary>
        public Vector3 TopAt => KwMesh.ToUnity(new Vector3(Along, Side * Out, Top));

        /// <summary>Where the climb puts the player, in the blueprint's Unity axes.</summary>
        public Vector3 TargetAt => KwMesh.ToUnity(Target);

        /// <summary>
        /// Level, out of the ship from the ladder (Unity axes); the climber faces the other way, inboard. On a curve it is
        /// turned square to the side there, so the ladder lies along the planking with both rails on it.
        /// </summary>
        public Vector3 Outboard => KwMesh.ToUnity(new Vector3(-Slope, Side, 0f).normalized);

        /// <summary>The hull's outside half width at a height, between the profile's samples (0 where there is no hull).</summary>
        public float HullAt(float z)
        {
            if (Profile.Count == 0)
                return 0f;
            for (int i = 0; i + 1 < Profile.Count; i++)
            {
                if (z <= Profile[i + 1].x)
                    return Mathf.Lerp(Profile[i].y, Profile[i + 1].y, Mathf.InverseLerp(Profile[i].x, Profile[i + 1].x, z));
            }
            return Profile[Profile.Count - 1].y;
        }

        public override string ToString() => string.Format(CultureInfo.InvariantCulture,
            "{0} at {1:0.00} m along on the {2} side, {3:0.00} to {4:0.00} m (water {5:0.00}), {6:0.00} m out; the climb to ({7:0.00}, {8:0.00}, {9:0.00})",
            Kind, Along, Side > 0f ? "port" : "starboard", Foot, Top, Water, Out, Target.x, Target.y, Target.z);
    }
}
