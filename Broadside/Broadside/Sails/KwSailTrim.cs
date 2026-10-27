using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How a launched ship's fore-and-aft sails are trimmed to the apparent wind (<see cref="KwApparentWind"/>): which side
    /// they lie to (leeward, kept until the wind is well over the other way, so they tack and gybe across only when it
    /// crosses the bow or the stern), how far off the centre line a boom is let out (sheeted in close-hauled, eased as the
    /// wind comes aft, out against the shrouds on a run), how far a jib's foot is sheeted out, and the turn about its stay
    /// that sets it there. Angles in degrees, a boom's and a foot's off the line aft from the mast or the tack. Pure
    /// arithmetic, the same on every screen.
    /// </summary>
    public static class KwSailTrim
    {
        /// <summary>A boom's least and most angle off the centre line, and a jib foot's.</summary>
        public const float BoomIn = 3f, BoomOut = 82f, FootIn = 6f, FootOut = 28f;

        // How far across the bow or stern the wind must come (the sine of its angle off the line) before the sails go over.
        private const float Over = 0.1f;

        /// <summary>The side the sails lie to, +1 port, -1 starboard: leeward of the wind's across (its signed sine, plus from starboard), else the side they lay to.</summary>
        public static int Side(int current, float across) => across > Over ? 1 : across < -Over ? -1 : current == 0 ? 1 : current;

        /// <summary>A boom's angle off the centre line with the apparent wind that far off the bow: a little over half of it, less 8°, from 3° close-hauled to 82° running.</summary>
        public static float Boom(float angle) => Mathf.Clamp(0.55f * angle - 8f, BoomIn, BoomOut);

        /// <summary>
        /// A jib's foot's angle off the centre line with the apparent wind that far off the bow: sheeted in close-hauled,
        /// out no further than a sheet to the ship's side takes it (turned further about its stay, its clew would climb).
        /// </summary>
        public static float Foot(float angle) => Mathf.Clamp(0.35f * angle - 4f, FootIn, FootOut);

        /// <summary>A boom's turn about its mast that far (degrees, plus to port) in the piece's own axes (Unity's: about up).</summary>
        public static Quaternion Boomed(float degrees) => KwTurns.About(degrees, Vector3.up);

        /// <summary>
        /// A jib's turn about its stay (in its bowsprit's own axes, Unity's) that sets its foot that far off the line aft
        /// (degrees, plus to port; <see cref="JibTurn"/>): the kit's right-handed turn is the other way in Unity's mirrored axes.
        /// </summary>
        public static Quaternion Sheeted(KwJibCut cut, float foot) => KwTurns.About(-JibTurn(cut.Alpha, cut.Beta, -foot), KwMesh.ToUnity(cut.StayDir));

        /// <summary>
        /// The turn (degrees, about the stay, right-handed in the bowsprit's kit axes: plus takes the foot to starboard) that
        /// sets a jib's foot that far off the line aft from its tack (plus to starboard), its stay rising at alpha and its
        /// foot at beta (radians).
        /// </summary>
        public static float JibTurn(float alpha, float beta, float foot)
        {
            float want = Mathf.Abs(foot) * Mathf.Deg2Rad, lo = 0f, hi = 0.75f * Mathf.PI;
            for (int i = 0; i < 24; i++)
            {
                float mid = (lo + hi) / 2f;
                if (FootAt(alpha, beta, mid) < want)
                    lo = mid;
                else
                    hi = mid;
            }
            return Mathf.Sign(foot) * (lo + hi) / 2f * Mathf.Rad2Deg;
        }

        /// <summary>How far off the line aft (radians) a jib's foot lies turned that far (radians) about its stay.</summary>
        public static float FootAt(float alpha, float beta, float turn)
        {
            float c = Mathf.Cos(turn), across = Mathf.Sin(alpha - beta) * Mathf.Sin(turn);
            float aft = Mathf.Cos(beta) * c + Mathf.Cos(alpha) * Mathf.Cos(alpha - beta) * (1f - c);
            return Mathf.Atan2(across, aft);
        }
    }
}
