using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A sail's cloth swinging a little after its ship as it rolls, pitches and speeds up: the free cloth hangs toward
    /// where gravity pulls it, less the ship's own push (both in the cloth's axes, so a rolling ship tips it to the side and
    /// a pitching one fore and aft), and follows that as a pendulum its height would, damped by the cloth and the wind. Its
    /// sheets and the wind hold it, so it gives half as far as loose cloth would, and a sail the wind fills hard less
    /// still. The ship's push comes from its body's speed (its physics, smoothed over half a second), never from where it
    /// is drawn between frames, which jumps with the frame times. A sail ashore stands still.
    /// </summary>
    public sealed class KwSailSwing
    {
        private const float Gravity = 9.81f, Damping = 0.45f, Give = 0.5f, Most = 0.08f;

        private Vector3 lastSpeed, push, angle, turn;
        private float lastAt = -1f;

        /// <summary>
        /// The cloth's lean now, per metre it hangs (its own axes): its facing, its speed, the time now, the cloth's length,
        /// how slack it is (1 slack, less filled), whether it is a square sail (a sail along the ship leans only to its sides).
        /// </summary>
        public Vector3 Feel(Quaternion facing, Vector3 speed, float now, float length, float slack, bool square, float dt)
        {
            bool fresh = lastAt >= 0f && now - lastAt < 0.25f && dt > 0f;
            if (fresh)
                push = Vector3.Lerp(push, (speed - lastSpeed) / dt, Mathf.Clamp01(dt * 2.5f));
            (lastSpeed, lastAt) = (speed, now);
            Quaternion back = new Quaternion(-facing.x, -facing.y, -facing.z, facing.w);
            Vector3 down = back * (new Vector3(0f, -Gravity, 0f) - push);
            Spring(Lean(down, square) * (Give * slack), length, dt);
            return angle;
        }

        // How far hanging cloth leans, per metre down, toward where gravity (less the ship's push) points in its own axes.
        private static Vector3 Lean(Vector3 down, bool square)
        {
            Vector3 g = down.normalized;
            float up = Mathf.Max(-g.y, 0.3f);
            return Vector3.ClampMagnitude(new Vector3(g.x / up, 0f, square ? g.z / up : 0f), 0.3f);
        }

        // The cloth swings after where it would hang as a pendulum its length does.
        private void Spring(Vector3 target, float length, float dt)
        {
            float w = Mathf.Sqrt(Gravity / Mathf.Max(length, 1f)), step = Mathf.Min(dt, 0.1f);
            turn += (w * w * (target - angle) - 2f * Damping * w * turn) * step;
            angle = Vector3.ClampMagnitude(angle + turn * step, Most);
        }
    }
}
