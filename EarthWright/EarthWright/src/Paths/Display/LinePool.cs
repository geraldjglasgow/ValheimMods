using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EarthWright.Paths
{
    /// <summary>
    /// A pool of world-space line renderers under one scene object, reused from redraw to redraw: a redraw starts with
    /// <see cref="Begin"/>, takes lines with <see cref="Draw"/>, and <see cref="End"/> switches off the lines it did not
    /// use. The scene object goes with the scene on logout; the pool then starts a new one.
    /// </summary>
    public sealed class LinePool
    {
        private readonly string name;
        private readonly List<LineRenderer> lines = new List<LineRenderer>();
        private GameObject root;
        private Material material;
        private int used;
        private Vector3[] buffer = new Vector3[64];
        private readonly List<Vector3> pair = new List<Vector3> { Vector3.zero, Vector3.zero };

        public LinePool(string name)
        {
            this.name = name;
        }

        public void Begin(Material lineMaterial)
        {
            material = lineMaterial;
            used = 0;
            if (root != null)
                return;
            lines.Clear();
            root = new GameObject(name);
        }

        /// <summary>A line through points[from..to] (inclusive), in one colour.</summary>
        public void Draw(List<Vector3> points, int from, int to, Color colour, float width)
        {
            int count = to - from + 1;
            if (count < 2 || material == null)
                return;
            if (buffer.Length < count)
                buffer = new Vector3[Mathf.NextPowerOfTwo(count)];
            points.CopyTo(from, buffer, 0, count);
            LineRenderer line = Next();
            line.positionCount = count;
            line.SetPositions(buffer);
            line.startColor = colour;
            line.endColor = colour;
            line.widthMultiplier = width;
        }

        /// <summary>A straight line from a to b.</summary>
        public void Segment(Vector3 a, Vector3 b, Color colour, float width)
        {
            pair[0] = a;
            pair[1] = b;
            Draw(pair, 0, 1, colour, width);
        }

        public void End()
        {
            for (int i = used; i < lines.Count; i++)
            {
                if (lines[i] != null)
                    lines[i].enabled = false;
            }
        }

        public void Hide()
        {
            used = 0;
            End();
        }

        private LineRenderer Next()
        {
            if (used == lines.Count || lines[used] == null)
            {
                if (used == lines.Count)
                    lines.Add(null);
                lines[used] = Create();
            }
            LineRenderer line = lines[used++];
            line.sharedMaterial = material;
            line.enabled = true;
            return line;
        }

        private LineRenderer Create()
        {
            GameObject holder = new GameObject("line");
            holder.transform.SetParent(root.transform, false);
            LineRenderer line = holder.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.textureMode = LineTextureMode.Stretch;
            return line;
        }
    }
}
