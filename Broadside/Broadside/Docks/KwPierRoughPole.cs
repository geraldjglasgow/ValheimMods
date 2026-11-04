using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A rough wood dock's piling (<see cref="KwPierRoughFrame"/>): a crooked round pole with bark patches (wood's pole
    /// strips), its own thickness (<see cref="ThinLeast"/> to <see cref="ThickMost"/> of the material's post), a little
    /// thicker at its foot, its top just under the cap beam a little off its mark, leaning a few degrees inward or along
    /// the dock (never out past the deck's sides, where a ladder or a moored ship comes, nor out past its ends at the first
    /// and last stations, where a ladder may hang) down to its saved foot, bent twice on the way; and now and then lashed to the cap beam with rope, three turns round its top and two loops over the beam
    /// (<see cref="KwPierRope"/>). Its draws are taken whether it stands or not (a station with no piling there), so the
    /// rest of the dock is the same. The collider stays <see cref="KwPierBody"/>'s straight box. Plain geometry.
    /// </summary>
    public static class KwPierRoughPole
    {
        private const float ThinLeast = 0.8f, ThickMost = 1.3f, LeanLeast = 1f, LeanMost = 3.5f, LeanFarthest = 0.3f, Crook = 0.04f;
        private const float LashShare = 0.45f, Overlap = 0.02f, Turn = 0.028f;

        /// <summary>A station's piling on one side (-1 left, 1 right), its cap beam's middle and half width given (z 0: none there).</summary>
        public static void Piling(KwPierMesh mesh, KwPierSpec spec, KwPierKind kind, KwPierUv uv, System.Random random, int station, float side, Vector3 cap)
        {
            float scale = KwPierRough.Draw(random, ThinLeast, ThickMost), inward = KwPierRough.Draw(random, 0f, 0.03f), shift = KwPierRough.Draw(random, -0.025f, 0.025f);
            float lean = KwPierRough.Draw(random, LeanLeast, LeanMost) * Mathf.Deg2Rad, way = KwPierRough.Draw(random, 0f, 2f * Mathf.PI);
            Vector2 crook = new Vector2(KwPierRough.Draw(random, -Crook, Crook), KwPierRough.Draw(random, -Crook, Crook));
            bool lashed = random.NextDouble() < LashShare;
            float depth = side < 0f ? spec.Depths[station].x : spec.Depths[station].y, radius = kind.Post * 0.5f * scale;
            Vector3 top = new Vector3(side * (spec.Width * 0.5f - radius - 0.02f - inward), kind.CapFoot + 0.04f, 0f);
            top.z = Mathf.Clamp(KwPierShape.PostZ(spec, kind, station) + shift, radius, spec.Length - radius);
            float tall = top.y + depth;
            if (tall < KwPierShape.MinPiling + 0.04f)
                return;
            float end = station == 0 ? 1f : station == spec.Stations - 1 ? -1f : 0f;
            Vector3 foot = top + Leaning(way, new Vector2(side, end), Mathf.Min(LeanFarthest, Mathf.Tan(lean) * tall)) + Vector3.down * tall;
            Pole(mesh, uv, top, foot, radius, tall > 1.2f ? Inward(crook, end) : Vector2.zero);
            if (lashed && cap.z > 0.5f && tall > 0.5f)
                Lash(mesh, kind, uv, top, radius, cap);
        }

        // A level lean that way, turned back where it would lean out past the deck's side (inward: x the side, -1 or 1) or
        // past an end (y: 1 at the start, where inward is along, -1 at the end, 0 between).
        private static Vector3 Leaning(float way, Vector2 inward, float far)
        {
            float x = Mathf.Cos(way), z = Mathf.Sin(way);
            if (x * inward.x > 0f)
                x = -x;
            if (z * inward.y < 0f)
                z = -z;
            return new Vector3(x, 0f, z) * far;
        }

        // Bends along the dock kept inward at an end station (end: 1 at the start, -1 at the end, 0 between).
        private static Vector2 Inward(Vector2 crook, float end) =>
            end == 0f ? crook : new Vector2(Mathf.Abs(crook.x), Mathf.Abs(crook.y)) * end;

        // The pole in three lengths, bent along the dock at its thirds (crook), each overlapping the next a little so no
        // crack shows at a bend; its top capped on the end grain.
        private static void Pole(KwPierMesh mesh, KwPierUv uv, Vector3 top, Vector3 foot, float radius, Vector2 crook)
        {
            Vector3 m1 = Vector3.Lerp(top, foot, 1f / 3f) + new Vector3(0f, 0f, crook.x), m2 = Vector3.Lerp(top, foot, 2f / 3f) + new Vector3(0f, 0f, crook.y);
            Vector3[] points = { top, m1, m2, foot };
            for (int i = 0; i < 3; i++)
            {
                Vector3 a = points[i], b = points[i + 1], way = (b - a).normalized;
                float ra = radius * (1f + 0.04f * i), rb = radius * (1.04f + 0.04f * i);
                mesh.Round(i > 0 ? a - way * Overlap : a, i < 2 ? b + way * Overlap : b, ra, rb, uv.Pole, uv, i == 0, false);
            }
        }

        // Rope round the pole's top under the cap beam (three turns) and over the beam (two loops), binding them.
        private static void Lash(KwPierMesh mesh, KwPierKind kind, KwPierUv uv, Vector3 top, float radius, Vector3 cap)
        {
            float under = kind.CapFoot - 0.03f, round = radius + KwPierRope.Thick;
            for (int k = 0; k < 3; k++)
                KwPierRope.Tube(mesh, KwPierRope.Round(new Vector3(top.x, under - k * Turn, top.z), round, k % 2 == 0 ? 0.012f : -0.012f), true, uv);
            float over = kind.JoistFoot + KwPierRope.Thick, below = under - 3f * Turn, t = KwPierRope.Thick;
            foreach (float dx in new[] { -0.03f, 0.03f })
            {
                float x = top.x + dx;
                List<Vector3> loop = new List<Vector3>
                {
                    new Vector3(x, over, cap.x - cap.y - t), new Vector3(x, over, cap.x + cap.y + t),
                    new Vector3(x, below, top.z + round), new Vector3(x, below, top.z - round),
                };
                KwPierRope.Tube(mesh, loop, true, uv);
            }
        }
    }
}
