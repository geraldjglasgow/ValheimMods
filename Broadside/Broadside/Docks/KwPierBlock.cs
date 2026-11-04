using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A crooked board or timber for the rough wood docks (<see cref="KwPierRough"/>): a box given by its eight corners, so it
    /// may lie a little askew, tilt, twist and end ragged; its top cupped (its middle line lower than its edges) when asked.
    /// Corners: the underside's four, then the top's four, each in the order (start, one side), (start, other side), (end,
    /// other side), (end, one side), with the grain from start to end. The top, underside and long sides take its strip
    /// along the grain (the top across its own width, the long sides across its thickness from the top's edge), the two
    /// ends the end grain; every face flat, its normal facing out of the block. Plain geometry into <see cref="KwPierMesh"/>.
    /// </summary>
    public static class KwPierBlock
    {
        private static readonly int[][] Faces =
        {
            new[] { 4, 5, 6, 7 }, new[] { 0, 3, 2, 1 }, new[] { 0, 4, 7, 3 }, new[] { 1, 2, 6, 5 }, new[] { 0, 1, 5, 4 }, new[] { 3, 7, 6, 2 },
        };

        /// <summary>The block into the mesh on its place in the strips (<see cref="KwPierUv"/>), its top cupped by so much.</summary>
        public static void Make(KwPierMesh mesh, Vector3[] c, KwPierUv uv, Vector3 place, float cup = 0f)
        {
            Vector3 centre = Vector3.zero;
            foreach (Vector3 p in c)
                centre += p / 8f;
            Vector3 grain = (c[7] - c[4]).normalized, across = (c[5] - c[4]).normalized;
            for (int f = 0; f < Faces.Length; f++)
            {
                Vector3[] q = { c[Faces[f][0]], c[Faces[f][1]], c[Faces[f][2]], c[Faces[f][3]] };
                if (f == 0 && cup > 0.0005f)
                    Cupped(mesh, q, centre, uv, place, cup);
                else
                    Face(mesh, q, centre, f < 4 ? Along(q, c[4], grain, across, f, uv, place) : Ends(q, uv));
            }
        }

        // The top as two faces either side of its middle line, that line lowered by the cup.
        private static void Cupped(KwPierMesh mesh, Vector3[] q, Vector3 centre, KwPierUv uv, Vector3 place, float cup)
        {
            Vector3 grain = (q[3] - q[0]).normalized, across = (q[1] - q[0]).normalized;
            Vector3 ms = (q[0] + q[1]) * 0.5f + Vector3.down * cup, me = (q[3] + q[2]) * 0.5f + Vector3.down * cup;
            Vector3[] near = { q[0], ms, me, q[3] }, far = { ms, q[1], q[2], me };
            Face(mesh, near, centre, Along(near, q[0], grain, across, 0, uv, place));
            Face(mesh, far, centre, Along(far, q[0], grain, across, 0, uv, place));
        }

        // A face in the grain: metres along it from the top's start corner, and across (the top and underside across the
        // width, a long side down its thickness from the top).
        private static Vector2[] Along(Vector3[] q, Vector3 origin, Vector3 grain, Vector3 across, int face, KwPierUv uv, Vector3 place)
        {
            Vector2[] t = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 d = q[i] - origin;
                float a = Mathf.Max(0f, Vector3.Dot(d, grain)), b = face < 2 ? Mathf.Abs(Vector3.Dot(d, across)) : Mathf.Max(0f, -d.y);
                t[i] = uv.Side(a, b, place);
            }
            return t;
        }

        // An end on the end grain, from its middle.
        private static Vector2[] Ends(Vector3[] q, KwPierUv uv)
        {
            Vector3 mid = (q[0] + q[1] + q[2] + q[3]) * 0.25f, side = (q[1] - q[0]).normalized;
            Vector2[] t = new Vector2[4];
            for (int i = 0; i < 4; i++)
                t[i] = uv.End(Vector3.Dot(q[i] - mid, side), q[i].y - mid.y);
            return t;
        }

        // A face whose normal looks away from the block's middle.
        private static void Face(KwPierMesh mesh, Vector3[] q, Vector3 centre, Vector2[] t)
        {
            Vector3 n = Vector3.Cross(q[1] - q[0], q[3] - q[0]);
            if (n.sqrMagnitude < 1e-12f)
                n = Vector3.Cross(q[2] - q[1], q[3] - q[1]);
            n = n.normalized;
            Vector3 mid = (q[0] + q[1] + q[2] + q[3]) * 0.25f;
            mesh.Face(q, t, Vector3.Dot(n, mid - centre) < 0f ? -n : n);
        }
    }
}
