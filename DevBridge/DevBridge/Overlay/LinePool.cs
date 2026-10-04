using System.Collections.Generic;
using DevBridge.Hitbox;
using UnityEngine;
using UnityEngine.Rendering;

namespace DevBridge.Overlay
{
    /// <summary>
    /// One category's lines, kept between redraws and set in place each time instead of made and destroyed: a redraw
    /// takes lines from the front of the pool, and the ones it did not need are switched off. At most max lines a redraw.
    /// </summary>
    internal sealed class LinePool
    {
        private readonly string name;
        private readonly List<LineRenderer> lines = new List<LineRenderer>();
        private Transform parent;
        private int max;

        internal int Used { get; private set; }
        internal bool Capped { get; private set; }
        internal int Max => max;

        internal LinePool(string name) => this.name = name;

        /// <summary>Starts a redraw; after a logout the old lines went with the world scene, so a new parent is made.</summary>
        internal void Begin(Transform root, int cap)
        {
            if (!parent)
            {
                lines.Clear();
                parent = new GameObject(name).transform;
                parent.SetParent(root, false);
            }
            (Used, Capped, max) = (0, false, cap);
        }

        /// <summary>Whether another line fits under the cap, checked before working out an expensive shape.</summary>
        internal bool Room()
        {
            if (Used < max) return true;
            Capped = true;
            return false;
        }

        /// <summary>The next line of the pool through these points; false once the cap is reached, and nothing is drawn.</summary>
        internal bool Add(Vector3[] points, Color color, float width = 0.05f)
        {
            if (points.Length < 2) return true;
            if (!Room()) return false;
            LineRenderer line = Used < lines.Count && lines[Used] ? lines[Used] : Make(Used);
            Used++;
            (line.startColor, line.endColor, line.widthMultiplier) = (color, color, width);
            line.positionCount = points.Length;
            line.SetPositions(points);
            line.enabled = true;
            return true;
        }

        /// <summary>Ends a redraw: lines left over from a busier one are switched off, and any above the cap removed.</summary>
        internal void End()
        {
            for (int i = lines.Count - 1; i >= Used; i--)
            {
                if (i >= max && lines[i]) Object.Destroy(lines[i].gameObject);
                if (i >= max) lines.RemoveAt(i);
                else if (lines[i]) lines[i].enabled = false;
            }
        }

        internal void Clear()
        {
            if (parent) Object.Destroy(parent.gameObject);
            (parent, Used, Capped) = (null, 0, false);
            lines.Clear();
        }

        private LineRenderer Make(int index)
        {
            var go = new GameObject("line");
            go.transform.SetParent(parent, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            (line.useWorldSpace, line.sharedMaterial) = (true, Lines.Shared);
            (line.shadowCastingMode, line.receiveShadows) = (ShadowCastingMode.Off, false);
            if (index < lines.Count) lines[index] = line;
            else lines.Add(line);
            return line;
        }
    }
}
