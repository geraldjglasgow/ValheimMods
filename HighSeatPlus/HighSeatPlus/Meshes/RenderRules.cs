using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HighSeatPlus.Meshes
{
    /// <summary>What a combined mesh needs from the meshes it copies.</summary>
    internal sealed class MeshInfo
    {
        // Readable (the copy is made on the CPU) and made of triangles only.
        public bool Fits;
        public int Attributes;
        public int Vertices;
        public int SubMeshes;
    }

    /// <summary>
    /// Which renderers can be merged without changing how they look. A material qualifies when its shader works in
    /// world space (Custom/Piece, Custom/StaticRock and Unity's Standard: the piece shader's value noise, ripple, rain and
    /// snow are worked out from world positions unless the material is marked as a moving object, and moving objects
    /// are never merged), when it draws before transparent objects (<see cref="MaxOpaqueQueue"/>: blended materials are
    /// sorted per object by distance) and when it does not use the piece shader's local-space triplanar mapping.
    /// Vegetation, grass, rugs and creature shaders bend or fade by object position, so they stay with the game. A mesh
    /// qualifies when it is readable and made of triangles. Answers are kept per material and per mesh (both shared
    /// assets); reading a shader's name allocates, so it happens once per material. <see cref="LookBits"/> folds the
    /// renderer settings that must match within one combined renderer into one number.
    /// </summary>
    internal static class RenderRules
    {
        private const int MaxOpaqueQueue = 2450;

        private static readonly HashSet<string> Shaders = new HashSet<string> { "Custom/Piece", "Custom/StaticRock", "Standard" };
        private static readonly int MoveableObject = Shader.PropertyToID("_MoveableObject");
        private static readonly int TriplanarLocal = Shader.PropertyToID("_TriplanarLocalPos");
        private static readonly VertexAttribute[] Channels =
        {
            VertexAttribute.Normal, VertexAttribute.Tangent, VertexAttribute.Color, VertexAttribute.TexCoord0,
            VertexAttribute.TexCoord1, VertexAttribute.TexCoord2, VertexAttribute.TexCoord3
        };
        private static readonly Dictionary<int, bool> MaterialFits = new Dictionary<int, bool>();
        private static readonly Dictionary<int, MeshInfo> MeshInfos = new Dictionary<int, MeshInfo>();

        /// <summary>Whether this shown renderer can go into a combined mesh; fills its shared materials.</summary>
        public static Verdict Check(MeshRenderer renderer, List<Material> materials, out MeshFilter filter)
        {
            if (!renderer.TryGetComponent(out filter) || filter.sharedMesh == null)
            {
                return Verdict.Never;
            }
            MeshInfo info = Info(filter.sharedMesh);
            renderer.GetSharedMaterials(materials);
            if (!info.Fits || materials.Count != info.SubMeshes || !SettingsFit(renderer))
            {
                return Verdict.Never;
            }
            for (int i = 0; i < materials.Count; i++)
            {
                if (!Fits(materials[i]))
                {
                    return Verdict.Never;
                }
            }
            return renderer.localToWorldMatrix.determinant > 0f ? Verdict.Combine : Verdict.Never;
        }

        public static MeshInfo Info(Mesh mesh)
        {
            int id = mesh.GetInstanceID();
            if (MeshInfos.TryGetValue(id, out MeshInfo info))
            {
                return info;
            }
            info = new MeshInfo
            {
                Fits = mesh.isReadable && AllTriangles(mesh),
                Attributes = AttributeBits(mesh),
                Vertices = mesh.vertexCount,
                SubMeshes = mesh.subMeshCount
            };
            MeshInfos[id] = info;
            return info;
        }

        /// <summary>Layer, shadow casting, shadow receiving, light and reflection probes, motion vectors and dynamic
        /// occlusion, packed: renderers that differ in any of these are drawn apart.</summary>
        public static int LookBits(Renderer renderer)
        {
            int bits = renderer.gameObject.layer;
            bits |= (int)renderer.shadowCastingMode << 5;
            bits |= (renderer.receiveShadows ? 1 : 0) << 7;
            bits |= (int)renderer.lightProbeUsage << 8;
            bits |= (int)renderer.reflectionProbeUsage << 11;
            bits |= (int)renderer.motionVectorGenerationMode << 13;
            bits |= (renderer.allowOcclusionWhenDynamic ? 1 : 0) << 15;
            return bits;
        }

        /// <summary>Gives a combined renderer the settings packed by <see cref="LookBits"/>.</summary>
        public static void Apply(MeshRenderer renderer, int bits)
        {
            renderer.gameObject.layer = bits & 31;
            renderer.shadowCastingMode = (ShadowCastingMode)((bits >> 5) & 3);
            renderer.receiveShadows = ((bits >> 7) & 1) != 0;
            renderer.lightProbeUsage = (LightProbeUsage)((bits >> 8) & 7);
            renderer.reflectionProbeUsage = (ReflectionProbeUsage)((bits >> 11) & 3);
            renderer.motionVectorGenerationMode = (MotionVectorGenerationMode)((bits >> 13) & 3);
            renderer.allowOcclusionWhenDynamic = ((bits >> 15) & 1) != 0;
        }

        public static void Clear()
        {
            MaterialFits.Clear();
            MeshInfos.Clear();
        }

        // A light probe proxy volume or custom probe data belongs to the renderer and would not carry over.
        private static bool SettingsFit(Renderer renderer)
        {
            LightProbeUsage probes = renderer.lightProbeUsage;
            return probes != LightProbeUsage.UseProxyVolume && probes != LightProbeUsage.CustomProvided;
        }

        private static bool Fits(Material material)
        {
            if (material == null)
            {
                return false;
            }
            int id = material.GetInstanceID();
            if (!MaterialFits.TryGetValue(id, out bool fits))
            {
                fits = Decide(material);
                MaterialFits[id] = fits;
            }
            return fits;
        }

        private static bool Decide(Material material)
        {
            Shader shader = material.shader;
            if (shader == null || !Shaders.Contains(shader.name) || material.renderQueue > MaxOpaqueQueue)
            {
                return false;
            }
            return Off(material, MoveableObject) && Off(material, TriplanarLocal);
        }

        private static bool Off(Material material, int property)
        {
            return !material.HasProperty(property) || material.GetFloat(property) == 0f;
        }

        private static bool AllTriangles(Mesh mesh)
        {
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                if (mesh.GetTopology(i) != MeshTopology.Triangles)
                {
                    return false;
                }
            }
            return true;
        }

        private static int AttributeBits(Mesh mesh)
        {
            int bits = 0;
            for (int i = 0; i < Channels.Length; i++)
            {
                if (mesh.HasVertexAttribute(Channels[i]))
                {
                    bits |= 1 << i;
                }
            }
            return bits;
        }
    }
}
