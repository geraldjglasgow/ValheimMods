using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A cabin wall's look (<see cref="KwCabinWall"/>): its outer and inner faces, piece by piece (<see cref="KwCabinWall.Pieces"/>),
    /// planked like the hull in strake rows level from the keel line (outside on the planking texture's upper half, inside
    /// on its lower half, <see cref="KwUv.Strake"/>), each wall starting along the texture at a place of its own; and its
    /// rim, the outline's edges through the wall's thickness. Kit axes.
    /// </summary>
    public static class KwCabinWallMesh
    {
        public static void Draw(MeshBuilder b, KwCabinWall wall)
        {
            float shift = Shift(wall.Name), w = KwUv.StrakeWidth;
            foreach ((List<Vector2> piece, int row, int _) in wall.Pieces())
            {
                float foot = row * w;
                b.Polygon(piece.ConvertAll(p => wall.At(p.x, p.y, 0f)), piece.ConvertAll(p => KwUv.Strake(true, p.x + shift, row, p.y - foot)));
                b.Polygon(piece.ConvertAll(p => wall.At(p.x, p.y, KwCabinWall.Thick)), piece.ConvertAll(p => KwUv.Strake(false, p.x + shift, row, p.y - foot)));
            }
            Rim(b, wall);
        }

        // Where along the planking texture a wall starts, so the walls round a cabin show different boards.
        private static float Shift(string name) => name switch
        {
            "back" => 3f,
            "left" => 17f,
            "front" => 31f,
            _ => 45f,
        };

        // The outline's edges through the wall's thickness, each cut where the wall's line bends, the planking running along
        // the edge as a strake's does (planar UVs on the long planking texture came out squashed), as the transom's rim is.
        private static void Rim(MeshBuilder b, KwCabinWall wall)
        {
            List<Vector2> outline = wall.Outline;
            float along = 0f, thick = KwCabinWall.Thick;
            for (int i = 0; i < outline.Count; i++)
            {
                List<Vector2> cut = Cut(wall, outline[i], outline[(i + 1) % outline.Count]);
                for (int j = 0; j + 1 < cut.Count; j++)
                {
                    Vector2 p = cut[j], q = cut[j + 1];
                    List<Vector3> face = new List<Vector3> { wall.At(p.x, p.y, 0f), wall.At(q.x, q.y, 0f), wall.At(q.x, q.y, thick), wall.At(p.x, p.y, thick) };
                    float next = along + Vector2.Distance(p, q);
                    if ((face[1] - face[0]).sqrMagnitude > 1e-8f)
                        b.Polygon(face, new List<Vector2> { KwUv.Strake(true, along, 0, 0f), KwUv.Strake(true, next, 0, 0f), KwUv.Strake(true, next, 0, thick), KwUv.Strake(true, along, 0, thick) });
                    along = next;
                }
            }
        }

        // An edge from p to q with a point added where it crosses each bend of the wall's line.
        private static List<Vector2> Cut(KwCabinWall wall, Vector2 p, Vector2 q)
        {
            List<Vector2> cut = new List<Vector2> { p };
            float lo = Mathf.Min(p.x, q.x), hi = Mathf.Max(p.x, q.x);
            List<float> bends = new List<float>();
            foreach (float mark in wall.Marks)
            {
                if (mark > lo + 1e-4f && mark < hi - 1e-4f)
                    bends.Add(mark);
            }
            bends.Sort();
            if (q.x < p.x)
                bends.Reverse();
            foreach (float mark in bends)
                cut.Add(Vector2.LerpUnclamped(p, q, (mark - p.x) / (q.x - p.x)));
            cut.Add(q);
            return cut;
        }
    }
}
