using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How a triangle sail's cloth is set into a jib's shape. The cloth (<see cref="KwSailCanvas"/>) has its luff straight up
    /// its own z axis and its foot straight aft along its -x, square to each other; a jib's luff runs up its stay and its foot
    /// aft, rising a little, at another angle to it. The shear that takes the cloth's x to the foot's way back (made
    /// forward) and its z up the stay is split into a turn, a stretch along two square axes and a turn back (the polar
    /// split, then the stretch's own axes), so it is three nested transforms the game draws as they are: the cloth's
    /// motion, folds and bolt rope stay the sail code's own, worked out in the cloth's square axes. Angles in the jib's
    /// plane from its forward (level, toward the bow) up: the stay's alpha, the foot's rise beta; the turns go the same way.
    /// </summary>
    public readonly struct KwJibShear
    {
        public KwJibShear(float turn, float along, float up, float back)
        {
            (Turn, Along, Up, Back) = (turn, along, up, back);
        }

        /// <summary>The first transform's turn in the plane (radians, forward toward up).</summary>
        public float Turn { get; }

        /// <summary>The stretch along the cloth's x (after the turn back) and along its z.</summary>
        public float Along { get; }
        public float Up { get; }

        /// <summary>The cloth's own turn in the plane, applied first.</summary>
        public float Back { get; }

        /// <summary>The shear for a stay rising at alpha and a foot rising aft at beta (alpha over beta).</summary>
        public static KwJibShear Of(float alpha, float beta)
        {
            float a11 = Mathf.Cos(beta), a21 = -Mathf.Sin(beta), a12 = -Mathf.Cos(alpha), a22 = Mathf.Sin(alpha);
            float theta = Mathf.Atan2(a21 - a12, a11 + a22), c = Mathf.Cos(theta), s = Mathf.Sin(theta);
            float s11 = c * a11 + s * a21, s12 = c * a12 + s * a22, s22 = -s * a12 + c * a22;
            float phi = 0.5f * Mathf.Atan2(2f * s12, s11 - s22), cp = Mathf.Cos(phi), sp = Mathf.Sin(phi);
            float l1 = s11 * cp * cp + 2f * s12 * sp * cp + s22 * sp * sp, l2 = s11 * sp * sp - 2f * s12 * sp * cp + s22 * cp * cp;
            return new KwJibShear(theta + phi, l1, l2, -phi);
        }

        /// <summary>Where a cloth point (its x along, its z up) lands in the jib's plane (forward, up).</summary>
        public Vector2 Apply(Vector2 p)
        {
            Vector2 turned = Rotate(p, Back);
            return Rotate(new Vector2(turned.x * Along, turned.y * Up), Turn);
        }

        /// <summary>The first transform's turn in Unity: the plane's own turn about the up axis (yaw: forward toward port), then Turn in it.</summary>
        public Quaternion RootTurn(float yaw) => new Quaternion(0f, Mathf.Sin(-yaw / 2f), 0f, Mathf.Cos(-yaw / 2f)) * Pitch(Turn);

        /// <summary>The second transform's scale in Unity's axes: none across, Up on its up (kit z), Along on its forward (kit x).</summary>
        public Vector3 Stretch => new Vector3(1f, Up, Along);

        /// <summary>The cloth's own turn in Unity.</summary>
        public Quaternion BackTurn => Pitch(Back);

        // A turn in the plane, kit x toward kit z, as Unity's: about its x axis, the other way round (kit to Unity is a mirror).
        private static Quaternion Pitch(float angle) => new Quaternion(Mathf.Sin(-angle / 2f), 0f, 0f, Mathf.Cos(-angle / 2f));

        private static Vector2 Rotate(Vector2 p, float angle)
        {
            float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
            return new Vector2(c * p.x - s * p.y, s * p.x + c * p.y);
        }
    }
}
