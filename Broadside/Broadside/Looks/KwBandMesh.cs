using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A planking part's painted bands as thin skins over its strakes (<see cref="KwStrakes"/>, cut the same way from the
    /// same measure, <see cref="KwGirth"/>): each painted strake's outside face again, <see cref="Proud"/> further out, both
    /// sides of the hull, its texture laid exactly as the strake's (<see cref="KwUv.Strake"/>) so the paint's grain sits on
    /// the plank's own. Only the faces are painted: a strake's lip, the gunwale's cap, the part's ends and a flat stern's
    /// transom board stay bare wood. A band's strakes are counted as <see cref="KwGirth"/> counts them, up from the keel
    /// line's height, so the same band is the same level strakes on every part. One mesh per colour; a strake the part does
    /// not reach draws nothing. Kit axes, around the part's pivot, as the planking's own mesh, each face carrying its strake
    /// and side as the planking's do, for <see cref="KwPlankShift"/>.
    /// </summary>
    public static class KwBandMesh
    {
        /// <summary>How far a band stands out of its strake's face, in metres.</summary>
        public const float Proud = 0.006f;
        private const float W = KwUv.StrakeWidth, OnEdge = 1e-4f;

        /// <summary>A planking part's band meshes from its saved paint segment, one per colour used; none unpainted.</summary>
        public static List<(int Colour, Mesh Mesh)> Meshes(KwDef def, string paint)
        {
            List<(int Colour, Mesh Mesh)> made = new List<(int Colour, Mesh Mesh)>();
            List<KwBand> bands = KwPaint.Read(paint);
            if (bands.Count == 0 || def.Stations.Count < 2)
                return made;
            KwGirth girth = KwGirth.Of(def);
            for (int colour = 0; colour < KwPaint.Names.Length; colour++)
            {
                List<KwBand> mine = bands.FindAll(b => b.Colour == colour);
                if (mine.Count == 0)
                    continue;
                Mesh mesh = Builder(def, girth, mine).ToMesh(def.Name + "_paint");
                if (mesh.vertexCount > 0)
                    made.Add((colour, mesh));
                else
                    Object.Destroy(mesh);
            }
            return made;
        }

        /// <summary>Some bands' faces, before they become a mesh.</summary>
        public static MeshBuilder Builder(KwDef def, KwGirth girth, List<KwBand> bands)
        {
            MeshBuilder b = new MeshBuilder(new Vector3(def.Pivot.x, 0f, def.Pivot.y)) { KeepsRows = true };
            foreach (KwBand band in bands)
            {
                for (int k = Mathf.Max(band.From, girth.Lowest); k <= Mathf.Min(band.To, girth.Highest); k++)
                {
                    foreach (float side in new[] { 1f, -1f })
                    {
                        for (int s = 0; s + 1 < def.Stations.Count; s++)
                            Gap(b, girth, s, side, k);
                    }
                }
            }
            return b;
        }

        // Strake k's outside face between station s and the next, on one side: each quad of section points it crosses, cut
        // to the strake's edges as the planking cuts it.
        private static void Gap(MeshBuilder b, KwGirth girth, int s, float side, int k)
        {
            float[] ga = girth.Of(s), gc = girth.Of(s + 1);
            float lo = k * W, hi = (k + 1) * W;
            for (int j = 0; j + 1 < ga.Length; j++)
            {
                if (Mathf.Max(ga[j + 1], gc[j + 1]) < lo + OnEdge || Mathf.Min(ga[j], gc[j]) >= hi)
                    continue;
                List<KwGirthPoint> quad = new List<KwGirthPoint>
                {
                    KwStrakes.Corner(girth, s, j, side, k), KwStrakes.Corner(girth, s, j + 1, side, k),
                    KwStrakes.Corner(girth, s + 1, j + 1, side, k), KwStrakes.Corner(girth, s + 1, j, side, k),
                };
                foreach (List<KwGirthPoint> piece in KwOpeningSkin.Apart(girth, side, KwClip.Between(quad, p => p.Girth, KwGirthPoint.Lerp, lo, hi)))
                    Face(b, k, side, piece);
            }
        }

        // One painted face, seen from outside the hull only (the corners turned round on the starboard side, the mirror).
        private static void Face(MeshBuilder b, int k, float side, List<KwGirthPoint> piece)
        {
            if (piece.Count < 3)
                return;
            List<Vector3> at = piece.ConvertAll(p => p.At + p.Out * (KwStrakes.Proud(p, k) + Proud));
            List<Vector2> uv = piece.ConvertAll(p => KwUv.Strake(true, p.Along, k, p.Girth - k * W));
            b.Row = new Vector2(k, side);
            if (side < 0f)
            {
                at.Reverse();
                uv.Reverse();
            }
            b.Face(at, uv);
        }
    }
}
