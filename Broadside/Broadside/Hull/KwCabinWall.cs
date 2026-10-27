using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One wall of a cabin, everything about it from its outline: the line its outer face stands on across the plan (kit
    /// x, y; the cabin's inside to its right seen from above, so its inner face is <see cref="Thick"/> to that side), and its
    /// face's outline, a polygon in (u, z): u along that line from its start, z up from the keel line. The look
    /// (<see cref="KwCabinWallMesh"/>) and a launched ship's boxes (<see cref="Boxes"/>) are both cut from <see cref="Pieces"/>:
    /// the outline in strake rows (level from the keel line, <see cref="KwUv.StrakeWidth"/>) and the line's straight
    /// segments, so the openings cut in it (<see cref="Cut"/>: doorways, windows) are left out of both. Pure numbers.
    /// </summary>
    public sealed class KwCabinWall
    {
        /// <summary>A wall's thickness, a cabin's own walls' and bulkheads'.</summary>
        public const float Thick = 0.06f;

        private readonly List<Vector2> path;
        private readonly List<float> marks = new List<float>();
        private readonly List<Vector2> inward = new List<Vector2>();
        private List<List<Vector2>>? holes;

        public KwCabinWall(string name, List<Vector2> path, List<Vector2> outline)
        {
            Name = name;
            this.path = path;
            Outline = outline;
            Measure();
        }

        /// <summary>"back", "front", "left" or "right": which wall of the cabin it is.</summary>
        public string Name { get; }

        /// <summary>The face's outline in (u along the wall, z up).</summary>
        public List<Vector2> Outline { get; }

        public IReadOnlyList<Vector2> Path => path;

        /// <summary>How long the wall's line is.</summary>
        public float Length => marks[marks.Count - 1];

        /// <summary>u at each point of the line, from 0 at its start.</summary>
        public IReadOnlyList<float> Marks => marks;

        // u at each point; each point's inward way, mitred between the segments either side.
        private void Measure()
        {
            marks.Add(0f);
            for (int i = 1; i < path.Count; i++)
                marks.Add(marks[i - 1] + Vector2.Distance(path[i - 1], path[i]));
            for (int i = 0; i < path.Count; i++)
            {
                Vector2 before = Right(Mathf.Max(0, i - 1)), after = Right(Mathf.Min(path.Count - 2, i));
                Vector2 mid = (before + after).normalized;
                float cos = Mathf.Max(0.5f, Vector2.Dot(mid, after));
                inward.Add(mid / cos);
            }
        }

        // Segment i's right (the cabin's inside), seen from above: its way turned a quarter clockwise.
        private Vector2 Right(int i)
        {
            Vector2 d = (path[i + 1] - path[i]).normalized;
            return new Vector2(d.y, -d.x);
        }

        /// <summary>The point of the wall at u along it, z up, depth in from its outer face (kit axes).</summary>
        public Vector3 At(float u, float z, float depth)
        {
            int i = Segment(u);
            float span = marks[i + 1] - marks[i], t = span > 1e-5f ? Mathf.Clamp01((u - marks[i]) / span) : 0f;
            Vector2 on = Vector2.LerpUnclamped(path[i], path[i + 1], t) + Vector2.LerpUnclamped(inward[i], inward[i + 1], t) * depth;
            return new Vector3(on.x, on.y, z);
        }

        /// <summary>The segment of the line u falls in.</summary>
        public int Segment(float u)
        {
            for (int i = 0; i + 2 < marks.Count; i++)
            {
                if (u <= marks[i + 1])
                    return i;
            }
            return marks.Count - 2;
        }

        /// <summary>Segment i's way along, and inward, in the plan (kit x, y).</summary>
        public (Vector2 Along, Vector2 In) Way(int i) => ((path[i + 1] - path[i]).normalized, Right(i));

        /// <summary>
        /// The face in convex pieces (u, z), each within one strake row (its number from the keel line) and one straight
        /// segment of the line.
        /// </summary>
        public List<(List<Vector2> Piece, int Row, int Segment)> Pieces()
        {
            List<(List<Vector2>, int, int)> pieces = new List<(List<Vector2>, int, int)>();
            (float lo, float hi) = Heights(Outline);
            float w = KwUv.StrakeWidth;
            for (int k = Mathf.FloorToInt(lo / w + 1e-3f); k * w < hi - 1e-3f; k++)
            {
                List<Vector2> row = KwClip.Between(Outline, p => p.y, Vector2.LerpUnclamped, k * w, (k + 1) * w);
                for (int s = 0; s + 1 < marks.Count && row.Count >= 3; s++)
                {
                    List<Vector2> piece = KwClip.Between(row, p => p.x, Vector2.LerpUnclamped, marks[s], marks[s + 1]);
                    if (piece.Count < 3)
                        continue;
                    foreach (List<Vector2> left in KwOpeningCut.Apart(piece, p => p, Vector2.LerpUnclamped, holes))
                        pieces.Add((left, k, s));
                }
            }
            return pieces;
        }

        /// <summary>The openings cut in it (<see cref="KwOpening"/>): those of a cabin's openings on this wall that fit it, left out of its pieces.</summary>
        public List<KwOpening> Openings { get; private set; } = new List<KwOpening>();

        /// <summary>The wall with a cabin's openings on it cut out (those on it by name that fit it).</summary>
        public KwCabinWall Cut(List<KwOpening> openings)
        {
            KwWallSurface surface = new KwWallSurface(this);
            Openings = openings.FindAll(o => o.Wall == Name && KwOpeningSurfaces.Fits(surface, o));
            holes = Openings.Count > 0 ? Openings.ConvertAll(o => o.Outline()) : null;
            return this;
        }

        /// <summary>The lowest and highest z of an outline.</summary>
        public static (float Lo, float Hi) Heights(List<Vector2> outline)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (Vector2 p in outline)
                (lo, hi) = (Mathf.Min(lo, p.y), Mathf.Max(hi, p.y));
            return (lo, hi);
        }

        /// <summary>
        /// What a launched ship bumps into: boxes, each a straight segment's stretch of the wall over a run of rows that
        /// share one reach along it (the narrower of each row's foot and head, so no box stands out past a sloping edge),
        /// as (u from, u to, z from, z to, segment).
        /// </summary>
        public List<(float U0, float U1, float Z0, float Z1, int Segment)> Boxes()
        {
            List<(float, float, float, float, int)> boxes = new List<(float, float, float, float, int)>();
            foreach ((List<Vector2> piece, int _, int segment) in Pieces().OrderBy(p => p.Segment).ThenBy(p => p.Row))
            {
                (float z0, float z1) = Heights(piece);
                (float u0, float u1) = Narrowest(piece, z0, z1);
                if (u1 - u0 < 0.05f || z1 - z0 < 0.02f)
                    continue;
                int last = boxes.Count - 1;
                if (last >= 0 && boxes[last].Item5 == segment && Mathf.Abs(boxes[last].Item1 - u0) < 0.01f
                    && Mathf.Abs(boxes[last].Item2 - u1) < 0.01f && Mathf.Abs(boxes[last].Item4 - z0) < 0.01f)
                    boxes[last] = (u0, u1, boxes[last].Item3, z1, segment);
                else
                    boxes.Add((u0, u1, z0, z1, segment));
            }
            return boxes;
        }

        // A piece's reach along at its foot and at its head, the narrower of the two.
        private static (float U0, float U1) Narrowest(List<Vector2> piece, float z0, float z1)
        {
            float lo0 = float.MaxValue, hi0 = float.MinValue, lo1 = float.MaxValue, hi1 = float.MinValue;
            foreach (Vector2 p in piece)
            {
                if (Mathf.Abs(p.y - z0) < 1e-3f)
                    (lo0, hi0) = (Mathf.Min(lo0, p.x), Mathf.Max(hi0, p.x));
                if (Mathf.Abs(p.y - z1) < 1e-3f)
                    (lo1, hi1) = (Mathf.Min(lo1, p.x), Mathf.Max(hi1, p.x));
            }
            return (Mathf.Max(lo0, lo1), Mathf.Min(hi0, hi1));
        }
    }
}
