using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// What lies on top of the leggings: the look's bands (<see cref="LegStyle.Bands"/>: belt, straps, trims, an ankle
    /// cuff without boots, measured from the leggings' lower end), each the shell cut between two planes and lifted a little further off it so it bends with
    /// the shell, and its buckle at the belt's front (<see cref="LegStyle.Buckle"/>), taking the skin weights of the body
    /// vertex under it.
    /// </summary>
    internal static class FittedBands
    {
        public static void Add(LegStyle style, BodySurface body, bool[] covered, Unwrap unwrap, FittedMesh mesh, float bottom, bool booted)
        {
            foreach (BandSpec band in style.Bands)
            {
                if (band.Ankle && booted)
                    continue;
                float offset = band.Ankle ? bottom : 0f;
                for (int t = 0; t < covered.Length; t++)
                {
                    if (covered[t])
                        Piece(body, unwrap, mesh, band, offset, style.Thickness + band.Lift, t);
                }
            }
            int front = Front(body);
            if (front >= 0)
                Disc(body, mesh, style.Buckle, style.Thickness, front);
        }

        /// <summary>The part of one covered triangle inside the band, <paramref name="lift"/> off the skin.</summary>
        private static void Piece(BodySurface body, Unwrap unwrap, FittedMesh mesh, BandSpec band, float offset, float lift, int t)
        {
            var corners = new List<FittedCorner>(3);
            for (int i = 0; i < 3; i++)
                corners.Add(Corner(body, body.Triangles[3 * t + i], lift));
            int part = Unwrap.Part((corners[0].Rest + corners[1].Rest + corners[2].Rest) / 3f);
            if (band.Hips != (part == 0))
                return;
            float from = band.From + offset, to = band.To + offset;
            corners = FittedCorner.Clip(FittedCorner.Clip(corners, band.Normal, from), -band.Normal, -to);
            if (corners.Count < 3)
                return;
            float[] angles = unwrap.Angles(part, corners.ConvertAll(c => c.Rest).ToArray());
            int first = -1;
            for (int i = 0; i < corners.Count; i++)
            {
                float across = (to - Vector3.Dot(corners[i].Rest, band.Normal)) / (to - from);
                Vector2 uv = FittedAtlas.Region(band.Region, angles[i] / Unwrap.Turn, across, band.Rotated);
                int index = mesh.Add(corners[i].Position, corners[i].Normal, uv, corners[i].Weight);
                first = first < 0 ? index : first;
            }
            mesh.Fan(first, corners.Count);
        }

        private static FittedCorner Corner(BodySurface body, int v, float lift) => new FittedCorner
        {
            Position = body.Out(v, lift),
            Normal = body.Outward[v],
            Rest = body.Rest[v],
            Weight = body.Weights[v],
        };

        /// <summary>A flat round piece facing out from the body vertex it sits on, its texture a disc of the atlas.</summary>
        private static void Disc(BodySurface body, FittedMesh mesh, DiscSpec disc, float thickness, int vertex)
        {
            Vector3 normal = body.Outward[vertex];
            Vector3 centre = body.Out(vertex, thickness + 0.01f);
            Vector3 up = Vector3.Cross(normal, Vector3.right).normalized, side = Vector3.Cross(up, normal).normalized;
            float radius = disc.Radius / BodySurface.Scale;
            BoneWeight weight = body.Weights[vertex];
            Vector2 middle = FittedAtlas.Px(disc.Centre.x, disc.Centre.y);
            int hub = mesh.Add(centre + normal * (0.004f / BodySurface.Scale), normal, middle, weight);
            for (int j = 0; j < 10; j++)
            {
                float a = j * Unwrap.Turn / 10f, cos = Mathf.Cos(a), sin = Mathf.Sin(a);
                Vector2 uv = FittedAtlas.Px(disc.Centre.x + cos * disc.Texels, disc.Centre.y - sin * disc.Texels);
                mesh.Add(centre + (side * cos + up * sin) * radius, normal, uv, weight);
            }
            for (int j = 0; j < 10; j++)
                mesh.Both(hub, hub + 1 + j, hub + 1 + (j + 1) % 10);
        }

        /// <summary>The foremost body vertex within 4 cm of the middle and of the belt's height; -1 for none.</summary>
        private static int Front(BodySurface body)
        {
            int best = -1;
            float forward = float.MinValue, height = (Belt.Bottom + Belt.Top) / 2f;
            for (int i = 0; i < body.Rest.Length; i++)
            {
                Vector3 r = body.Rest[i];
                if (Mathf.Abs(r.x) <= 0.04f && Mathf.Abs(r.y - height) <= 0.04f && r.z > forward && !body.Upper[i])
                {
                    forward = r.z;
                    best = i;
                }
            }
            return best;
        }
    }
}
