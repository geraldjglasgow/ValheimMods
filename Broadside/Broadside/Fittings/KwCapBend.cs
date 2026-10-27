using System;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A long neck made longer or shorter and leaning forward or back (<see cref="KwCapSpec.Length"/>,
    /// <see cref="KwCapSpec.Lean"/>), as a warp of space in the plane of up and forward, by the height a point had as carved.
    /// Below the neck's bottom (the foot's top) nothing moves. From there to its top (a carved head's <c>head_start</c>, a
    /// scroll's or tail's stem top) the neck's middle line (<see cref="KwCapHead.Middle"/>; straight up for the code's
    /// stems, which bend their own line) is stretched along its height and turned by an angle growing with the height, both
    /// easing in and out over a fifth of the neck at each end (<see cref="Share"/>), so it leaves the foot and meets the head
    /// without a crease and reaches the whole lean at the top; a point keeps its offset forward of that middle, turned with
    /// the line there, so the neck bends about its own middle and its front and back are pressed and drawn out alike.
    /// Everything above the top moves whole with the line's top (<see cref="Whole"/>), so a head or a curl keeps its shape.
    /// Points as (up, forward) pairs, so a head's model (Unity y, z) and the code's caps (kit z, x) bend alike. Pure
    /// arithmetic.
    /// </summary>
    public sealed class KwCapBend
    {
        private const int Steps = 64;

        // The share of a neck at each end over which its bend and stretch ease in; and the middle's rate that this leaves.
        private const float Ease = 0.2f, Peak = 1f / (1f - Ease);
        private readonly float bottom, top, turn, length;
        private readonly Func<float, float> middle;

        // The bent middle line at each of the steps from bottom to top: (up, forward).
        private readonly Vector2[] line = new Vector2[Steps + 1];

        public KwCapBend(float bottom, float top, float degrees, float length = 1f, Func<float, float>? middle = null)
        {
            this.bottom = bottom;
            this.top = Mathf.Max(top, bottom + 1e-3f);
            (turn, this.length, this.middle) = (degrees * Mathf.Deg2Rad, length, middle ?? (_ => 0f));
            float step = (this.top - bottom) / Steps;
            line[0] = new Vector2(bottom, Middle(bottom));
            for (int i = 0; i < Steps; i++)
                line[i + 1] = line[i] + Along(bottom + i * step, bottom + (i + 1) * step);
        }

        /// <summary>A point warped, and how it moves with its height (dUp) and its forward offset (dForward), each (up, forward).</summary>
        public Vector2 At(float up, float forward, out Vector2 dUp, out Vector2 dForward)
        {
            float angle = Angle(up), off = forward - Middle(up), t = Part(up);
            dUp = Way(angle) * (Stretch(t) - off * Rate(t));
            dForward = Ahead(angle);
            return Line(up) + Ahead(angle) * off;
        }

        /// <summary>A point warped: (up, forward).</summary>
        public Vector2 At(float up, float forward) => At(up, forward, out _, out _);

        /// <summary>A point of what stands on the neck's top moved whole with it, whatever its height (a curl hanging below the top).</summary>
        public Vector2 Whole(float up, float forward) => line[Steps] + Way(turn) * (up - top) + Ahead(turn) * (forward - Middle(top));

        /// <summary>A kit point (x forward, y across, z up) warped by its height, or moved whole with the neck's top.</summary>
        public Vector3 Kit(Vector3 p, bool whole = false)
        {
            Vector2 bent = whole ? Whole(p.z, p.x) : At(p.z, p.x);
            return new Vector3(bent.y, p.y, bent.x);
        }

        /// <summary>
        /// How much of a neck's bend and stretch lies below share t of its height: 0 at its foot, 1 at its top, its rate
        /// (<see cref="Rate(float)"/>) rising as a smoothstep over the first fifth, level in the middle, falling over the last.
        /// </summary>
        public static float Share(float t)
        {
            if (t <= 0f || t >= 1f)
                return Mathf.Clamp01(t);
            if (t < Ease)
                return Peak * Ease * Eased(t / Ease);
            return t > 1f - Ease ? 1f - Peak * Ease * Eased((1f - t) / Ease) : Peak * (Ease / 2f + t - Ease);
        }

        // The integral of a smoothstep from 0 to u.
        private static float Eased(float u) => u * u * u - u * u * u * u / 2f;

        // The rate of the share at t: 0 at both ends, the peak in the middle.
        private static float Density(float t)
        {
            if (t <= 0f || t >= 1f)
                return 0f;
            float u = Mathf.Min(t, 1f - t) / Ease;
            return u >= 1f ? Peak : Peak * u * u * (3f - 2f * u);
        }

        // The share of the neck at a height, as carved.
        private float Part(float up) => Mathf.Clamp01((up - bottom) / (top - bottom));

        private float Angle(float up) => turn * Share(Part(up));

        // How fast the turn grows with height (radians a metre as carved), and how much the neck is stretched there.
        private float Rate(float t) => turn * Density(t) / (top - bottom);

        private float Stretch(float t) => 1f + (length - 1f) * Density(t);

        private float Middle(float up) => middle(Mathf.Clamp(up, bottom, top));

        // The bent middle line at a height: straight below the bottom, on from the top along its last way above it.
        private Vector2 Line(float up)
        {
            if (up <= bottom)
                return new Vector2(up, Middle(bottom));
            if (up >= top)
                return line[Steps] + Way(turn) * (up - top);
            float step = (top - bottom) / Steps;
            int i = Mathf.Min((int)((up - bottom) / step), Steps - 1);
            return line[i] + Along(bottom + i * step, up);
        }

        // The middle line from one height to another (as carved): stretched exactly (the share's integral), turned as at their middle.
        private Vector2 Along(float from, float to)
        {
            float stretched = to - from + (length - 1f) * (top - bottom) * (Share(Part(to)) - Share(Part(from)));
            return Way(Angle((from + to) / 2f)) * stretched + Ahead(Angle((from + to) / 2f)) * (Middle(to) - Middle(from));
        }

        // The line's way at a turn (up, forward), and square to it, forward.
        private static Vector2 Way(float angle) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        private static Vector2 Ahead(float angle) => new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle));
    }
}
