using UnityEngine;

namespace EarthWright.Preview
{
    /// <summary>
    /// The one parent object of every preview visual, kept across scene loads so logging out and in again does not
    /// leave dangling references. Children are plain renderers without colliders on the default layer: only this
    /// machine draws them, nothing is networked and nothing blocks the game's raycasts.
    /// </summary>
    internal static class VisualRoot
    {
        private static GameObject root;

        /// <summary>A new, inactive child object under the root.</summary>
        public static GameObject CreateChild(string name)
        {
            if (root == null)
            {
                root = new GameObject("EarthWright Preview");
                Object.DontDestroyOnLoad(root);
            }
            GameObject child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            child.SetActive(false);
            return child;
        }
    }

    /// <summary>
    /// One LineRenderer in world space: created on first use, re-created if something destroyed it, hidden by
    /// deactivating it. The line takes its colour from the vertex colours, so all lines share one material.
    /// </summary>
    internal sealed class LineStrip
    {
        private readonly string name;
        private GameObject go;
        private LineRenderer line;
        private Vector3[] buffer = new Vector3[0];

        public LineStrip(string name)
        {
            this.name = name;
        }

        /// <summary>Draws the points as a line (closed when <paramref name="loop"/>). Nothing when no material exists.</summary>
        public void Show(Vector3[] points, int count, bool loop, Color colour, float width)
        {
            if (count < 2 || !Ensure())
            {
                Hide();
                return;
            }
            if (buffer.Length != count)
                buffer = new Vector3[count];
            System.Array.Copy(points, buffer, count);
            line.positionCount = count;
            line.SetPositions(buffer);
            line.loop = loop;
            line.startColor = line.endColor = colour;
            line.startWidth = line.endWidth = width;
            go.SetActive(true);
        }

        public void Hide()
        {
            if (go != null && go.activeSelf)
                go.SetActive(false);
        }

        private bool Ensure()
        {
            if (go != null)
                return true;
            Material material = PreviewMaterials.Lines;
            if (material == null)
                return false;
            go = VisualRoot.CreateChild(name);
            line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.sharedMaterial = material;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            return true;
        }
    }
}
