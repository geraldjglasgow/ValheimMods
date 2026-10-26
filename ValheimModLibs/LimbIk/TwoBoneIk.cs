using UnityEngine;

namespace LimbIk
{
    /// <summary>
    /// Bends a two-bone limb so its end reaches a target (the law of cosines at the elbow, then the whole limb turned
    /// onto the target), rolling it about the shoulder-to-end line until the elbow points at the hint. Works on the
    /// bones' world rotations, so it runs after the animator has posed them (a LateUpdate); a target out of reach is
    /// met as far as the limb stretches, never past it.
    /// </summary>
    public static class TwoBoneIk
    {
        private const float Epsilon = 1e-6f;

        /// <summary>
        /// The limb's upper bone, lower bone and end (a shoulder, elbow and hand, or a hip, knee and foot) bent so the
        /// end's origin is on the target, the middle joint toward the hint (a point in the world).
        /// </summary>
        public static void Solve(Transform upper, Transform lower, Transform end, Vector3 target, Vector3 hint)
        {
            Vector3 a = upper.position, b = lower.position, c = end.position;
            float ab = (b - a).magnitude, bc = (c - b).magnitude;
            float at = Mathf.Clamp((target - a).magnitude, Mathf.Abs(ab - bc) + 0.001f, ab + bc - 0.001f);
            float bend = 0.5f * (Angle((c - a).magnitude, ab, bc) - Angle(at, ab, bc));
            Vector3 axis = Vector3.Cross(b - a, c - b);
            if (axis.sqrMagnitude < Epsilon)
                axis = Vector3.Cross(hint - a, c - b);
            lower.rotation = Quaternion.AngleAxis(2f * bend * Mathf.Rad2Deg, axis.normalized) * lower.rotation;
            upper.rotation = Quaternion.FromToRotation(end.position - a, target - a) * upper.rotation;
            TurnElbow(upper, lower, end, hint);
        }

        // The limb rolled about the line from its root to its end, so the middle joint points at the hint.
        private static void TurnElbow(Transform upper, Transform lower, Transform end, Vector3 hint)
        {
            Vector3 a = upper.position;
            Vector3 line = (end.position - a).normalized;
            Vector3 elbow = Vector3.ProjectOnPlane(lower.position - a, line);
            Vector3 wanted = Vector3.ProjectOnPlane(hint - a, line);
            if (elbow.sqrMagnitude > Epsilon && wanted.sqrMagnitude > Epsilon)
                upper.rotation = Quaternion.FromToRotation(elbow, wanted) * upper.rotation;
        }

        // The angle opposite side a in a triangle of sides a, b and c, in radians.
        private static float Angle(float a, float b, float c) => Mathf.Acos(Mathf.Clamp((b * b + c * c - a * a) / (2f * b * c), -1f, 1f));
    }
}
