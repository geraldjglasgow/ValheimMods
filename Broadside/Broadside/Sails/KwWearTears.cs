using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The wear's numbers (<see cref="KwSailWear"/>): hashes and smooth noise from a fixed seed, never a random number, so
    /// a worn sail is the same on every machine and in every game; a torn rim; and a texel's shading.
    /// </summary>
    internal static class KwWearNoise
    {
        private const uint Seed = 0x4B57u;

        /// <summary>A number from 0 to 1 for a pair of whole numbers, always the same for the same pair.</summary>
        public static float Hash(int a, int b)
        {
            unchecked
            {
                uint h = (uint)a * 0x9E3779B1u ^ (uint)b * 0x85EBCA77u ^ Seed;
                h ^= h >> 16;
                h *= 0x7FEB352Du;
                h ^= h >> 15;
                h *= 0x846CA68Bu;
                h ^= h >> 16;
                return (h >> 8) / 16777216f;
            }
        }

        /// <summary>Smooth noise along a line, 0 to 1, a new value at every whole x.</summary>
        public static float Line(float x, int salt)
        {
            int i = Mathf.FloorToInt(x);
            return Mathf.Lerp(Hash(i, salt), Hash(i + 1, salt), Ease(x - i));
        }

        /// <summary>Smooth noise over a plane, 0 to 1, a new value at every whole point.</summary>
        public static float Field(Vector2 p, int salt)
        {
            int i = Mathf.FloorToInt(p.x), j = Mathf.FloorToInt(p.y), a = j * 7919 + salt, b = a + 7919;
            float fx = Ease(p.x - i), fy = Ease(p.y - j);
            float low = Mathf.Lerp(Hash(i, a), Hash(i + 1, a), fx), high = Mathf.Lerp(Hash(i, b), Hash(i + 1, b), fx);
            return Mathf.Lerp(low, high, fy);
        }

        /// <summary>0 at x 0 or less, 1 at x 1 or more, smooth between.</summary>
        public static float Ease(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        /// <summary>
        /// A torn rim, u how far out of the tear in rims (under 0 in it): cut out in the tear and here and there near it
        /// (frayed, as much as fray allows), darkened toward the tear.
        /// </summary>
        public static bool Rim(float u, KwWearTexel t, int salt, float fray, ref float shade)
        {
            if (u < 0f)
                return true;
            if (u >= 1f)
                return false;
            float near = 1f - u;
            if (Hash(t.I, t.J * 977 + salt) < 0.4f * fray * near * near * near)
                return true;
            shade *= Mathf.Lerp(0.62f, 1f, u * u);
            return false;
        }

        /// <summary>A colour times a shade each way, its alpha set.</summary>
        public static Color32 Shade(Color32 c, Vector3 tone, byte alpha) => new Color32(Byte(c.r * tone.x), Byte(c.g * tone.y), Byte(c.b * tone.z), alpha);

        public static Color32 Shade(Color32 c, float shade) => Shade(c, new Vector3(shade, shade, shade), c.a);

        private static byte Byte(float v) => (byte)Mathf.Clamp(Mathf.Round(v), 0f, 255f);
    }

    /// <summary>
    /// A hole worn through the cloth (<see cref="KwSailWear"/>): roughly round, a little longer one way, its outline
    /// wandering broadly and jagged finely; inside it the cloth is cut out, round it a darker frayed rim. Places and sizes
    /// in heights of the sail.
    /// </summary>
    internal sealed class KwWearHole
    {
        private const int Jags = 36;
        private readonly Vector2 centre, way;
        private readonly float radius, stretch, rim, reach;
        private readonly Vector3 phase;
        private readonly int salt;

        public KwWearHole(Vector2 centre, float radius, float stretch, float turn, int salt)
        {
            (this.centre, this.radius, this.stretch, this.salt) = (centre, radius, stretch, salt);
            way = new Vector2(Mathf.Cos(turn * Mathf.Deg2Rad), Mathf.Sin(turn * Mathf.Deg2Rad));
            rim = Mathf.Max(0.006f, 0.3f * radius);
            reach = stretch * (1.4f * radius + rim);
            phase = new Vector3(KwWearNoise.Hash(salt, 1), KwWearNoise.Hash(salt, 2), KwWearNoise.Hash(salt, 3)) * (2f * Mathf.PI);
        }

        /// <summary>Whether the hole cuts a texel out; a texel on its rim darkened (shade made less).</summary>
        public bool Cut(KwWearTexel t, ref float shade)
        {
            Vector2 q = t.On - centre;
            if (q.sqrMagnitude > reach * reach)
                return false;
            float a = Vector2.Dot(q, way) / stretch, b = Vector2.Dot(q, new Vector2(-way.y, way.x));
            float u = (Mathf.Sqrt(a * a + b * b) - Edge(Mathf.Atan2(b, a))) / rim;
            return KwWearNoise.Rim(u, t, salt, 1f, ref shade);
        }

        // The hole's edge at an angle round it: its radius wandering broadly and jagged finely, meeting itself all round.
        private float Edge(float angle)
        {
            float broad = 0.14f * Mathf.Sin(2f * angle + phase.x) + 0.09f * Mathf.Sin(3f * angle + phase.y) + 0.05f * Mathf.Sin(5f * angle + phase.z);
            float bin = (angle / (2f * Mathf.PI) + 0.5f) * Jags;
            int k = Mathf.Clamp((int)bin, 0, Jags);
            float jag = Mathf.Lerp(KwWearNoise.Hash(k % Jags, salt), KwWearNoise.Hash((k + 1) % Jags, salt), bin - k) - 0.5f;
            return radius * (1f + broad + 0.18f * jag);
        }
    }

    /// <summary>
    /// A free edge of the cloth torn ragged (<see cref="KwSailWear"/>: a square sail's side or foot, any other sail's
    /// leech): frayed all along, a few rips deep into it, whole near its corners, where the ropes hold the cloth; nothing
    /// beyond it. Its rim darkened and frayed as a hole's is, the cloth grimy along it. Places in heights of the sail.
    /// </summary>
    internal sealed class KwWearEdge
    {
        private const float Grime = 0.06f;
        private readonly Vector2 from, along, inward;
        private readonly float length, scale, ends;
        private readonly Vector4[] rips;
        private readonly int salt;

        public KwWearEdge(Vector2 from, Vector2 to, float scale, int salt)
        {
            (this.from, this.scale, this.salt) = (from, scale, salt);
            length = (to - from).magnitude;
            along = (to - from) / Mathf.Max(length, 1e-5f);
            inward = new Vector2(-along.y, along.x);
            ends = Mathf.Max(Mathf.Min(0.07f, 0.25f * length), 1e-4f);
            rips = Rips(length, scale, salt);
        }

        // About two rips a sail height along the edge, spread along it, of different depths and widths, each wider on one
        // side than the other: x where, y how deep, z and w how wide before and after it.
        private static Vector4[] Rips(float length, float scale, int salt)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(length * 2.2f));
            Vector4[] rips = new Vector4[count];
            for (int n = 0; n < count; n++)
            {
                float at = (n + 0.2f + 0.6f * KwWearNoise.Hash(n, salt)) / count * length, wide = 0.012f + 0.02f * KwWearNoise.Hash(n, salt + 2);
                float lean = 0.5f + KwWearNoise.Hash(n, salt + 3);
                rips[n] = new Vector4(at, scale * (0.03f + 0.05f * KwWearNoise.Hash(n, salt + 1)), scale * wide * lean, scale * wide / lean);
            }
            return rips;
        }

        /// <summary>Whether the torn edge cuts a texel out; a texel near it darkened (shade made less).</summary>
        public bool Cut(KwWearTexel t, ref float shade)
        {
            Vector2 q = t.On - from;
            float inside = Vector2.Dot(q, inward);
            if (inside < 0f)
                return true;
            if (inside > 0.15f)
                return false;
            float s = Mathf.Clamp(Vector2.Dot(q, along), 0f, length), whole = Whole(s);
            shade *= Mathf.Lerp(0.86f, 1f, inside / Grime);
            return KwWearNoise.Rim((inside - Depth(s) * whole) / (0.004f + 0.008f * scale), t, salt, whole, ref shade);
        }

        // How deep the edge is torn s along it: a fray all along and the rips.
        private float Depth(float s)
        {
            float fray = 0.003f + 0.011f * KwWearNoise.Line(s * 28f, salt) + 0.006f * KwWearNoise.Line(s * 105f, salt + 7);
            float rip = 0f;
            foreach (Vector4 r in rips)
                rip = Mathf.Max(rip, r.y * Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(s - r.x) / (s < r.x ? r.z : r.w)), 1.3f));
            return scale * fray + rip;
        }

        // How much of the tear there is s along the edge: none at its corners, all of it a little way from them.
        private float Whole(float s) => KwWearNoise.Ease(s / ends) * KwWearNoise.Ease((length - s) / ends);
    }
}
