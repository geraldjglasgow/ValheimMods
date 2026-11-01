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
            int a = body.Triangles[3 * t], b = body.Triangles[3 * t + 1], c = body.Triangles[3 * t + 2];
            int part = Unwrap.Part((body.Rest[a] + body.Rest[b] + body.Rest[c]) / 3f);
            float from = band.From + offset, to = band.To + offset;
            if (band.Hips != (part == 0) || Outside(body.Rest[a], body.Rest[b], body.Rest[c], band.Normal, from, to))
                return;
            var corners = new List<FittedCorner>(3) { Corner(body, a, lift), Corner(body, b, lift), Corner(body, c, lift) };
            corners = FittedCorner.Clip(FittedCorner.Clip(corners, band.Normal, from), -band.Normal, -to);
            if (corners.Count < 3)
                return;
            float[] angles = unwrap.Angles(part, corners.ConvertAll(corner => corner.Rest).ToArray());
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

        /// <summary>
        /// True when the two clips would leave nothing of the triangle, found before any of it is built (most triangles lie
        /// outside a band): every corner dropped at the lower plane, or none dropped there and every corner at the upper
        /// one. It is the clip's own test on the same corners, so exactly the triangles the clips would empty are skipped.
        /// </summary>
        private static bool Outside(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, float from, float to)
        {
            bool dropA = Dropped(a, normal, from), dropB = Dropped(b, normal, from), dropC = Dropped(c, normal, from);
            if (dropA && dropB && dropC)
                return true;
            Vector3 down = -normal;
            return !dropA && !dropB && !dropC && Dropped(a, down, -to) && Dropped(b, down, -to) && Dropped(c, down, -to);
        }

        /// <summary>A corner <see cref="FittedCorner.Clip"/> drops at this plane (it keeps one at a distance of zero or more).</summary>
        private static bool Dropped(Vector3 rest, Vector3 normal, float distance) => !(Vector3.Dot(rest, normal) - distance >= 0f);

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
