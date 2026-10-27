using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A splinter at a hole's broken edge (<see cref="KwHoleRim"/>): a thin tapering stick of the board's own wood from the
    /// middle of the broken end at a notch, reaching into the hole partly along the grain and bent 20 to 50 degrees in
    /// through the hull, as long as a third to two thirds of the hole's radius but clear of its middle; its sizes and bend from the hole's seed, so
    /// every peer draws the same. Four faces to its tip, each facing out of the stick. Pure.
    /// </summary>
    public static class KwHoleSplinter
    {
        public static void Add(KwHoleMesh cut, List<int> into, KwHoleShape hole, KwRimSample at)
        {
            if (!at.Solid)
                return;
            float n1 = KwHoleShape.Noise(hole.Seed + 7, at.Corner), n2 = KwHoleShape.Noise(hole.Seed + 8, at.Corner);
            Vector3 root = hole.Point(at.At, (at.Outer + at.Inner) / 2f), way = Way(hole, at.At, n1);
            float length = Clear(hole, at.At, way, Mathf.Max(0.06f, hole.Radius * (0.3f + 0.35f * n2))), width = 0.018f + 0.022f * n1;
            float thick = Mathf.Clamp((at.Outer - at.Inner) * 0.5f, 0.01f, 0.05f);
            Vector3 side = Vector3.Cross(way, hole.Normal).normalized, lift = Vector3.Cross(side, way).normalized;
            Vector3 tip = root + way * length + side * ((n2 - 0.5f) * width);
            Vector3[] foot =
            {
                root - side * (width / 2f) - lift * (thick / 2f), root + side * (width / 2f) - lift * (thick / 2f),
                root + side * (width / 2f) + lift * (thick / 2f), root - side * (width / 2f) + lift * (thick / 2f),
            };
            Vector3 inside = root + way * (length * 0.25f);
            Vector2[] uvs = { at.Uv, at.Uv + new Vector2(0.003f, 0f), at.Uv + new Vector2(0.012f, 0.004f) };
            for (int i = 0; i < 4; i++)
            {
                Vector3[] face = { foot[i], foot[(i + 1) % 4], tip };
                Vector3 facing = Vector3.Cross(face[1] - face[0], face[2] - face[0]).normalized;
                KwHoleRim.Polygon(cut, into, face, uvs, Vector3.Dot(facing, (face[0] + face[1] + face[2]) / 3f - inside) >= 0f ? facing : -facing);
            }
        }

        // A splinter's length shortened until its tip stays out of the hole's middle (two thirds of its inner radius round
        // it), so a shot or a look straight through the middle is clear.
        private static float Clear(KwHoleShape hole, Vector2 at, Vector3 way, float length)
        {
            Vector2 flat = new Vector2(Vector3.Dot(way, hole.Along), Vector3.Dot(way, hole.Up));
            for (int i = 0; i < 8 && (at + flat * length).magnitude < 0.67f * hole.Inner; i++)
                length *= 0.7f;
            return length;
        }

        // The way a splinter reaches: toward the hole's middle, half turned along the grain, bent in through the hull.
        private static Vector3 Way(KwHoleShape hole, Vector2 at, float noise)
        {
            Vector2 inward = at.sqrMagnitude > 1e-8f ? -at.normalized : Vector2.right;
            Vector2 flat = (inward * 0.55f + new Vector2(inward.x >= 0f ? 0.45f : -0.45f, 0f)).normalized;
            float bend = (20f + 30f * noise) * Mathf.Deg2Rad;
            return ((hole.Along * flat.x + hole.Up * flat.y) * Mathf.Cos(bend) - hole.Normal * Mathf.Sin(bend)).normalized;
        }
    }
}
