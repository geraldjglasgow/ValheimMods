using System.Collections.Generic;
using PackPanel.Core;
using UnityEngine;

namespace PackPanel.Look
{
    /// <summary>One continuous wood silhouette and bevel; no separate border texture or metal band.</summary>
    public static class TimberFrame
    {
        public static float Width => InventorySettings.FrameWidth?.Value ?? 5.5f;
        private static float Roughness => InventorySettings.FrameJaggedness?.Value ?? 1.5f;
        private static bool pending;
        public static int Version { get; private set; }

        public static float BevelWidth(Rect rect, bool round)
        {
            float shortSide = Mathf.Min(rect.width, rect.height);
            float radius = round ? shortSide * 0.5f : Mathf.Min(12f, shortSide * 0.3f);
            float cut = round ? 0f : Mathf.Min(Roughness * 3f, shortSide * 0.12f);
            return Mathf.Min(Width, shortSide * 0.12f, Mathf.Max(0.5f, radius - cut - 0.75f));
        }

        public static void Initialize()
        {
            InventorySettings.FrameWidth.SettingChanged += (sender, args) => pending = true;
            InventorySettings.FrameJaggedness.SettingChanged += (sender, args) => pending = true;
        }

        public static void Poll()
        {
            if (!pending) return;
            pending = false;
            Version++;
        }

        // Recognize previous preview sprites when restoring an existing panel.
        public static bool Owns(Sprite sprite) => sprite != null &&
            (sprite.name == "PackPanel_frame" || sprite.name == "PackPanel_ringframe");

        public static void Contour(Rect rect, bool round, List<Vector2> points, List<Vector2> normals)
        {
            points.Clear();
            normals.Clear();
            if (round)
            {
                for (int i = 0; i < 192; i++)
                {
                    float angle = -i * Mathf.PI * 2f / 192f;
                    Vector2 n = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    points.Add(rect.center + Vector2.Scale(n, rect.size * 0.5f));
                    normals.Add(n);
                }
                return;
            }
            float radius = Mathf.Min(12f, Mathf.Min(rect.width, rect.height) * 0.3f);
            Vector2[] centers = {
                new Vector2(rect.xMin + radius, rect.yMax - radius),
                new Vector2(rect.xMax - radius, rect.yMax - radius),
                new Vector2(rect.xMax - radius, rect.yMin + radius),
                new Vector2(rect.xMin + radius, rect.yMin + radius)
            };
            for (int side = 0; side < 4; side++)
            {
                float startAngle = (180f - side * 90f) * Mathf.Deg2Rad;
                Vector2 normal = new Vector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle));
                Vector2 start = centers[(side + 3) % 4] + normal * radius;
                Vector2 end = centers[side] + normal * radius;
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(start, end) / 2f));
                for (int i = 0; i < steps; i++)
                    Add(Vector2.Lerp(start, end, i / (float)steps), normal, rect, points, normals);
                for (int i = 0; i < 8; i++)
                {
                    float angle = startAngle - i / 8f * Mathf.PI * 0.5f;
                    Vector2 n = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Add(centers[side] + n * radius, n, rect, points, normals);
                }
            }
        }

        private static void Add(Vector2 p, Vector2 n, Rect rect, List<Vector2> points, List<Vector2> normals)
        {
            // Deliberate planar chips with visible depth. Each side varies along its own length,
            // rather than averaging two smooth fields until the silhouette becomes a straight line.
            float x = p.x - rect.xMin, y = p.y - rect.yMin;
            float vertical = Noise(y, 17f, 2f) * 0.8f + Noise(y, 57f, 12f) * 0.2f;
            float horizontal = Noise(x, 21f, 9f) * 0.8f + Noise(x, 63f, 21f) * 0.2f;
            float depth = Mathf.Min(Roughness * 3f, Mathf.Min(rect.width, rect.height) * 0.12f);
            float weight = Mathf.Abs(n.x) + Mathf.Abs(n.y);
            float cut = depth * (0.12f + (Mathf.Abs(n.x) * vertical + Mathf.Abs(n.y) * horizontal) / weight * 0.88f);
            float profile = (Mathf.Abs(n.x) * vertical + Mathf.Abs(n.y) * horizontal) / weight;
            cut += depth * 0.45f * Mathf.Clamp01((profile - 0.55f) / 0.3f);
            points.Add(p - n * cut);
            normals.Add(n);
        }

        private static float Noise(float position, float spacing, float seed)
        {
            float cell = Mathf.Floor(position / spacing);
            float left = (cell + Hash(cell + seed + 41f) * 0.4f) * spacing;
            if (position < left)
            {
                cell--;
                left = (cell + Hash(cell + seed + 41f) * 0.4f) * spacing;
            }
            float right = (cell + 1f + Hash(cell + seed + 42f) * 0.4f) * spacing;
            float t = (position - left) / (right - left);
            // Ease the cut faces slightly without reducing their peak-to-valley depth.
            t = Mathf.Lerp(t, t * t * (3f - 2f * t), 0.35f);
            return Mathf.Lerp(Hash(cell + seed), Hash(cell + seed + 1f), t);
        }

        private static float Hash(float value) => Mathf.Repeat(Mathf.Sin(value * 127.1f) * 43758.5453f, 1f);

        public static void Triangulate(List<Vector2> polygon, List<int> triangles)
        {
            triangles.Clear();
            var remaining = new List<int>(polygon.Count);
            for (int i = 0; i < polygon.Count; i++) remaining.Add(i);
            int cursor = 0;
            while (remaining.Count > 2)
            {
                bool clipped = false;
                for (int step = 0; step < remaining.Count; step++)
                {
                    int i = (cursor + step) % remaining.Count;
                    int a = remaining[(i + remaining.Count - 1) % remaining.Count];
                    int b = remaining[i], c = remaining[(i + 1) % remaining.Count];
                    float turn = Cross(polygon[a], polygon[b], polygon[c]);
                    if (turn > 0.00001f) continue;
                    if (Mathf.Abs(turn) <= 0.00001f)
                    {
                        remaining.RemoveAt(i);
                        cursor = i % remaining.Count;
                        clipped = true;
                        break;
                    }
                    bool occupied = false;
                    float minX = Mathf.Min(polygon[a].x, Mathf.Min(polygon[b].x, polygon[c].x));
                    float maxX = Mathf.Max(polygon[a].x, Mathf.Max(polygon[b].x, polygon[c].x));
                    float minY = Mathf.Min(polygon[a].y, Mathf.Min(polygon[b].y, polygon[c].y));
                    float maxY = Mathf.Max(polygon[a].y, Mathf.Max(polygon[b].y, polygon[c].y));
                    foreach (int p in remaining)
                    {
                        if (p == a || p == b || p == c) continue;
                        if (polygon[p].x < minX || polygon[p].x > maxX
                            || polygon[p].y < minY || polygon[p].y > maxY) continue;
                        if (Cross(polygon[a], polygon[b], polygon[p]) <= 0f
                            && Cross(polygon[b], polygon[c], polygon[p]) <= 0f
                            && Cross(polygon[c], polygon[a], polygon[p]) <= 0f)
                        { occupied = true; break; }
                    }
                    if (occupied) continue;
                    triangles.Add(a); triangles.Add(b); triangles.Add(c);
                    remaining.RemoveAt(i);
                    cursor = i % remaining.Count;
                    clipped = true;
                    break;
                }
                if (!clipped) throw new System.InvalidOperationException("Wood contour intersects itself.");
            }
        }

        private static float Cross(Vector2 a, Vector2 b, Vector2 c) =>
            (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }
}


