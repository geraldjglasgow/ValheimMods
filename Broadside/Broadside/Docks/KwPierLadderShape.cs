using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A dock ladder's look (<see cref="KwPierLadder"/>), in its dock's material and atlas, in the piece's frame: x along the
    /// edge, y up from the deck's top, z out from the edge over the water. Two rails standing just off the deck's edge from
    /// the foot to a little over the deck, rungs between them every <see cref="Rung"/> from the foot up, and at the top a
    /// hook from each rail lying back over the deck's edge, which is what it hangs by. Wood (the rough tier): rails of
    /// crooked round poles with bark patches, rungs of slats nailed across their front, of odd lengths and a little askew,
    /// each rail lashed under its hook with rope; core wood: round logs, round rungs and log hooks; fine wood: square planed
    /// rails, round dowel rungs, square hooks. From the ladder's seed, so the same on every peer and in the preview. Plain
    /// geometry into <see cref="KwPierMesh"/>.
    /// </summary>
    public static class KwPierLadderShape
    {
        /// <summary>Half the ladder's width between its rails' middles, and how far out from the edge they stand.</summary>
        public const float Half = 0.24f, Out = 0.075f;

        public const float Rung = 0.3f;
        private const float Top = 0.06f, TopRung = -0.12f, Hook = 0.3f;

        /// <summary>The ladder's mesh, for its piece or the preview.</summary>
        public static Mesh Mesh(KwPierKind kind, float length, int seed, string name)
        {
            KwPierMesh mesh = new KwPierMesh();
            Build(mesh, kind, length, seed);
            return mesh.ToMesh(name);
        }

        public static void Build(KwPierMesh mesh, KwPierKind kind, float length, int seed)
        {
            KwPierUv uv = KwPierUv.For(kind, seed);
            System.Random random = new System.Random(seed);
            foreach (float side in new[] { -1f, 1f })
                Rail(mesh, kind, uv, random, side, length);
            for (float y = -length + Rung; y <= TopRung + 0.01f; y += Rung)
                RungAt(mesh, kind, uv, random, y);
            foreach (float side in new[] { -1f, 1f })
                HookAt(mesh, kind, uv, side);
        }

        private static void Rail(KwPierMesh mesh, KwPierKind kind, KwPierUv uv, System.Random random, float side, float length)
        {
            float lean = KwPierRough.Draw(random, -0.025f, 0.025f);
            Vector3 foot = new Vector3(side * Half + lean, -length, Out), top = new Vector3(side * Half, Top, Out);
            if (kind == KwPierKinds.Wood)
                mesh.Round(foot, top, 0.043f, 0.038f, uv.Pole, uv, true, true);
            else if (kind.Round)
                mesh.Log(foot - Vector3.right * lean, top, 0.05f, uv, true, true);
            else
                mesh.Box(new Vector3(side * Half - 0.03f, -length, Out - 0.035f), new Vector3(side * Half + 0.03f, Top, Out + 0.035f), 1, uv, uv.Timber(length + Top));
        }

        // One rung across at a height: a slat nailed across the rails' front (wood: its ends past them by odd amounts, a
        // little askew), a round rung through them (core wood), a dowel between them (fine wood).
        private static void RungAt(KwPierMesh mesh, KwPierKind kind, KwPierUv uv, System.Random random, float y)
        {
            float left = KwPierRough.Draw(random, 0.02f, 0.07f), right = KwPierRough.Draw(random, 0.02f, 0.07f), tilt = KwPierRough.Draw(random, -0.012f, 0.012f);
            if (kind == KwPierKinds.Wood)
                Slat(mesh, uv, new Vector2(-Half - left, Half + right), y, tilt);
            else if (kind.Round)
                mesh.Log(new Vector3(-Half - 0.04f, y, Out), new Vector3(Half + 0.04f, y, Out), 0.028f, uv, true, true);
            else
                mesh.Round(new Vector3(-Half, y, Out), new Vector3(Half, y, Out), 0.02f, 0.02f, uv.Timber, uv, true, true);
        }

        // A slat across the rails' front from x span.x to span.y, its middle at y, one end that much higher than the other.
        private static void Slat(KwPierMesh mesh, KwPierUv uv, Vector2 span, float y, float tilt)
        {
            const float tall = 0.045f, near = Out + 0.035f, far = Out + 0.062f;
            Vector3[] c = new Vector3[8];
            for (int k = 0; k < 4; k++)
            {
                bool end = k == 2 || k == 3, outer = k == 1 || k == 2;
                float x = end ? span.y : span.x, mid = y + (end ? tilt : -tilt) * 0.5f;
                (c[k], c[k + 4]) = (new Vector3(x, mid - tall * 0.5f, outer ? far : near), new Vector3(x, mid + tall * 0.5f, outer ? far : near));
            }
            KwPierBlock.Make(mesh, c, uv, uv.Board(span.y - span.x, tall));
        }

        // A rail's hook lying back over the deck's edge from its top (the rough wood's lashed to the rail with rope).
        private static void HookAt(KwPierMesh mesh, KwPierKind kind, KwPierUv uv, float side)
        {
            float x = side * Half;
            if (kind.Round)
            {
                mesh.Log(new Vector3(x, 0.03f, Out + 0.03f), new Vector3(x, 0.03f, -Hook - 0.02f), 0.03f, uv, true, true);
                return;
            }
            mesh.Box(new Vector3(x - 0.03f, 0.004f, -Hook), new Vector3(x + 0.03f, 0.044f, Out + 0.04f), 2, uv, uv.Timber(Hook + Out + 0.04f));
            if (kind != KwPierKinds.Wood)
                return;
            foreach (float y in new[] { -0.015f, -0.045f })
                KwPierRope.Tube(mesh, KwPierRope.Round(new Vector3(x, y, Out), 0.04f + KwPierRope.Thick, 0.008f), true, uv);
        }
    }
}
