using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A rough patch of hide sewn on the cloth (<see cref="KwSailWear"/>): a piece of the same hide from elsewhere in it,
    /// undyed (sewn on after the sail was painted), lighter or darker, cut roughly square and a little askew, its edge
    /// shadowed, crude stitches across its edge all round. Places in heights of the sail.
    /// </summary>
    internal sealed class KwWearPatch
    {
        private const float Seam = 0.005f, Shadow = 0.004f, Stitch = 0.012f, Gap = 0.036f;
        private static readonly Vector2[] Square = { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(1f, 1f), new Vector2(-1f, 1f) };
        private readonly Vector2[] outline;
        private readonly Vector3 tone;
        private readonly int dx, dy;

        private KwWearPatch(Vector2 centre, Vector2[] outline, Vector3 tone, int dx, int dy, float reach)
        {
            (Centre, this.outline, this.tone, this.dx, this.dy, Reach) = (centre, outline, tone, dx, dy, reach);
        }

        public Vector2 Centre { get; }

        /// <summary>How far from its centre the patch and its stitches reach at most.</summary>
        public float Reach { get; }

        /// <summary>A patch half w wide and h high at a centre, turned (degrees), its corners cut unevenly; tone its hide's shade.</summary>
        public static KwWearPatch Of(Vector2 centre, Vector2 half, float turn, Vector3 tone, int salt)
        {
            Vector2 x = new Vector2(Mathf.Cos(turn * Mathf.Deg2Rad), Mathf.Sin(turn * Mathf.Deg2Rad)), y = new Vector2(-x.y, x.x);
            Vector2[] outline = new Vector2[Square.Length];
            for (int n = 0; n < Square.Length; n++)
            {
                Vector2 c = Vector2.Scale(Square[n], half) * (0.85f + 0.3f * KwWearNoise.Hash(n, salt));
                outline[n] = centre + x * c.x + y * c.y;
            }
            int dx = 40 + (int)(90f * KwWearNoise.Hash(salt, 9)), dy = 30 + (int)(90f * KwWearNoise.Hash(salt, 10));
            return new KwWearPatch(centre, outline, tone, dx, dy, 1.2f * half.magnitude + Stitch);
        }

        /// <summary>A texel with the patch over it: its own hide inside, a shadow along its edge, the stitches across it.</summary>
        public Color32 Over(Color32 c, KwWearTexel t)
        {
            if ((t.On - Centre).sqrMagnitude > Reach * Reach)
                return c;
            float inside = Inside(t.On, out float along);
            if (inside < -Stitch)
                return c;
            Color32 under = inside >= 0f ? KwWearNoise.Shade(t.OwnAt(dx, dy), tone, 255) : c;
            float shade = inside >= 0f ? (inside < Seam ? 0.66f : 1f) : (inside > -Shadow ? 0.75f : 1f);
            if (Mathf.Abs(inside) < Stitch && Mathf.Repeat(along / Gap, 1f) < 0.32f)
                shade = 0.3f;
            return KwWearNoise.Shade(under, shade);
        }

        // How far inside the patch's edge a place is (under 0 outside), and how far along the nearest side.
        private float Inside(Vector2 p, out float along)
        {
            float least = float.MaxValue;
            along = 0f;
            for (int i = 0; i < outline.Length; i++)
            {
                Vector2 a = outline[i], way = (outline[(i + 1) % outline.Length] - a).normalized;
                float d = Vector2.Dot(p - a, new Vector2(-way.y, way.x));
                if (d < least)
                    (least, along) = (d, Vector2.Dot(p - a, way));
            }
            return least;
        }
    }

    /// <summary>
    /// A tear sewn shut (<see cref="KwSailWear"/>): a dark jagged line with crude stitches across it, from under a patch.
    /// Places in heights of the sail.
    /// </summary>
    internal sealed class KwWearSlit
    {
        private const float Line = 0.0035f, Cross = 0.011f, Gap = 0.022f;
        private readonly Vector2[] points;
        private readonly Vector2 middle;
        private readonly float reach;

        private KwWearSlit(Vector2[] points)
        {
            this.points = points;
            middle = (points[0] + points[points.Length - 1]) / 2f;
            reach = (points[points.Length - 1] - points[0]).magnitude / 2f + 0.03f;
        }

        /// <summary>A sewn tear from a place, that way and that long (shortened to stay well inside the cloth); null if too short.</summary>
        public static KwWearSlit? Of(Vector2 from, Vector2 way, float length, Vector2[] outline, int salt)
        {
            while (length > 0.05f && KwSailFrame.Inside(outline, from + way * length) < 0.03f)
                length *= 0.85f;
            if (length <= 0.05f)
                return null;
            Vector2 side = new Vector2(-way.y, way.x);
            Vector2[] points = new Vector2[5];
            for (int n = 0; n < points.Length; n++)
            {
                float off = n == 0 ? 0f : (KwWearNoise.Hash(n, salt) - 0.5f) * 0.12f * length;
                points[n] = from + way * (length * n / (points.Length - 1)) + side * off;
            }
            return new KwWearSlit(points);
        }

        /// <summary>A texel with the sewn tear over it.</summary>
        public Color32 Over(Color32 c, Vector2 p)
        {
            if ((p - middle).sqrMagnitude > reach * reach)
                return c;
            float d = Distance(p, out float along);
            if (d > Cross)
                return c;
            return KwWearNoise.Shade(c, d < Line ? 0.28f : Mathf.Repeat(along / Gap, 1f) < 0.3f ? 0.36f : 1f);
        }

        // How far a place is from the tear, and how far along it the nearest point is.
        private float Distance(Vector2 p, out float along)
        {
            float least = float.MaxValue, walked = 0f;
            along = 0f;
            for (int i = 0; i + 1 < points.Length; i++)
            {
                Vector2 a = points[i], ab = points[i + 1] - a;
                float length = ab.magnitude, s = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(length * length, 1e-8f));
                float d = (p - (a + ab * s)).magnitude;
                if (d < least)
                    (least, along) = (d, walked + s * length);
                walked += length;
            }
            return least;
        }
    }

    /// <summary>
    /// A stain on the hide or a spot worn thin (<see cref="KwSailWear"/>), its outline blotchy: darker, a darker ring (a
    /// dried water mark) or lighter. Places in heights of the sail.
    /// </summary>
    internal sealed class KwWearStain
    {
        public const int Dark = 0, Ring = 1, Thin = 2;
        private static readonly Vector3 Pale = new Vector3(232f, 214f, 184f);
        private readonly Vector2 centre;
        private readonly float radius, strength;
        private readonly int kind, salt;

        public KwWearStain(Vector2 centre, float radius, float strength, int kind, int salt)
        {
            (this.centre, this.radius, this.strength, this.kind, this.salt) = (centre, radius, strength, kind, salt);
        }

        /// <summary>A texel with the stain over it.</summary>
        public Color32 Over(Color32 c, Vector2 p)
        {
            Vector2 q = (p - centre) / radius;
            if (q.sqrMagnitude > 2.5f)
                return c;
            float d = q.magnitude + 0.55f * (KwWearNoise.Field(q * 2.5f, salt) - 0.5f);
            if (kind == Thin)
                return Paler(c, strength * KwWearNoise.Ease((1f - d) / 0.7f));
            float w = kind == Ring ? Mathf.Exp(-Mathf.Pow((d - 0.8f) / 0.16f, 2f)) : KwWearNoise.Ease((1f - d) / 0.6f);
            float dark = strength * w;
            return KwWearNoise.Shade(c, new Vector3(1f - dark, 1f - 1.08f * dark, 1f - 1.2f * dark), c.a);
        }

        // A colour a share of the way to the pale of hide worn thin.
        private static Color32 Paler(Color32 c, float share) => new Color32(
            (byte)Mathf.Round(Mathf.Lerp(c.r, Pale.x, share)), (byte)Mathf.Round(Mathf.Lerp(c.g, Pale.y, share)), (byte)Mathf.Round(Mathf.Lerp(c.b, Pale.z, share)), c.a);
    }
}
