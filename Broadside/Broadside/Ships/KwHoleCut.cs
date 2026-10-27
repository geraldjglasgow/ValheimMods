using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Holes cut through a hull part's mesh (<see cref="KwHoleLooks"/>): every triangle lying within <see cref="Outside"/>
    /// out of a hole's face and <see cref="Inside"/> in through it, where the hole's outline reaches it seen along the
    /// hole's normal, loses what lies inside the outline within those depths (<see cref="KwHoleClip"/>), so both faces of
    /// a board, its edges and a painted band over it are cut straight through; the rest of the mesh keeps its vertices and
    /// triangles as they were (only the triangles near a hole are looked at, <see cref="KwHoleIndex"/>). Each hole's cut
    /// faces then get their rim and splinters (<see cref="KwHoleRim"/>). The holes are given in the mesh's own frame. Pure.
    /// </summary>
    public static class KwHoleCut
    {
        /// <summary>How far out of a part's face a hole cuts (past planking's laps and painted bands) and in through it (its boards).</summary>
        public const float Outside = 0.08f, Inside = 0.15f;

        /// <summary>The mesh with the holes cut through it, rims and splinters added (but when rims is false: the cut alone).</summary>
        public static KwHoleMesh Cut(KwHoleMesh mesh, List<KwHoleShape> holes, bool rims = true, KwHoleIndex? index = null) =>
            Join(mesh, new List<KwHolePatch> { Patch(mesh, holes, rims, index) });

        /// <summary>
        /// What some holes do to a mesh, kept apart from it (<see cref="Join"/>): the triangles they take out and the pieces,
        /// rims and splinters they add, so a part's other holes need not be cut again when one changes.
        /// </summary>
        public static KwHolePatch Patch(KwHoleMesh mesh, List<KwHoleShape> holes, bool rims = true, KwHoleIndex? index = null)
        {
            KwHolePatch patch = new KwHolePatch(mesh.Subs.Count);
            List<List<(int Sub, int At)>> touched = holes.ConvertAll(_ => new List<(int Sub, int At)>());
            for (int s = 0; s < mesh.Subs.Count; s++)
            {
                foreach (int t in index != null ? index.Near(s, holes) : KwHoleIndex.All(mesh, s))
                {
                    if (Triangle(mesh, patch.Added, s, t, holes, touched))
                        patch.Gone[s].Add(t);
                }
            }
            for (int h = 0; h < holes.Count && rims; h++)
                KwHoleRim.Add(mesh, patch.Added, holes[h], touched[h]);
            return patch;
        }

        /// <summary>A mesh with patches on it: its own triangles less those the patches take out, the patches' after them.</summary>
        public static KwHoleMesh Join(KwHoleMesh mesh, List<KwHolePatch> patches)
        {
            KwHoleMesh joined = mesh.Vertices();
            for (int s = 0; s < mesh.Subs.Count; s++)
            {
                joined.Subs[s].AddRange(mesh.Subs[s]);
                List<int> gone = new List<int>();
                patches.ForEach(p => gone.AddRange(p.Gone[s]));
                Drop(joined.Subs[s], gone);
            }
            foreach (KwHolePatch patch in patches)
                joined.Append(patch.Added);
            return joined;
        }

        // One triangle: as it is when no hole reaches it (false), else what the holes leave of it as new vertices and
        // triangles (true: the triangle itself goes).
        private static bool Triangle(KwHoleMesh mesh, KwHoleMesh added, int s, int t, List<KwHoleShape> holes, List<List<(int Sub, int At)>> touched)
        {
            List<int> tris = mesh.Subs[s];
            List<List<KwHoleVertex>>? pieces = null;
            List<KwHoleVertex>? whole = null;
            for (int h = 0; h < holes.Count; h++)
            {
                if (!Reaches(holes[h], mesh.Points[tris[t]], mesh.Points[tris[t + 1]], mesh.Points[tris[t + 2]]))
                    continue;
                touched[h].Add((s, t));
                pieces ??= new List<List<KwHoleVertex>> { (whole = new List<KwHoleVertex> { mesh.Vertex(tris[t]), mesh.Vertex(tris[t + 1]), mesh.Vertex(tris[t + 2]) }) };
                pieces = KwHoleClip.Minus(pieces, holes[h]);
            }
            if (pieces == null || (pieces.Count == 1 && ReferenceEquals(pieces[0], whole)))
                return false;
            foreach (List<KwHoleVertex> piece in pieces)
                Fan(added, added.Subs[s], piece);
            return true;
        }

        // Triangles taken out of a list by their starts (each once), each filled by the list's last (their order is free).
        private static void Drop(List<int> tris, List<int> gone)
        {
            gone.Sort();
            for (int i = gone.Count - 1; i >= 0; i--)
            {
                if (i + 1 < gone.Count && gone[i] == gone[i + 1])
                    continue;
                int last = tris.Count - 3;
                for (int k = 0; k < 3 && gone[i] != last; k++)
                    tris[gone[i] + k] = tris[last + k];
                tris.RemoveRange(last, 3);
            }
        }

        /// <summary>Whether a triangle lies within a hole's depths and its outline's reach, seen along its normal.</summary>
        public static bool Reaches(KwHoleShape hole, Vector3 a, Vector3 b, Vector3 c)
        {
            float da = hole.Depth(a), db = hole.Depth(b), dc = hole.Depth(c);
            if (Mathf.Min(da, Mathf.Min(db, dc)) > Outside || Mathf.Max(da, Mathf.Max(db, dc)) < -Inside)
                return false;
            Vector2 fa = hole.Flat(a), fb = hole.Flat(b), fc = hole.Flat(c);
            float r = hole.Reach;
            return Mathf.Max(fa.x, Mathf.Max(fb.x, fc.x)) > -r && Mathf.Min(fa.x, Mathf.Min(fb.x, fc.x)) < r
                && Mathf.Max(fa.y, Mathf.Max(fb.y, fc.y)) > -r && Mathf.Min(fa.y, Mathf.Min(fb.y, fc.y)) < r;
        }

        // A convex piece as a fan of triangles from its first corner, turned as the triangle it came from.
        private static void Fan(KwHoleMesh cut, List<int> into, List<KwHoleVertex> piece)
        {
            int first = cut.Count;
            foreach (KwHoleVertex v in piece)
                cut.Add(v);
            for (int i = 1; i + 1 < piece.Count; i++)
            {
                into.Add(first);
                into.Add(first + i);
                into.Add(first + i + 1);
            }
        }
    }

    /// <summary>Some holes' cut of a mesh (<see cref="KwHoleCut.Patch"/>): each submesh's triangles taken out, and what is added (indexed from 0).</summary>
    public sealed class KwHolePatch
    {
        public KwHolePatch(int subs)
        {
            for (int s = 0; s < subs; s++)
            {
                Gone.Add(new List<int>());
                Added.Subs.Add(new List<int>());
            }
        }

        public List<List<int>> Gone { get; } = new List<List<int>>();
        public KwHoleMesh Added { get; } = new KwHoleMesh();

        /// <summary>Whether it changes nothing (its holes reach no triangle).</summary>
        public bool Empty => Added.Count == 0 && Gone.TrueForAll(g => g.Count == 0);
    }

    /// <summary>
    /// A mesh's triangles as bounding spheres (each its corners' middle and how far its furthest corner lies), in plain
    /// arrays made once a mesh (at a part's first hole, <see cref="KwHolePart"/>), so a hole looks closely only at the
    /// triangles round it. Pure.
    /// </summary>
    public sealed class KwHoleIndex
    {
        private readonly List<float[]> spheres = new List<float[]>();

        public KwHoleIndex(KwHoleMesh mesh)
        {
            for (int s = 0; s < mesh.Subs.Count; s++)
            {
                List<int> tris = mesh.Subs[s];
                float[] four = new float[tris.Count / 3 * 4];
                for (int t = 0, k = 0; t + 2 < tris.Count; t += 3, k += 4)
                {
                    Vector3 a = mesh.Points[tris[t]], b = mesh.Points[tris[t + 1]], c = mesh.Points[tris[t + 2]], m = (a + b + c) / 3f;
                    (four[k], four[k + 1], four[k + 2]) = (m.x, m.y, m.z);
                    four[k + 3] = Mathf.Sqrt(Mathf.Max((a - m).sqrMagnitude, Mathf.Max((b - m).sqrMagnitude, (c - m).sqrMagnitude)));
                }
                spheres.Add(four);
            }
        }

        /// <summary>Every triangle's start of a submesh (no index).</summary>
        public static IEnumerable<int> All(KwHoleMesh mesh, int sub)
        {
            for (int t = 0; t + 2 < mesh.Subs[sub].Count; t += 3)
                yield return t;
        }

        /// <summary>The starts of a submesh's triangles whose spheres come within any hole's reach and depths.</summary>
        public List<int> Near(int sub, List<KwHoleShape> holes)
        {
            List<int> found = new List<int>();
            float[] four = spheres[sub];
            for (int k = 0; k + 3 < four.Length; k += 4)
            {
                foreach (KwHoleShape hole in holes)
                {
                    float dx = four[k] - hole.Centre.x, dy = four[k + 1] - hole.Centre.y, dz = four[k + 2] - hole.Centre.z, reach = four[k + 3] + hole.Reach + KwHoleCut.Inside;
                    if (dx * dx + dy * dy + dz * dz > reach * reach)
                        continue;
                    found.Add(k / 4 * 3);
                    break;
                }
            }
            return found;
        }
    }
}
