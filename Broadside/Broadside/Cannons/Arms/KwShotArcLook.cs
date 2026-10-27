using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// How the shot arcs are drawn (<see cref="KwShotArcs"/>), on the captain's screen: thin unlit lines, no skin of their
    /// own, the game's sprite shader (vertex colours, transparent, depth tested, so the ship's own hull hides what is behind
    /// it), kept in a pool under one object riding on the ship steered (its points in the ship's own space, so they move
    /// with it between two looks). An arc is a faint white dotted line (a dot a segment, the path's points about two
    /// metres apart) ending where the ball strikes, with no marker there; dimmed for a cannon not loaded. Its width grows
    /// with the distance from the camera, so it looks as thin far off as near.
    /// </summary>
    public sealed class KwShotArcLook
    {
        private const float ArcWidth = 0.0022f, Least = 0.025f, Dim = 0.3f;
        private static readonly Color Faint = new Color(1f, 1f, 1f, 0.5f);

        private readonly List<LineRenderer> arcs = new List<LineRenderer>();
        private Vector3[] buffer = new Vector3[160];
        private Transform? root, ship;
        private Material? dotted;
        private int arcsUsed;
        private bool shown;

        /// <summary>A new look begins: the pool under the ship steered.</summary>
        public void Begin(Transform onShip)
        {
            if (root == null)
            {
                root = new GameObject("Broadside_ShotArcLines").transform;
                (ship, shown) = (null, false);
                arcs.Clear();
            }
            if (ship != onShip)
            {
                root.SetParent(onShip, false);
                ship = onShip;
            }
            (arcsUsed, shown) = (0, true);
        }

        /// <summary>An arc through world points, faint, dimmed when its cannon is not loaded.</summary>
        public void Arc(List<Vector3> points, bool ready)
        {
            Material? material = dotted ??= KwShotArcMaterials.Dotted();
            if (points.Count < 2 || material == null)
                return;
            LineRenderer line = Next(material);
            if (buffer.Length < points.Count)
                buffer = new Vector3[Mathf.NextPowerOfTwo(points.Count)];
            for (int i = 0; i < points.Count; i++)
                buffer[i] = ship!.InverseTransformPoint(points[i]);
            line.positionCount = points.Count;
            line.SetPositions(buffer);
            line.startColor = line.endColor = Shade(Faint, ready);
            (line.startWidth, line.endWidth) = (Width(points[0], ArcWidth), Width(points[points.Count - 1], ArcWidth));
        }

        /// <summary>The look done: the lines it did not use switched off.</summary>
        public void End()
        {
            Off(arcs, arcsUsed);
        }

        /// <summary>Everything switched off (once).</summary>
        public void Hide()
        {
            if (!shown)
                return;
            shown = false;
            Off(arcs, 0);
        }

        // The next line of the pool, made the first time (under the root, in its own space), switched on.
        private LineRenderer Next(Material material)
        {
            if (arcsUsed == arcs.Count || arcs[arcsUsed] == null)
            {
                LineRenderer made = KwShotArcMaterials.Line(root!, material);
                if (arcsUsed == arcs.Count)
                    arcs.Add(made);
                else
                    arcs[arcsUsed] = made;
            }
            LineRenderer line = arcs[arcsUsed++];
            if (!line.enabled)
                line.enabled = true;
            return line;
        }

        private static void Off(List<LineRenderer> pool, int from)
        {
            for (int i = from; i < pool.Count; i++)
            {
                if (pool[i] != null && pool[i].enabled)
                    pool[i].enabled = false;
            }
        }

        private static Color Shade(Color colour, bool ready) => ready ? colour : new Color(colour.r, colour.g, colour.b, colour.a * Dim);

        // A width that looks the same on screen near and far: so much a metre of distance from the camera.
        private static float Width(Vector3 at, float perMetre)
        {
            Camera? view = Utils.GetMainCamera();
            return view != null ? Mathf.Max(Least, Vector3.Distance(view.transform.position, at) * perMetre) : Least;
        }
    }

    /// <summary>The shot arcs' material and lines: the game's sprite shader with a dot texture made here.</summary>
    public static class KwShotArcMaterials
    {
        private const int Queue = 3100;

        /// <summary>The dotted arcs' material: one soft dot a segment of the line; null when the shader is missing (nothing drawn).</summary>
        public static Material? Dotted()
        {
            Shader? shader = Shader.Find("Sprites/Default") ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended") ?? Shader.Find("UI/Default");
            return shader != null ? new Material(shader) { name = "Broadside shot arc", renderQueue = Queue, mainTexture = Dots() } : null;
        }

        /// <summary>A line under the root, in its space: unlit, no shadows, its dot texture repeated once a segment.</summary>
        public static LineRenderer Line(Transform root, Material material)
        {
            GameObject go = new GameObject("arc");
            go.transform.SetParent(root, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            (line.sharedMaterial, line.useWorldSpace, line.loop) = (material, false, false);
            (line.shadowCastingMode, line.receiveShadows, line.numCapVertices) = (ShadowCastingMode.Off, false, 0);
            line.textureMode = LineTextureMode.RepeatPerSegment;
            return line;
        }

        // Eight texels across, a soft dot in the middle three: a dot, then a gap, each segment.
        private static Texture2D Dots()
        {
            Texture2D dots = new Texture2D(8, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, name = "Broadside shot dots" };
            float[] alpha = { 0f, 0f, 0.45f, 1f, 1f, 0.45f, 0f, 0f };
            for (int i = 0; i < alpha.Length; i++)
                dots.SetPixel(i, 0, new Color(1f, 1f, 1f, alpha[i]));
            dots.Apply(false, true);
            return dots;
        }
    }
}
