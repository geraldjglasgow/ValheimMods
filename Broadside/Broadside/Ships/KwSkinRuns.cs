using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Stretches of a hull's face that run on straight over several station gaps (a long part's even middle, a side that
    /// changes little from station to station): one box over the whole run (<see cref="KwHullSkin"/>), laid along the
    /// strakes, where its face is near a rectangle of the run and every station's points on the stretch lie within reach of
    /// its plane and inside its face, so a straight side is a few boxes rather than one a gap. Pure.
    /// </summary>
    public static class KwSkinRuns
    {
        /// <summary>How many gaps from row i on stretch j of a sheet one box covers (its box), or 1 (no box) where no run starts.</summary>
        public static int Of(KwSkinSheet sheet, int i, int j, float stand, float reach, out KwSkinBox box)
        {
            box = default;
            int n = 1;
            while (i + n < sheet.Last && Fits(sheet, i, j, n + 1, stand, reach, out KwSkinBox longer))
                (box, n) = (longer, n + 1);
            return n;
        }

        // One box over gaps i to i + n - 1 of stretch j: along the strakes, near a rectangle of the run, each station's two
        // points on it within reach of its plane and inside its face.
        private static bool Fits(KwSkinSheet sheet, int i, int j, int n, float stand, float reach, out KwSkinBox box)
        {
            Vector3 a0 = sheet.Point(i, j, 0f, 0f), a1 = sheet.Point(i, j, 0f, 1f), c0 = sheet.Point(i + n - 1, j, 1f, 0f), c1 = sheet.Point(i + n - 1, j, 1f, 1f);
            if (!KwSkinFit.Of(a0, a1, c0, c1, sheet.Out(i, j), stand, KwSkinFit.Along, out box, out float off) || off > reach
                || KwSkinFit.Over(box, a0, a1, c0, c1) > reach)
                return false;
            for (int k = 1; k < n; k++)
            {
                if (!On(box, stand, sheet.Point(i + k, j, 0f, 0f), reach) || !On(box, stand, sheet.Point(i + k, j, 0f, 1f), reach))
                    return false;
            }
            return true;
        }

        // Whether a point lies within reach of a box's face plane and inside its face.
        private static bool On(KwSkinBox box, float stand, Vector3 p, float reach)
        {
            Vector3 off = p - KwSkinFit.Plane(box, stand);
            Rect face = box.Face;
            return Mathf.Abs(Vector3.Dot(off, box.Out)) <= reach && face.Contains(new Vector2(Vector3.Dot(off, box.Along), Vector3.Dot(off, box.Up)));
        }
    }
}
