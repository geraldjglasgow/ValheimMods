using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>A Unity mesh read into a <see cref="KwHoleMesh"/>'s lists, and the lists written into a Unity mesh.</summary>
    public static class KwHoleMeshes
    {
        public static KwHoleMesh Read(Mesh mesh)
        {
            KwHoleMesh data = new KwHoleMesh();
            mesh.GetVertices(data.Points);
            mesh.GetNormals(data.Normals);
            mesh.GetTangents(data.Tangents);
            mesh.GetUVs(0, data.Uvs);
            mesh.GetColors(data.Colours);
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                List<int> triangles = new List<int>();
                mesh.GetTriangles(triangles, s);
                data.Subs.Add(triangles);
            }
            return data;
        }

        /// <summary>The lists into a mesh, every channel the lists hold whole; its bounds made again.</summary>
        public static void Write(KwHoleMesh data, Mesh mesh)
        {
            mesh.Clear();
            mesh.indexFormat = data.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(data.Points);
            if (data.Normals.Count == data.Count)
                mesh.SetNormals(data.Normals);
            if (data.Tangents.Count == data.Count)
                mesh.SetTangents(data.Tangents);
            if (data.Uvs.Count == data.Count)
                mesh.SetUVs(0, data.Uvs);
            if (data.Colours.Count == data.Count)
                mesh.SetColors(data.Colours);
            mesh.subMeshCount = data.Subs.Count;
            for (int s = 0; s < data.Subs.Count; s++)
                mesh.SetTriangles(data.Subs[s], s, false);
            mesh.RecalculateBounds();
        }
    }
}
