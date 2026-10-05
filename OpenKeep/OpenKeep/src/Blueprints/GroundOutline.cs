using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// The outline of a ground fix on the ground it will have: yellow round the ground under the building (where pieces
    /// touch it), blue round all the ground that moves or is painted. Thin flat strips along the edges of the one-metre
    /// cells, one mesh, vertex coloured, drawn only on this machine.
    /// </summary>
    internal static class GroundOutline
    {
        private const float Width = 0.12f;
        private const float Lift = 0.08f;
        private static readonly Color Under = new Color(1f, 0.85f, 0.2f, 0.95f);
        private static readonly Color Moved = new Color(0.3f, 0.75f, 1f, 0.85f);
        private static readonly (int dx, int dz)[] Sides = { (1, 0), (-1, 0), (0, 1), (0, -1) };

        private static GameObject go;
        private static Mesh mesh;
        private static GroundWork shown;

        public static void Show(GroundWork work, Func<Vector3, bool> under)
        {
            if (!Ensure())
                return;
            if (work != shown)
                Build(work, under);
            go.SetActive(true);
        }

        public static void Hide()
        {
            if (go != null && go.activeSelf)
                go.SetActive(false);
        }

        private static void Build(GroundWork work, Func<Vector3, bool> under)
        {
            shown = work;
            Dictionary<long, float> all = new Dictionary<long, float>();
            HashSet<long> pad = new HashSet<long>();
            foreach (GroundPoint p in work.Points)
            {
                long key = GroundWork.Key(p.X, p.Z);
                all[key] = p.Height;
                if (under(new Vector3(p.X, 0f, p.Z)))
                    pad.Add(key);
            }
            List<Vector3> vertices = new List<Vector3>();
            List<Color> colours = new List<Color>();
            foreach (GroundPoint p in work.Points)
                AddEdges(p, all, pad, vertices, colours);
            Fill(vertices, colours);
        }

        /// <summary>An edge strip on every side of the vertex's cell that borders a cell outside its set (both sets).</summary>
        private static void AddEdges(GroundPoint p, Dictionary<long, float> all, HashSet<long> pad, List<Vector3> v, List<Color> c)
        {
            bool inPad = pad.Contains(GroundWork.Key(p.X, p.Z));
            foreach ((int dx, int dz) in Sides)
            {
                long next = GroundWork.Key(p.X + dx, p.Z + dz);
                if (!all.ContainsKey(next))
                    Strip(p, dx, dz, Moved, v, c);
                if (inPad && !pad.Contains(next))
                    Strip(p, dx, dz, Under, v, c, 0.04f);
            }
        }

        private static void Strip(GroundPoint p, int dx, int dz, Color colour, List<Vector3> v, List<Color> c, float raise = 0f)
        {
            float y = p.Height + Lift + raise;
            Vector3 mid = new Vector3(p.X + dx * 0.5f, y, p.Z + dz * 0.5f);
            Vector3 along = new Vector3(dz, 0f, dx) * 0.5f;
            Vector3 across = new Vector3(dx, 0f, dz) * (Width * 0.5f);
            v.Add(mid - along - across);
            v.Add(mid + along - across);
            v.Add(mid + along + across);
            v.Add(mid - along + across);
            for (int i = 0; i < 4; i++)
                c.Add(colour);
        }

        private static void Fill(List<Vector3> vertices, List<Color> colours)
        {
            int[] quads = new int[vertices.Count];
            for (int i = 0; i < quads.Length; i++)
                quads[i] = i;
            mesh.Clear();
            mesh.indexFormat = vertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetColors(colours);
            mesh.SetIndices(quads, MeshTopology.Quads, 0);
            mesh.RecalculateBounds();
        }

        private static bool Ensure()
        {
            if (go != null)
                return true;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                return false;
            go = new GameObject("OpenKeep Fix Ground Outline");
            mesh = new Mesh { name = "OpenKeep fix ground outline" };
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(shader) { renderQueue = 3100 };
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            shown = null;
            return true;
        }
    }
}
