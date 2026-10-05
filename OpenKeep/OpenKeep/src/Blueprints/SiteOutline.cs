using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// Lines on the levelled ground of a placed blueprint: its whole site (green, or red while it cannot be built there)
    /// and every dug square, such as a canal or a pool, at its floor (blue when it lies below sea level and so holds
    /// water). Plain LineRenderers on this machine only.
    /// </summary>
    internal static class SiteOutline
    {
        private const float Width = 0.08f;
        private const float Lift = 0.05f;
        private static readonly Color Normal = new Color(0.45f, 0.95f, 0.45f, 0.9f);
        private static readonly Color Blocked = new Color(1f, 0.35f, 0.3f, 0.9f);
        private static readonly Color Water = new Color(0.25f, 0.55f, 1f, 0.9f);

        private static readonly List<LineRenderer> lines = new List<LineRenderer>();
        private static readonly Vector3[] points = new Vector3[4];
        private static GameObject root;
        private static Material material;

        public static void Show(Blueprint bp, BuildFrame frame, bool blocked)
        {
            Draw(0, SiteArea.Local(bp), 0f, frame, blocked ? Blocked : Normal);
            int used = 1;
            foreach (LevelStep step in bp.Levels)
            {
                if (step.Y >= 0f)
                    continue;
                Rect square = Rect.MinMaxRect(step.X - step.Half, step.Z - step.Half, step.X + step.Half, step.Z + step.Half);
                Draw(used++, square, step.Y, frame, frame.Ground + step.Y < WaterRule.Sea ? Water : Normal);
            }
            for (int i = used; i < lines.Count; i++)
                lines[i].gameObject.SetActive(false);
        }

        public static void Hide()
        {
            foreach (LineRenderer line in lines)
            {
                if (line != null && line.gameObject.activeSelf)
                    line.gameObject.SetActive(false);
            }
        }

        private static void Draw(int index, Rect local, float y, BuildFrame frame, Color colour)
        {
            LineRenderer line = Line(index);
            int n = 0;
            foreach (Vector2 corner in SiteArea.Corners(local, 0f))
                points[n++] = frame.World(corner.x, y + Lift, corner.y);
            line.positionCount = 4;
            line.SetPositions(points);
            line.startColor = line.endColor = colour;
            line.gameObject.SetActive(true);
        }

        private static LineRenderer Line(int index)
        {
            if (root == null)
            {
                root = new GameObject("OpenKeep Blueprint Outline");
                lines.Clear();
            }
            while (lines.Count <= index)
                lines.Add(NewLine());
            return lines[index];
        }

        private static LineRenderer NewLine()
        {
            GameObject go = new GameObject("line");
            go.transform.SetParent(root.transform, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.startWidth = line.endWidth = Width;
            line.sharedMaterial = Material();
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private static Material Material()
        {
            if (material == null)
                material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Standard"));
            return material;
        }
    }
}
