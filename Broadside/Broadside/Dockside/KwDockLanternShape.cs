using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A dock lantern's post (<see cref="KwDockLanternPiece"/>), made in code like the dock's pilings, in its dock's material
    /// and atlas (<see cref="KwPierLooks"/>, <see cref="KwPierUv"/>), in the piece's frame: y up from the deck's top, z in
    /// over the deck, the post's axis at the origin, from <see cref="KwDockLanternSpec.Rise"/> over the deck down to its foot.
    /// Wood (the rough tier): a crooked round pole with bark patches, a little thicker at its foot, bent twice on its way down,
    /// lashed to the deck's side with two loops of rope; core wood: a round log; fine wood: a planed square post. Its top is
    /// under the lantern head's iron cap. From the lantern's seed, so the same on every peer and in the previews. Plain
    /// geometry into <see cref="KwPierMesh"/>.
    /// </summary>
    public static class KwDockLanternShape
    {
        /// <summary>Gap between the post and the deck's side it stands against.</summary>
        public const float Gap = 0.03f;

        private const float Crook = 0.035f, Overlap = 0.02f;

        /// <summary>Half the post's width (a pole's or log's radius) for a material: all fit the lantern head's 0.306 m cap.</summary>
        public static float Half(KwPierKind kind) => kind.Round ? 0.15f : 0.13f;

        /// <summary>How far the post's axis stands out from the deck's edge.</summary>
        public static float Off(KwPierKind kind) => Half(kind) + Gap;

        /// <summary>The post's mesh, for its piece or a preview.</summary>
        public static Mesh Mesh(KwPierKind kind, float length, int seed, string name)
        {
            KwPierMesh mesh = new KwPierMesh();
            Build(mesh, kind, length, seed);
            return mesh.ToMesh(name);
        }

        public static void Build(KwPierMesh mesh, KwPierKind kind, float length, int seed)
        {
            KwPierUv uv = KwPierUv.For(kind, seed);
            Vector3 top = new Vector3(0f, KwDockLanternSpec.Rise, 0f), foot = new Vector3(0f, -length, 0f);
            float half = Half(kind);
            if (kind == KwPierKinds.Wood)
                Pole(mesh, uv, new System.Random(seed), top, foot, half);
            else if (kind.Round)
                mesh.Log(foot, top, half, uv, true, true);
            else
                mesh.Box(new Vector3(-half, -length, -half), new Vector3(half, KwDockLanternSpec.Rise, half), 1, uv, uv.Timber(length + KwDockLanternSpec.Rise));
        }

        // The rough pole in three lengths, bent across and along at its thirds, each overlapping the next a little; then
        // lashed to the deck's side under the deck's top.
        private static void Pole(KwPierMesh mesh, KwPierUv uv, System.Random random, Vector3 top, Vector3 foot, float half)
        {
            Vector3 m1 = Vector3.Lerp(top, foot, 0.4f) + Bend(random), m2 = Vector3.Lerp(top, foot, 0.7f) + Bend(random);
            Vector3[] points = { top, m1, m2, foot };
            for (int i = 0; i < 3; i++)
            {
                Vector3 a = points[i], b = points[i + 1], way = (b - a).normalized;
                float ra = half * (1f + 0.04f * i), rb = half * (1.04f + 0.04f * i);
                mesh.Round(i > 0 ? a - way * Overlap : a, i < 2 ? b + way * Overlap : b, ra, rb, uv.Pole, uv, i == 0, false);
            }
            foreach (float y in new[] { -0.06f, -0.14f })
                KwPierRope.Tube(mesh, Lashing(half, y), true, uv);
        }

        // A little off its line, never toward the deck (+z), where it stands against the side.
        private static Vector3 Bend(System.Random random) =>
            new Vector3(KwPierRough.Draw(random, -Crook, Crook), 0f, -KwPierRough.Draw(random, 0f, Crook));

        // A loop of rope round the pole's outer side and into the deck's side behind it.
        private static List<Vector3> Lashing(float half, float y)
        {
            float r = half + KwPierRope.Thick, into = half + Gap + 0.08f;
            List<Vector3> loop = new List<Vector3>();
            for (int k = 0; k <= 6; k++)
            {
                float angle = Mathf.PI * k / 6f;
                loop.Add(new Vector3(Mathf.Cos(angle) * r, y, -Mathf.Sin(angle) * r));
            }
            loop.Add(new Vector3(-0.07f, y, into));
            loop.Add(new Vector3(0.07f, y, into));
            return loop;
        }
    }
}
