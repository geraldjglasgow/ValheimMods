using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// A launched ship's rudder blade drawn with its chunks gone (the user, 2026-10-26: "If a lucky shot hits the rudder,
    /// the rudder should be able to be damaged"), on the screens that draw it: the blade's own mesh cut fore of where each
    /// chunk broke its boards off (<see cref="KwRigBlade.BrokenAt"/>, ragged board by board, <see cref="KwRigBladeCut"/>),
    /// the broken boards' ends closed. The blade and its box still turn with the helm; the steering it gives is
    /// <see cref="KwRigDamageDrive.Steer"/>'s. Mended, it wears its own mesh again, growing back from what was left of it (<see cref="KwRigRise"/>).
    /// </summary>
    public sealed class KwRigDamageRudder : MonoBehaviour
    {
        private KwRigPiece piece = null!;
        private MeshFilter filter = null!;
        private Mesh whole = null!;
        private string shown = "";
        private float left = 1f;

        public static KwRigDamageRudder? Add(Transform? blade, KwRigPiece piece)
        {
            MeshFilter? filter = blade != null ? blade.GetComponent<MeshFilter>() : null;
            if (filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable)
                return null;
            KwRigDamageRudder made = blade!.gameObject.AddComponent<KwRigDamageRudder>();
            (made.piece, made.filter, made.whole) = (piece, filter, filter.sharedMesh);
            return made;
        }

        public void Show(KwRigCondition condition)
        {
            string key = condition.Key(piece.Index, "r");
            if (key == shown)
                return;
            shown = key;
            List<KwRigMark> chunks = condition.Chunks(piece.Index);
            Mesh? old = filter.sharedMesh != whole ? filter.sharedMesh : null;
            filter.sharedMesh = chunks.Count == 0 ? whole : Cut(chunks);
            if (old != null)
                Destroy(old);
            // mended with the hammer: the blade grows back from what was left of it (KwRigRise)
            (float was, left) = (left, KwRigBlade.Left(piece, chunks));
            if (chunks.Count == 0 && was < 0.99f)
                KwRigRise.Grow(transform, was);
        }

        // The blade's mesh cut by its chunks, submesh by submesh, the broken boards' ends added to the first (the wood).
        private Mesh Cut(List<KwRigMark> chunks)
        {
            KwRigBladeCut cut = new KwRigBladeCut(whole.vertices, whole.normals, whole.uv);
            List<List<int>> subs = new List<List<int>>();
            List<float> heights = KwRigBlade.Heights(piece, chunks);
            for (int m = 0; m < whole.subMeshCount; m++)
                subs.Add(cut.Clip(whole.GetTriangles(m), z => KwRigBlade.BrokenAt(piece, chunks, z), heights, KwRigBlade.Forward(piece, chunks)));
            cut.Ends(subs[0], piece, chunks);
            Mesh made = new Mesh { name = whole.name + "_hurt", indexFormat = IndexFormat.UInt32, subMeshCount = subs.Count };
            made.SetVertices(cut.Points);
            made.SetNormals(cut.Normals);
            made.SetUVs(0, cut.Uvs);
            for (int m = 0; m < subs.Count; m++)
                made.SetTriangles(subs[m], m);
            made.RecalculateTangents();
            made.RecalculateBounds();
            return made;
        }

        // The blade's own mesh let go once a cut one is worn (its mesh's owner lets go what it wears when the blade goes).
        private void OnDestroy()
        {
            if (filter != null && filter.sharedMesh != whole && whole != null)
                Destroy(whole);
        }
    }

    /// <summary>
    /// A rudder blade's triangles cut where it is broken (<see cref="KwRigDamageRudder"/>), plain arithmetic on its points in
    /// the blade's Unity axes (kit x, forward, is Unity z; kit z, up, is Unity y): each triangle sliced at the heights where
    /// the breaks change (<see cref="KwRigBlade.Heights"/>), so a tall board is cut only over the chunk's height, each slice
    /// clipped to the part fore of where the blade is broken off there, new points made on the cuts with their normals and
    /// UVs between; and each broken board's end closed by a face across the blade's thickness.
    /// </summary>
    public sealed class KwRigBladeCut
    {
        private const float Board = 0.12f;

        public KwRigBladeCut(Vector3[] points, Vector3[] normals, Vector2[] uvs)
        {
            Points = new List<Vector3>(points);
            Normals = normals.Length == points.Length ? new List<Vector3>(normals) : new List<Vector3>(new Vector3[points.Length]);
            Uvs = uvs.Length == points.Length ? new List<Vector2>(uvs) : new List<Vector2>(new Vector2[points.Length]);
        }

        public List<Vector3> Points { get; }
        public List<Vector3> Normals { get; }
        public List<Vector2> Uvs { get; }

        /// <summary>
        /// The triangles kept: one wholly fore of the most forward break as it is; else cut into slabs between the heights
        /// given (where the blade's breaks change), each slab clipped to the part fore of where the blade is broken off at
        /// its middle's height (kit x; null where it is whole).
        /// </summary>
        public List<int> Clip(int[] triangles, System.Func<float, float?> brokenAt, List<float> heights, float forward)
        {
            List<int> kept = new List<int>(triangles.Length);
            for (int k = 0; k + 2 < triangles.Length; k += 3)
            {
                List<int> triangle = new List<int> { triangles[k], triangles[k + 1], triangles[k + 2] };
                if (triangle.TrueForAll(i => Points[i].z >= forward))
                    kept.AddRange(triangle);
                else
                    Slabs(triangle, heights).ForEach(slab => Keep(kept, slab, brokenAt));
            }
            return kept;
        }

        // A polygon cut into slabs by the heights inside its own height range.
        private List<List<int>> Slabs(List<int> polygon, List<float> heights)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (int i in polygon)
                (lo, hi) = (Mathf.Min(lo, Points[i].y), Mathf.Max(hi, Points[i].y));
            List<List<int>> slabs = new List<List<int>>();
            List<int> rest = polygon;
            foreach (float h in heights)
            {
                if (h <= lo + 1e-5f || h >= hi - 1e-5f)
                    continue;
                slabs.Add(Cut(rest, p => h - p.y));
                rest = Cut(rest, p => p.y - h);
            }
            slabs.Add(rest);
            return slabs.FindAll(slab => slab.Count >= 3);
        }

        // A slab kept fore of where the blade is broken off at its middle's height, or whole there.
        private void Keep(List<int> kept, List<int> slab, System.Func<float, float?> brokenAt)
        {
            float mid = 0f;
            foreach (int i in slab)
                mid += Points[i].y / slab.Count;
            float? x = brokenAt(mid);
            List<int> fore = x.HasValue ? Cut(slab, p => p.z - x.Value) : slab;
            for (int i = 1; i + 1 < fore.Count; i++)
                kept.AddRange(new[] { fore[0], fore[i], fore[i + 1] });
        }

        // A polygon's part where a distance is not below zero (Sutherland and Hodgman), new points made on the cut.
        private List<int> Cut(List<int> polygon, System.Func<Vector3, float> distance)
        {
            List<int> left = new List<int>();
            for (int i = 0; i < polygon.Count; i++)
            {
                int p = polygon[i], q = polygon[(i + 1) % polygon.Count];
                float dp = distance(Points[p]), dq = distance(Points[q]);
                if (dp >= 0f)
                    left.Add(p);
                if ((dp >= 0f) != (dq >= 0f))
                    left.Add(Between(p, q, dp / (dp - dq)));
            }
            return left;
        }

        private int Between(int p, int q, float t)
        {
            Points.Add(Vector3.Lerp(Points[p], Points[q], t));
            Normals.Add(Vector3.Lerp(Normals[p], Normals[q], t).normalized);
            Uvs.Add(Vector2.Lerp(Uvs[p], Uvs[q], t));
            return Points.Count - 1;
        }

        /// <summary>Each broken board's end closed: a face across the blade's thickness where it broke, both ways.</summary>
        public void Ends(List<int> wood, KwRigPiece rudder, List<KwRigMark> chunks)
        {
            KwRudderSpan span = rudder.Tier!.Span(rudder.Fit);
            float half = rudder.Tier.BladeHalf * span.Thick;
            for (float z0 = Mathf.Floor(span.Foot / Board) * Board; z0 < span.Tip; z0 += Board)
            {
                float lo = Mathf.Max(z0, span.Foot), hi = Mathf.Min(z0 + Board, span.Tip);
                float? x = KwRigBlade.BrokenAt(rudder, chunks, (lo + hi) / 2f);
                if (x.HasValue && hi > lo && span.AftAt((lo + hi) / 2f) > -x.Value)
                    End(wood, x.Value, lo, hi, half);
            }
        }

        // One board's end: a quad at the break (kit x) from lo to hi, across the thickness, seen from both sides.
        private void End(List<int> wood, float x, float lo, float hi, float half)
        {
            int start = Points.Count;
            foreach (Vector3 corner in new[] { new Vector3(-half, lo, x), new Vector3(half, lo, x), new Vector3(half, hi, x), new Vector3(-half, hi, x) })
            {
                Points.Add(corner);
                Normals.Add(Vector3.back);
                Uvs.Add(new Vector2(corner.x * 4f, corner.y));
            }
            wood.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
            wood.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }
    }
}
