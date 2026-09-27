using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EarthWright.Paths
{
    /// <summary>
    /// A small flat square on every terrain point a planned ramp or road changes, at the height it will get: blue where
    /// the ground is filled, orange where it is cut, white where it barely moves, red past the height limit; fainter
    /// on the shoulders. One mesh, rebuilt when the plan changes.
    /// </summary>
    public sealed class VertexDots
    {
        private const float Half = 0.09f;
        private const float Lift = 0.06f;
        private const float Moves = 0.05f;
        private const int MaxDots = 16000;

        private static readonly Color Filled = new Color(0.35f, 0.65f, 1f);
        private static readonly Color Dug = new Color(1f, 0.6f, 0.2f);
        private static readonly Color Even = new Color(0.95f, 0.95f, 0.95f);
        private static readonly Color Past = new Color(1f, 0.1f, 0.1f);

        private readonly List<Vector3> corners = new List<Vector3>();
        private readonly List<Color> colours = new List<Color>();
        private readonly List<int> triangles = new List<int>();
        private GameObject holder;
        private Mesh mesh;
        private MeshRenderer renderer;

        public void Show(List<PlannedVertex> vertices, float alpha, Material material)
        {
            if (material == null || vertices.Count == 0)
            {
                Hide();
                return;
            }
            Ensure();
            Fill(vertices, alpha);
            mesh.Clear();
            mesh.SetVertices(corners);
            mesh.SetColors(colours);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            renderer.sharedMaterial = material;
            holder.SetActive(true);
        }

        public void Hide()
        {
            if (holder != null)
                holder.SetActive(false);
        }

        private void Ensure()
        {
            if (holder != null)
                return;
            if (mesh != null)
                Object.Destroy(mesh);
            holder = new GameObject("EarthWright.PathDots");
            mesh = new Mesh { name = "EarthWright.PathDots", indexFormat = IndexFormat.UInt32 };
            mesh.MarkDynamic();
            holder.AddComponent<MeshFilter>().sharedMesh = mesh;
            renderer = holder.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void Fill(List<PlannedVertex> vertices, float alpha)
        {
            corners.Clear();
            colours.Clear();
            triangles.Clear();
            int count = 0;
            foreach (PlannedVertex vertex in vertices)
            {
                if (!vertex.Loaded || count++ >= MaxDots)
                    continue;
                AddDot(new Vector3(vertex.X, vertex.Final + Lift, vertex.Z), DotColour(vertex, alpha));
            }
        }

        private void AddDot(Vector3 centre, Color colour)
        {
            int first = corners.Count;
            corners.Add(centre + new Vector3(-Half, 0f, -Half));
            corners.Add(centre + new Vector3(-Half, 0f, Half));
            corners.Add(centre + new Vector3(Half, 0f, Half));
            corners.Add(centre + new Vector3(Half, 0f, -Half));
            for (int i = 0; i < 4; i++)
                colours.Add(colour);
            triangles.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
        }

        private static Color DotColour(PlannedVertex vertex, float alpha)
        {
            float change = vertex.Final - vertex.Current;
            Color colour = vertex.PastLimit ? Past : change > Moves ? Filled : change < -Moves ? Dug : Even;
            colour.a = alpha * (0.35f + 0.65f * vertex.Weight);
            return colour;
        }
    }
}
