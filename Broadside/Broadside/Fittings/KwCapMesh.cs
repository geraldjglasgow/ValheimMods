using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A post cap's mesh (<see cref="KwCapSpec"/>) wherever one is drawn: the generated part on the dock and on every peer
    /// (<see cref="KwGenerated"/>), the tool's ghost (<see cref="KwCapGhost"/>), a blueprint's look, the edit's preview and a
    /// launched ship (<see cref="KwBpMeshSet"/>), an old hammer dragon. A carved head is its model warped onto its post
    /// (<see cref="KwCapWarp"/>), the warped corners kept by the cap's text (style, base, length, lean, facing), so a head
    /// seen again is not warped again; each call makes a new mesh from them, its caller's to destroy. Every other cap, and a
    /// head whose model is not loaded, is drawn by the code (<see cref="KwCaps.Draw"/>).
    /// </summary>
    public static class KwCapMesh
    {
        private const int Kept = 48;
        private static readonly Dictionary<string, (Vector3[] Vertices, Vector3[] Normals, Vector4[] Tangents)> warped =
            new Dictionary<string, (Vector3[] Vertices, Vector3[] Normals, Vector4[] Tangents)>();

        /// <summary>The cap's mesh: its carved head where loaded, else the code's.</summary>
        public static Mesh Of(KwCapSpec spec) => Head(spec) ?? KwMesh.Build(spec.Def());

        /// <summary>The cap's carved head fitted to its post; null for a style the code draws or a head not loaded.</summary>
        public static Mesh? Head(KwCapSpec spec)
        {
            KwCapHead? head = KwCapHeads.Of(spec.Style);
            if (head == null)
                return null;
            var corners = Corners(head, spec);
            Mesh mesh = new Mesh { name = KwCapSpec.Made };
            mesh.SetVertices(corners.Vertices);
            mesh.SetNormals(corners.Normals);
            if (corners.Tangents.Length == corners.Vertices.Length)
                mesh.SetTangents(corners.Tangents);
            mesh.SetUVs(0, head.Uv);
            mesh.SetColors(new List<Color>(new Color[head.Vertices.Length]).ConvertAll(_ => Color.white));
            mesh.SetTriangles(head.Triangles, 0);
            if (corners.Tangents.Length != corners.Vertices.Length)
                mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        // The head's corners warped for the cap, made once for each text (a few dozen kept: the ghost steps through many).
        private static (Vector3[] Vertices, Vector3[] Normals, Vector4[] Tangents) Corners(KwCapHead head, KwCapSpec spec)
        {
            string key = spec.ToString();
            if (warped.TryGetValue(key, out var made))
                return made;
            if (warped.Count >= Kept)
                warped.Clear();
            return warped[key] = new KwCapWarp(spec, head).Apply(head);
        }
    }
}
