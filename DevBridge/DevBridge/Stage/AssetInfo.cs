using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevBridge.Stage
{
    /// <summary>One-line summaries of bundle assets and placed objects: sizes, triangles, textures, materials, clips.</summary>
    internal static class AssetInfo
    {
        internal static string Line(Object asset)
        {
            switch (asset)
            {
                case GameObject prefab: return $"{prefab.name}: {Parts(prefab)}";
                case Texture2D texture: return $"{texture.name}: {texture.width}x{texture.height} {texture.format}, {texture.filterMode} filter";
                case AudioClip clip: return $"{clip.name}: {clip.length:0.00} s, {clip.frequency} Hz, {clip.channels} ch";
                case AnimationClip clip: return $"{clip.name}: {clip.length:0.00} s at {clip.frameRate:0} fps{(clip.isLooping ? ", loops" : "")}{(clip.humanMotion ? ", humanoid" : "")}";
                case Material material: return $"{material.name}: {material.shader.name}{Maps(material)}";
                case Mesh mesh: return $"{mesh.name}: {Triangles(mesh)} triangles, {mesh.vertexCount} vertices";
                case Sprite sprite: return $"{sprite.name}: {sprite.rect.width:0}x{sprite.rect.height:0}";
                case RuntimeAnimatorController controller: return $"{controller.name}: {controller.animationClips.Length} clips";
                default: return asset.name;
            }
        }

        /// <summary>"1210 triangles, 1 renderer, animator, 2 particle systems, 1 sound" for a prefab or placed object.</summary>
        internal static string Parts(GameObject root)
        {
            var parts = new List<string> { $"{Triangles(root)} triangles", Count(root.GetComponentsInChildren<Renderer>(true).Count(r => !(r is ParticleSystemRenderer)), "renderer") };
            if (root.GetComponentInChildren<Animator>(true)) parts.Add("animator");
            int particles = root.GetComponentsInChildren<ParticleSystem>(true).Length;
            if (particles > 0) parts.Add(Count(particles, "particle system"));
            int sounds = root.GetComponentsInChildren<AudioSource>(true).Length;
            if (sounds > 0) parts.Add(Count(sounds, "sound"));
            if (root.GetComponentInChildren<Light>(true)) parts.Add("light");
            return string.Join(", ", parts);
        }

        private static string Count(int n, string what) => $"{n} {what}{(n == 1 ? "" : "s")}";

        /// <summary>Triangles drawn at the nearest level of detail (lower LODs are left out).</summary>
        internal static int Triangles(GameObject root)
        {
            var farther = new HashSet<Renderer>(root.GetComponentsInChildren<LODGroup>(true)
                .SelectMany(g => g.GetLODs().Skip(1)).SelectMany(l => l.renderers).Where(r => r));
            int total = 0;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true).Where(r => !farther.Contains(r)))
            {
                Mesh mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh) total += Triangles(mesh);
            }
            return total;
        }

        internal static int Triangles(Mesh mesh)
        {
            long indices = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
                if (mesh.GetTopology(i) == MeshTopology.Triangles) indices += mesh.GetIndexCount(i);
            return (int)(indices / 3);
        }

        /// <summary>The box round the meshes that are drawn now (particles left out); a 0.4 m box at the root when none are.</summary>
        internal static Bounds Bounds(GameObject root)
        {
            Renderer[] drawn = root.GetComponentsInChildren<Renderer>()
                .Where(r => r.enabled && (r is MeshRenderer || r is SkinnedMeshRenderer)).ToArray();
            if (drawn.Length == 0) return new Bounds(root.transform.position, Vector3.one * 0.4f);
            Bounds box = drawn[0].bounds;
            foreach (Renderer renderer in drawn.Skip(1)) box.Encapsulate(renderer.bounds);
            return box;
        }

        /// <summary>Each distinct material under the root: "name [shader] albedo WxH, normal map".</summary>
        internal static List<string> Materials(GameObject root) =>
            root.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m)
                .Distinct().Select(m => $"{m.name} [{m.shader.name}]{Maps(m)}").ToList();

        private static string Maps(Material material)
        {
            Texture albedo = material.HasProperty("_MainTex") ? material.mainTexture : null;
            bool normal = material.HasProperty("_BumpMap") && material.GetTexture("_BumpMap");
            return (albedo ? $" albedo {albedo.name} {albedo.width}x{albedo.height}" : "") + (normal ? ", normal map" : "");
        }
    }
}
