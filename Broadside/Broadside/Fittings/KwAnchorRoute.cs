using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How an anchor's cable runs on deck from its windlass to a fairlead (<see cref="KwAnchorSpot"/>), in the windlass's kit
    /// frame: off the drum where it leaves the coil's round toward it (at the place along the coil nearest it, over the
    /// drum, or from under it when the windlass's rail is in the way over it), in a straight run with a little sag; or,
    /// only when the windlass's own frame or something on deck is in the way of that, first straight out from the drum to a
    /// deck lead (an eye on the deck) and from there to the fairlead (<see cref="KwAnchorPlan"/> counts the turn there as
    /// cable). Every run is checked every <see cref="Step"/> against the ship's parts (<see cref="KwAnchorShip"/>), the
    /// cable's thickness and a little more kept clear.
    /// </summary>
    public static class KwAnchorRoute
    {
        /// <summary>The cable's thickness, and the half of it with room to spare that is kept clear of everything.</summary>
        public const float Width = 0.045f, Clear = 0.05f;

        /// <summary>The top of the cable's coil on the drum.</summary>
        public static readonly Vector3 DrumTop = new Vector3(0f, 0f, KwAnchorShapes.Axle + KwAnchorShapes.Coiled);

        /// <summary>How far the cable may angle off square to the drum when it leaves it straight for a fairlead, as the
        /// tangent of the angle (45 degrees): past that it is led by a deck lead, straight off the drum, where one is clear.</summary>
        public const float Fleet = 1f;

        // How far apart a run is checked; how far along the drum the cable may leave from its middle (its room kept clear
        // of the flanges); how high a deck lead carries the cable over the deck, and how far out from the drum it may stand.
        private const float Step = 0.1f, CoilEnd = KwAnchorShapes.FlangeIn - Clear - 0.005f, LeadHigh = 0.14f;
        private static readonly float[] LeadOut = { 0.85f, 1.25f };

        /// <summary>
        /// The cable's fixed points from the drum to the fairlead's inner lip, or null with why not: straight off the drum
        /// (over it, else from under it) when it angles off square to it no more than <see cref="Fleet"/>; else by a deck
        /// lead; straight at a steeper angle only when no deck lead is clear either.
        /// </summary>
        public static List<Vector3>? Find(KwAnchorShip ship, Vector3 to, out string? why)
        {
            float way = to.x >= 0f ? 1f : -1f;
            bool square = Angle(to) <= Fleet;
            why = null;
            if (square && Straight(ship, way, to, ref why) is List<Vector3> straight)
                return straight;
            foreach (float out1 in LeadOut)
            {
                List<Vector3>? led = Led(ship, way, to, out1);
                if (led != null)
                    return led;
            }
            return square ? null : Straight(ship, way, to, ref why);
        }

        // Straight off the drum to the point, over it or else from under it; null, with what was in the way, when neither is clear.
        private static List<Vector3>? Straight(KwAnchorShip ship, float way, Vector3 to, ref string? why)
        {
            foreach (float off in new[] { way, -way })
            {
                List<Vector3> run = new List<Vector3> { Drum(off, to), to };
                string? blocked = Blocked(ship, run);
                if (blocked == null)
                    return run;
                why ??= blocked;
            }
            return null;
        }

        /// <summary>How far a cable straight off the drum to a point angles along the drum, as the tangent of its angle off square.</summary>
        public static float Angle(Vector3 to) => Mathf.Abs(to.y - LeaveAt(to)) / Mathf.Max(0.1f, new Vector2(to.x, to.z - KwAnchorShapes.Axle).magnitude);

        /// <summary>
        /// Where the cable leaves the coil toward a point, at the place along the coil nearest it between the flanges
        /// (<see cref="LeaveAt"/>): the tangent off the coil's outer turns there, over the drum for a way the same as the
        /// point's (1 a point ahead, -1 astern), from under it for the other way.
        /// </summary>
        public static Vector3 Drum(float way, Vector3 toward)
        {
            float y = LeaveAt(toward), radius = Off(toward, y);
            Vector2 p = new Vector2(way * toward.x, toward.z - KwAnchorShapes.Axle);
            float d = p.magnitude;
            float angle = d <= radius ? Mathf.PI / 2f : Mathf.Atan2(p.y, p.x) + Mathf.Acos(radius / d);
            return new Vector3(way * radius * Mathf.Cos(angle), y, KwAnchorShapes.Axle + radius * Mathf.Sin(angle));
        }

        /// <summary>
        /// Where along the coil the cable leaves toward a point: straight under it where the flanges allow; for a point out
        /// past a flange, far enough inboard that the cable angling out to it has risen over the flange's rim, with room, by
        /// the time it comes to the flange (its way off the coil and its drift along the drum both reckoned square to the axle).
        /// </summary>
        public static float LeaveAt(Vector3 toward)
        {
            float far = Mathf.Abs(toward.y);
            if (far <= CoilEnd)
                return toward.y;
            float radius = KwAnchorCoil.Outer + Width / 2f, rim = KwAnchorShapes.Flange + Clear;
            float d = new Vector2(toward.x, toward.z - KwAnchorShapes.Axle).magnitude, run = Mathf.Sqrt(Mathf.Max(0f, d * d - radius * radius));
            float f = Mathf.Sqrt(rim * rim - radius * radius) / Mathf.Max(run, 1e-3f);
            float inboard = f >= 1f ? 0f : (CoilEnd - far * f) / (1f - f);
            return Mathf.Sign(toward.y) * Mathf.Clamp(inboard, 0f, CoilEnd);
        }

        // How far from the axle the cable leaves the coil at y toward a point: on its outer turns, over the riding turns
        // when it crosses them on its way out along the drum, and bedded between the turns when it leaves square to the drum.
        private static float Off(Vector3 toward, float y)
        {
            float over = Mathf.Max(KwAnchorCoil.Surface(y), KwAnchorCoil.Surface(Mathf.Clamp(toward.y, -CoilEnd, CoilEnd)));
            float square = Mathf.Clamp01(1f - 20f * Angle(toward));
            return over + Width / 2f - KwAnchorCoil.Bed * square;
        }

        /// <summary>A point of a run from a to b with its sag, at t (0 to 1) along it.</summary>
        public static Vector3 Sagged(Vector3 a, Vector3 b, float t) => Vector3.Lerp(a, b, t) - new Vector3(0f, 0f, 4f * Sag(a, b) * t * (1f - t));

        /// <summary>How far a run sags at its middle: a little, as a cable with an anchor on it.</summary>
        public static float Sag(Vector3 a, Vector3 b) => Mathf.Min(0.025f * (b - a).magnitude, 0.1f);

        /// <summary>What is in the way of a run with its sag (null: nothing), its first point (on the drum) left out when asked.</summary>
        public static string? Run(KwAnchorShip ship, Vector3 a, Vector3 b, bool outboard, bool fromDrum)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt((b - a).magnitude / Step));
            List<KwAnchorSolid> near = ship.Near(a, b, Clear + Sag(a, b));
            for (int i = fromDrum ? 1 : 0; i <= steps; i++)
            {
                string? blocker = KwAnchorShip.Blocker(Sagged(a, b, i / (float)steps), Clear, outboard, near);
                if (blocker != null)
                    return blocker;
            }
            return null;
        }

        // What is in the way of a cable through these points on deck (the first on the drum).
        private static string? Blocked(KwAnchorShip ship, List<Vector3> points)
        {
            for (int i = 0; i + 1 < points.Count; i++)
            {
                string? blocker = Run(ship, points[i], points[i + 1], false, i == 0);
                if (blocker != null)
                    return blocker;
            }
            return null;
        }

        // Out from the drum to a deck lead on the deck, then to the fairlead; null where there is no deck there or a run is not clear.
        private static List<Vector3>? Led(KwAnchorShip ship, float way, Vector3 to, float out1)
        {
            float y = Mathf.Clamp(to.y, -CoilEnd, CoilEnd);
            float? deck = ship.DeckTop(way * out1, y, 0f, 0.3f);
            if (deck == null)
                return null;
            Vector3 lead = new Vector3(way * out1, y, deck.Value + LeadHigh);
            List<Vector3> points = new List<Vector3> { Drum(way, lead), lead, to };
            return Blocked(ship, points) == null ? points : null;
        }

        /// <summary>A cable's length through its points.</summary>
        public static float Length(List<Vector3> points)
        {
            float length = 0f;
            for (int i = 0; i + 1 < points.Count; i++)
                length += (points[i + 1] - points[i]).magnitude;
            return length;
        }
    }
}
