using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenKeep.Boots
{
    /// <summary>
    /// Cuts one of the game's skinned meshes in two by its triangles, at runtime: most of the game's leggings meshes are
    /// not readable on the CPU, so the index and vertex buffers are copied back from the GPU (once per mesh, at load) and
    /// both parts keep every vertex byte for byte (positions, normals, UVs, bone weights), the bind poses and the bounds;
    /// only the triangles are shared out. Nothing of the game's leaves the process. Needs a graphics device.
    /// </summary>
    internal static class MeshCut
    {
        private const MeshUpdateFlags Quiet =
            MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontNotifyMeshUsers;

        /// <summary>True when the mesh is what the split was made for: one triangle list of this many triangles.</summary>
        public static bool Fits(Mesh mesh, int triangles) =>
            mesh.subMeshCount == 1 && mesh.GetTopology(0) == MeshTopology.Triangles && mesh.blendShapeCount == 0
            && mesh.GetIndexCount(0) == (uint)triangles * 3 && mesh.vertexBufferCount > 0;

        /// <summary>[0] every triangle not flagged, [1] the flagged ones (the boots).</summary>
        public static Mesh[] Cut(Mesh source, bool[] flagged)
        {
            int[] indices = Indices(source);
            byte[][] streams = Streams(source);
            var parts = new[] { new List<int>(), new List<int>() };
            for (int i = 0; i + 2 < indices.Length; i += 3)
            {
                List<int> part = parts[flagged[i / 3] ? 1 : 0];
                part.Add(indices[i]);
                part.Add(indices[i + 1]);
                part.Add(indices[i + 2]);
            }
            return new[] { Build(source, streams, parts[0], "_ok_legs"), Build(source, streams, parts[1], "_ok_boots") };
        }

        private static int[] Indices(Mesh source)
        {
            using (GraphicsBuffer buffer = source.GetIndexBuffer())
            {
                var indices = new int[buffer.count];
                if (source.indexFormat == IndexFormat.UInt32)
                {
                    buffer.GetData(indices);
                    return indices;
                }
                var shorts = new ushort[buffer.count];
                buffer.GetData(shorts);
                for (int i = 0; i < shorts.Length; i++)
                    indices[i] = shorts[i];
                return indices;
            }
        }

        private static byte[][] Streams(Mesh source)
        {
            var streams = new byte[source.vertexBufferCount][];
            for (int stream = 0; stream < streams.Length; stream++)
            {
                if (source.GetVertexBufferStride(stream) == 0)
                    continue;
                using (GraphicsBuffer buffer = source.GetVertexBuffer(stream))
                {
                    streams[stream] = new byte[buffer.count * buffer.stride];
                    buffer.GetData(streams[stream]);
                }
            }
            return streams;
        }

        private static Mesh Build(Mesh source, byte[][] streams, List<int> indices, string suffix)
        {
            var mesh = new Mesh { name = source.name + suffix };
            mesh.SetVertexBufferParams(source.vertexCount, source.GetVertexAttributes());
            for (int stream = 0; stream < streams.Length; stream++)
            {
                if (streams[stream] != null)
                    mesh.SetVertexBufferData(streams[stream], 0, 0, streams[stream].Length, stream, Quiet);
            }
            SetIndices(mesh, indices, source.indexFormat);
            mesh.bindposes = source.bindposes;
            mesh.bounds = source.bounds;
            mesh.UploadMeshData(true);
            return mesh;
        }

        private static void SetIndices(Mesh mesh, List<int> indices, IndexFormat format)
        {
            mesh.SetIndexBufferParams(indices.Count, format);
            if (format == IndexFormat.UInt32)
                mesh.SetIndexBufferData(indices.ToArray(), 0, 0, indices.Count, Quiet);
            else
                mesh.SetIndexBufferData(indices.ConvertAll(i => (ushort)i).ToArray(), 0, 0, indices.Count, Quiet);
            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, indices.Count), Quiet);
        }
    }
}
