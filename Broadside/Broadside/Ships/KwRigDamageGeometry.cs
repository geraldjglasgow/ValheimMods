using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A round spar of a rig piece (<see cref="KwRigPiece"/>), in the piece's kit axes as its mesh is drawn
    /// (<see cref="KwSailShapes"/>, <see cref="KwRound.Taper"/>): from <see cref="A"/> to <see cref="C"/>, its radius going
    /// from <see cref="R0"/> to <see cref="R1"/>, or for a yard (<see cref="RMid"/> set) thickest at its middle and
    /// tapering to <see cref="R0"/> at both ends. Its part number says which spar of the piece it is: <see cref="Column"/>
    /// (a mast's own column, a bowsprit's spar, a cap's upright), a square sail's yard (the sail's number), a boom
    /// (<see cref="Boom"/> + the sail's number) or a gaff (<see cref="Gaff"/> + it). Angles round it go from KwRound's own
    /// first way across the axis (<see cref="Across"/>), so a notch lines up with the drawn mesh's sides.
    /// </summary>
    public readonly struct KwRigSpar
    {
        public const int Column = -1, Boom = 100, Gaff = 200;

        public KwRigSpar(int part, Vector3 a, Vector3 c, float r0, float r1, float rMid = 0f, int sides = 8)
        {
            (Part, A, C, R0, R1, RMid, Sides) = (part, a, c, r0, r1, rMid, sides);
        }

        public int Part { get; }
        public Vector3 A { get; }
        public Vector3 C { get; }
        public float R0 { get; }
        public float R1 { get; }
        public float RMid { get; }
        public int Sides { get; }

        public float Length => (C - A).magnitude;

        public Vector3 Axis => Length > 1e-5f ? (C - A) / Length : Vector3.forward;

        /// <summary>Its radius s along it from A.</summary>
        public float Radius(float s)
        {
            float l = Length;
            if (RMid > 0f)
                return Mathf.Lerp(RMid, R0, Mathf.Abs(s - l / 2f) / Mathf.Max(l / 2f, 1e-4f));
            return Mathf.Lerp(R0, R1, l > 0f ? s / l : 0f);
        }

        /// <summary>Its point s along it.</summary>
        public Vector3 At(float s) => A + Axis * s;

        /// <summary>Two ways across its axis, square to each other, as <see cref="KwRound.Taper"/> takes them.</summary>
        public (Vector3 U, Vector3 V) Across
        {
            get
            {
                Vector3 axis = Axis, u = Vector3.Cross(axis, Mathf.Abs(axis.z) < 0.9f ? Vector3.forward : Vector3.right).normalized;
                return (u, Vector3.Cross(axis, u));
            }
        }

        /// <summary>The way out from its axis at an angle round it (degrees).</summary>
        public Vector3 Out(float degrees)
        {
            (Vector3 u, Vector3 v) = Across;
            float a = degrees * Mathf.Deg2Rad;
            return u * Mathf.Cos(a) + v * Mathf.Sin(a);
        }

        /// <summary>The angle round it (degrees) of a way across it.</summary>
        public float AngleOf(Vector3 way)
        {
            (Vector3 u, Vector3 v) = Across;
            return Mathf.Atan2(Vector3.Dot(way, v), Vector3.Dot(way, u)) * Mathf.Rad2Deg;
        }

        /// <summary>How far along it a point lies (from A).</summary>
        public float Along(Vector3 kit) => Vector3.Dot(kit - A, Axis);

        /// <summary>Whether a point lies in its solid, give or take a margin.</summary>
        public bool Holds(Vector3 kit, float margin)
        {
            float s = Along(kit), l = Length;
            if (s < -margin || s > l + margin)
                return false;
            Vector3 out1 = kit - A - Axis * s;
            return out1.magnitude <= Radius(Mathf.Clamp(s, 0f, l)) + margin;
        }

        /// <summary>
        /// A shot past it, met at a point and flying a way: how far along it, the side it comes from (degrees) and how far
        /// off the axis its path goes, square to it; a shot along the axis comes from the point's own side, through the middle.
        /// </summary>
        public (float S, float Angle, float Off) Shot(Vector3 point, Vector3 way)
        {
            Vector3 e = Axis, rel = point - A;
            float s = Mathf.Clamp(Vector3.Dot(rel, e), 0f, Length);
            Vector3 radial = rel - e * Vector3.Dot(rel, e), flat = way - e * Vector3.Dot(way, e);
            if (flat.sqrMagnitude < 0.0025f)
                return (s, radial.sqrMagnitude > 1e-8f ? AngleOf(radial) : 0f, 0f);
            Vector3 dir = flat.normalized;
            float off = (radial - dir * Vector3.Dot(radial, dir)).magnitude;
            return (s, AngleOf(-dir), off);
        }
    }
}
