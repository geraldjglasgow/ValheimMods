using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevBridge.Stage
{
    /// <summary>
    /// Puts a bundle model's placeholder materials into the game's own, the way ValheimModLibs' GameMaterials does in the
    /// mods: a copy of a game material (its shader, lighting and settings) wearing the placeholder's baked albedo and
    /// normal map (Dress), optionally without the game maps laid out for the game model's UVs (Plain). Every dress starts
    /// from the materials the model had when placed, so dresses can be tried one after another.
    /// </summary>
    internal static class Dress
    {
        // The shorthands: a game shader, the prefabs whose body material is the one to copy first, and the gloss Plain gets.
        private static readonly Dictionary<string, (string Shader, string[] Prefabs, float Gloss)> Shorthands =
            new Dictionary<string, (string, string[], float)>(StringComparer.OrdinalIgnoreCase)
            {
                ["Creature"] = ("Custom/Creature", new[] { "Skeleton", "Greydwarf", "Boar" }, 0.1f),
                ["Piece"] = ("Custom/Piece", new[] { "piece_chest_wood", "wood_wall", "woodwall" }, 0.1f),
            };

        private static readonly Dictionary<string, Material> ByShaderCache = new Dictionary<string, Material>();

        /// <summary>Back to the originals, then every dress in order.</summary>
        internal static void Apply(Placement placement)
        {
            Restore(placement);
            foreach (DressSpec spec in placement.Dresses.Where(s => !s.Undress)) Put(placement, spec);
        }

        /// <summary>Adds a dress (replacing one with the same `only` filter) and applies them all; "none" clears them.</summary>
        internal static void Add(Placement placement, DressSpec spec)
        {
            if (spec.Undress && spec.Only == null) placement.Dresses.Clear();
            placement.Dresses.RemoveAll(s => string.Equals(s.Only, spec.Only, StringComparison.OrdinalIgnoreCase));
            placement.Dresses.Add(spec);
            Apply(placement);
        }

        /// <summary>A dress spec from with=/dress=, child=, only= and plain= (a gloss, or off).</summary>
        internal static DressSpec Spec(BridgeRequest request, string with)
        {
            string plain = request.Get("plain");
            float? gloss = Shorthands.TryGetValue(with, out var shorthand) ? shorthand.Gloss : (float?)null;
            if (plain == "off") gloss = null;
            else if (plain != null) gloss = request.Float("plain", 0.1f);
            return new DressSpec { With = with, Child = request.Get("child"), Only = request.Get("only"), Gloss = gloss };
        }

        private static void Restore(Placement placement)
        {
            foreach (KeyValuePair<Renderer, Material[]> pair in placement.Originals.Where(p => p.Key))
                pair.Key.sharedMaterials = pair.Value;
            foreach (Material material in placement.Made.Where(m => m)) Object.Destroy(material);
            placement.Made.Clear();
        }

        private static void Put(Placement placement, DressSpec spec)
        {
            Material game = Source(spec.With, spec.Child);
            var dressed = new Dictionary<Material, Material>();
            foreach (KeyValuePair<Renderer, Material[]> pair in placement.Originals.Where(p => p.Key))
            {
                Material[] current = pair.Key.sharedMaterials;
                for (int i = 0; i < pair.Value.Length && i < current.Length; i++)
                {
                    Material placeholder = pair.Value[i];
                    if (!placeholder || !Matches(placeholder, spec.Only)) continue;
                    if (!dressed.TryGetValue(placeholder, out Material material))
                        dressed[placeholder] = material = Made(placement, Dressed(game, placeholder, spec.Gloss));
                    current[i] = material;
                }
                pair.Key.sharedMaterials = current;
            }
        }

        private static bool Matches(Material placeholder, string only) =>
            only == null || placeholder.name.IndexOf(only, StringComparison.OrdinalIgnoreCase) >= 0;

        private static Material Made(Placement placement, Material material)
        {
            placement.Made.Add(material);
            return material;
        }

        /// <summary>
        /// The game material a dress names: Creature or Piece (the game's creature and building shaders, as worn by a
        /// Skeleton and a wood chest), or any game prefab's: the renderer named child=, else its largest skinned mesh
        /// (a creature's body), else its first renderer.
        /// </summary>
        internal static Material Source(string with, string child)
        {
            if (Shorthands.TryGetValue(with, out var shorthand)) return ByShader(shorthand.Shader, shorthand.Prefabs);
            GameObject prefab = GamePrefabs.Require(with);
            Renderer renderer = child != null ? Named(prefab, child) : Body(prefab);
            return renderer && renderer.sharedMaterial ? renderer.sharedMaterial
                : throw new BridgeException($"{with} has no {(child != null ? "renderer " + child : "renderer")} with a material to borrow");
        }

        private static Renderer Named(GameObject prefab, string child) =>
            prefab.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name == child)
            ?? prefab.GetComponentsInChildren<Transform>(true).Where(t => t.name == child).Select(t => t.GetComponentInChildren<Renderer>(true)).FirstOrDefault(r => r)
            ?? throw new BridgeException($"{prefab.name} has no renderer under a child named {child}");

        private static Renderer Body(GameObject prefab)
        {
            SkinnedMeshRenderer body = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .OrderByDescending(r => r.sharedMesh ? r.sharedMesh.vertexCount : 0).FirstOrDefault();
            return body ? body : prefab.GetComponentInChildren<Renderer>(true);
        }

        private static Material ByShader(string shader, string[] prefabs)
        {
            if (ByShaderCache.TryGetValue(shader, out Material cached) && cached) return cached;
            IEnumerable<GameObject> candidates = prefabs.Select(GamePrefabs.Find).Where(p => p).Concat(GamePrefabs.All());
            Material found = candidates.Select(p => Wearing(p, shader)).FirstOrDefault(m => m)
                ?? throw new BridgeException($"no game prefab wears the shader {shader}");
            return ByShaderCache[shader] = found;
        }

        private static Material Wearing(GameObject prefab, string shader)
        {
            Renderer body = Body(prefab);
            if (body && body.sharedMaterial && body.sharedMaterial.shader.name == shader) return body.sharedMaterial;
            return prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).FirstOrDefault(m => m && m.shader.name == shader);
        }

        /// <summary>GameMaterials.Dress, then GameMaterials.Plain when a gloss is given.</summary>
        internal static Material Dressed(Material game, Material placeholder, float? gloss)
        {
            var material = new Material(game) { name = placeholder.name, color = Color.white };
            if (material.HasProperty("_MainTex"))
            {
                material.mainTexture = placeholder.HasProperty("_MainTex") ? placeholder.mainTexture : null;
                material.mainTextureScale = Vector2.one;
                material.mainTextureOffset = Vector2.zero;
            }
            if (material.HasProperty("_BumpMap"))
                material.SetTexture("_BumpMap", placeholder.HasProperty("_BumpMap") ? placeholder.GetTexture("_BumpMap") : null);
            if (gloss.HasValue) Plain(material, gloss.Value);
            return material;
        }

        // GameMaterials.Plain, plus the building shader's metal mask (_MetallicTex), which it names differently.
        private static void Plain(Material material, float gloss)
        {
            foreach (string map in new[] { "_MetallicGlossMap", "_MetallicTex", "_EmissionMap", "_StyleTex" })
                if (material.HasProperty(map)) material.SetTexture(map, null);
            foreach (KeyValuePair<string, float> value in new Dictionary<string, float> { ["_Metallic"] = 0f, ["_MetalGloss"] = 0f, ["_Glossiness"] = gloss })
                if (material.HasProperty(value.Key)) material.SetFloat(value.Key, value.Value);
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", Color.black);
            material.DisableKeyword("_EMISSION");
        }
    }
}
