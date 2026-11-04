using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a crate's sling bands lie on its side faces (<see cref="KwCrateSling"/>), measured once per crate prefab from
    /// the prefab itself (its lid shut): up each side, within the band's strip, the path a taut rope takes from the box's
    /// bottom edge to its top edge, pushed out over whatever the meshes have there past the box's face (a latch, the lid's
    /// lip, a rope handle, a lashing; at most <see cref="MostOut"/> past it), that is the outward hull of those points. A
    /// mesh that cannot be read counts as its box. Plain numbers in the crate's own frame, from the box's middle.
    /// </summary>
    public sealed class KwCrateSlingFit
    {
        /// <summary>The half width of a band's strip, and the most a side's path stands out past the box's face (m).</summary>
        private const float Strip = 0.06f, MostOut = 0.12f;
        private static readonly Dictionary<string, KwCrateSlingFit> made = new Dictionary<string, KwCrateSlingFit>();

        /// <summary>Each side's path as (height, distance out), both from the box's middle, bottom edge first: +x, -x, +z, -z.</summary>
        public readonly List<Vector2>[] Sides = { new List<Vector2>(), new List<Vector2>(), new List<Vector2>(), new List<Vector2>() };

        /// <summary>The fit of this crate's prefab, measured the first time it is asked for.</summary>
        public static KwCrateSlingFit Of(KwCrate crate)
        {
            string prefab = Utils.GetPrefabName(crate.gameObject);
            if (made.TryGetValue(prefab, out KwCrateSlingFit fit))
                return fit;
            GameObject? source = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
            return made[prefab] = new KwCrateSlingFit(source != null ? source.transform : crate.transform, crate.BoxCentre, crate.Half);
        }

        private KwCrateSlingFit(Transform root, Vector3 centre, Vector3 half)
        {
            for (int side = 0; side < Sides.Length; side++)
            {
                float face = side < 2 ? half.x : half.z;
                Sides[side].Add(new Vector2(-half.y, face));
                Sides[side].Add(new Vector2(half.y, face));
            }
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh? mesh = filter.sharedMesh;
                if (mesh != null && mesh.isReadable)
                    Gather(mesh, root.worldToLocalMatrix * filter.transform.localToWorldMatrix, centre, half);
            }
            foreach (List<Vector2> side in Sides)
                Taut(side);
        }

        // Every corner of a triangle that reaches into a band's strip, standing out past the box's face on that band's sides.
        private void Gather(Mesh mesh, Matrix4x4 toCrate, Vector3 centre, Vector3 half)
        {
            Vector3[] points = mesh.vertices;
            for (int i = 0; i < points.Length; i++)
                points[i] = toCrate.MultiplyPoint3x4(points[i]) - centre;
            int[] corners = mesh.triangles;
            for (int i = 0; i + 2 < corners.Length; i += 3)
            {
                Vector3 a = points[corners[i]], b = points[corners[i + 1]], c = points[corners[i + 2]];
                for (int axis = 0; axis <= 2; axis += 2)
                {
                    if (!InStrip(2 - axis, a, b, c))
                        continue;
                    Point(axis, a, half);
                    Point(axis, b, half);
                    Point(axis, c, half);
                }
            }
        }

        // Whether a triangle reaches the strip about the middle across this axis.
        private static bool InStrip(int across, Vector3 a, Vector3 b, Vector3 c) =>
            Mathf.Min(a[across], Mathf.Min(b[across], c[across])) <= Strip && Mathf.Max(a[across], Mathf.Max(b[across], c[across])) >= -Strip;

        // A corner past the +face or the -face along this axis (0: x, 2: z) is a point of that side's path.
        private void Point(int axis, Vector3 p, Vector3 half)
        {
            int side = axis == 0 ? 0 : 2;
            float face = half[axis], height = Mathf.Clamp(p.y, -half.y, half.y);
            if (p[axis] > face)
                Sides[side].Add(new Vector2(height, Mathf.Min(p[axis], face + MostOut)));
            else if (-p[axis] > face)
                Sides[side + 1].Add(new Vector2(height, Mathf.Min(-p[axis], face + MostOut)));
        }

        // The outward hull from the bottom edge to the top (the upper chain of a monotone hull, heights rising): a taut
        // rope's path over the points. In place.
        private static void Taut(List<Vector2> points)
        {
            points.Sort((p, q) => p.x != q.x ? p.x.CompareTo(q.x) : p.y.CompareTo(q.y));
            int kept = 0;
            for (int i = 0; i < points.Count; i++)
            {
                while (kept >= 2 && Cross(points[kept - 2], points[kept - 1], points[i]) >= 0f)
                    kept--;
                points[kept++] = points[i];
            }
            points.RemoveRange(kept, points.Count - kept);
        }

        private static float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
    }
}
