using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EarthWright.Preview
{
    /// <summary>
    /// The vertex lists a mesh is built from. Builders fill them relative to <see cref="Origin"/> (a nearby world
    /// position, so the floats stay precise far from the world centre) and a <see cref="MeshLayer"/> shows them.
    /// </summary>
    internal sealed class MeshData
    {
        public Vector3 Origin;
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Color> Colours = new List<Color>();
        public readonly List<int> Triangles = new List<int>();

        public void Clear(Vector3 origin)
        {
            Origin = origin;
            Vertices.Clear();
            Colours.Clear();
            Triangles.Clear();
        }

        /// <summary>Adds a quad (a, b, c, d in order around it) in one colour; world positions.</summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color colour)
        {
            int first = Vertices.Count;
            Vertices.Add(a - Origin);
            Vertices.Add(b - Origin);
            Vertices.Add(c - Origin);
            Vertices.Add(d - Origin);
            for (int i = 0; i < 4; i++)
                Colours.Add(colour);
            Triangles.Add(first);
            Triangles.Add(first + 1);
            Triangles.Add(first + 2);
            Triangles.Add(first);
            Triangles.Add(first + 2);
            Triangles.Add(first + 3);
        }

        /// <summary>Adds a triangle in one colour; world positions.</summary>
        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color colour)
        {
            int first = Vertices.Count;
            Vertices.Add(a - Origin);
            Vertices.Add(b - Origin);
            Vertices.Add(c - Origin);
            for (int i = 0; i < 3; i++)
                Colours.Add(colour);
            Triangles.Add(first);
            Triangles.Add(first + 1);
            Triangles.Add(first + 2);
        }
    }

    /// <summary>
    /// One combined mesh object (points, volume or grid): a single renderer however many markers it shows, never a
    /// GameObject per marker. Created on first use, re-created if destroyed, hidden by deactivating it.
    /// </summary>
    internal sealed class MeshLayer
    {
        private readonly string name;
        private GameObject go;
        private Mesh mesh;

        public MeshLayer(string name)
        {
            this.name = name;
        }

        public void Show(MeshData data)
        {
            if (data.Vertices.Count == 0 || !Ensure())
            {
                Hide();
                return;
            }
            mesh.Clear();
            mesh.indexFormat = data.Vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(data.Vertices);
            mesh.SetColors(data.Colours);
            mesh.SetTriangles(data.Triangles, 0);
            mesh.RecalculateBounds();
            go.transform.position = data.Origin;
            go.SetActive(true);
        }

        public void Hide()
        {
            if (go != null && go.activeSelf)
                go.SetActive(false);
        }

        private bool Ensure()
        {
            if (go != null && mesh != null)
                return true;
            Material material = PreviewMaterials.Surfaces;
            if (material == null)
                return false;
            if (go == null)
            {
                go = VisualRoot.CreateChild(name);
                go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>();
            }
            if (mesh != null)
                Object.Destroy(mesh);
            mesh = new Mesh { name = name, hideFlags = HideFlags.HideAndDontSave };
            mesh.MarkDynamic();
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return true;
        }
    }
}
