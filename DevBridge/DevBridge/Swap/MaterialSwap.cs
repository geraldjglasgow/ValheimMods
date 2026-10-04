using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Stage;
using UnityEngine;

namespace DevBridge.Swap
{
    /// <summary>
    /// The materials a swapped renderer draws with. By default each slot keeps the live material (the mod's dressed game
    /// shader) as a copy wearing the bundle material's textures, those both shaders have and the bundle sets (its baked
    /// albedo and normal map, as GameMaterials.Dress takes them); one copy per live and bundle material, shared by every
    /// renderer and copy of the prefab. materials=replace puts the bundle's own materials on; keep leaves them.
    /// </summary>
    internal static class MaterialSwap
    {
        /// <summary>Dresses one renderer after its mesh was swapped; returns what its materials got, for the reply.</summary>
        internal static string Put(SwapEntry entry, Renderer live, Renderer bundle)
        {
            Material[] theirs = bundle.sharedMaterials;
            if (entry.Materials == MaterialMode.Keep || theirs.Length == 0) return "materials kept";
            if (entry.Materials == MaterialMode.Replace) return Replace(entry, live, theirs);
            Material[] current = live.sharedMaterials;
            var copied = new SortedSet<string>(StringComparer.Ordinal);
            var result = new Material[theirs.Length];
            for (int i = 0; i < theirs.Length; i++)
            {
                Material basis = current.Length == 0 ? null : Source(entry, current[Math.Min(i, current.Length - 1)]);
                result[i] = basis ? Textured(entry, basis, theirs[i], copied) : Replaced(entry, theirs[i]);
            }
            live.sharedMaterials = result;
            return copied.Count == 0 ? "no texture to copy, materials kept" : "textures " + string.Join(", ", copied);
        }

        private static string Replace(SwapEntry entry, Renderer live, Material[] theirs)
        {
            live.sharedMaterials = theirs.Select(m => Replaced(entry, m)).ToArray();
            return "the bundle's materials";
        }

        private static Material Replaced(SwapEntry entry, Material material)
        {
            if (!material || !entry.Placed.Add(material)) return material;
            foreach (string name in material.GetTexturePropertyNames())
            {
                Texture texture = material.GetTexture(name);
                if (texture) entry.Textures.Add(texture);
            }
            return material;
        }

        /// <summary>A copy of ours stands for the material it was copied from (a swap applied again starts from the live one).</summary>
        private static Material Source(SwapEntry entry, Material material) =>
            material && entry.Made.TryGetValue(material, out Material source) ? source : material;

        private static Material Textured(SwapEntry entry, Material basis, Material theirs, SortedSet<string> copied)
        {
            List<string> names = theirs ? Shared(basis, theirs) : new List<string>();
            if (names.Count == 0) return basis;
            copied.UnionWith(names);
            // A copy the game made of ours (a ragdoll's or a worn item's own material, the build ghost's) wears them already.
            if (names.All(n => basis.GetTexture(n) == theirs.GetTexture(n))) return basis;
            if (entry.Copies.TryGetValue((basis, theirs), out Material made) && made) return made;
            made = new Material(basis) { name = basis.name };
            foreach (string name in names)
            {
                made.SetTexture(name, theirs.GetTexture(name));
                made.SetTextureScale(name, theirs.GetTextureScale(name));
                made.SetTextureOffset(name, theirs.GetTextureOffset(name));
                entry.Textures.Add(theirs.GetTexture(name));
            }
            entry.Copies[(basis, theirs)] = made;
            entry.Made[made] = basis;
            return made;
        }

        /// <summary>The texture slots the bundle material fills that the live material's shader also has.</summary>
        private static List<string> Shared(Material basis, Material theirs) =>
            theirs.GetTexturePropertyNames().Where(n => theirs.GetTexture(n) && basis.HasTexture(n)).Distinct().ToList();

        /// <summary>
        /// The renderer's materials back: each of ours for what it replaced (the recorded original slot for a bundle
        /// material), then any material the game copied from ours cleared of the bundle's textures.
        /// </summary>
        internal static void Restore(SwapEntry entry, Renderer renderer, Material[] original)
        {
            Material[] current = renderer.sharedMaterials;
            bool resized = original != null && current.Length != original.Length;
            Material[] result = resized ? (Material[])original.Clone() : current.Select((m, i) => Back(entry, m, original, i)).ToArray();
            if (resized || !result.SequenceEqual(current)) renderer.sharedMaterials = result;
            foreach (Material material in result) Untexture(entry, material);
        }

        private static Material Back(SwapEntry entry, Material material, Material[] original, int slot)
        {
            if (!material) return material;
            if (entry.Made.TryGetValue(material, out Material source)) return source;
            bool ours = entry.Placed.Contains(material) || (entry.Materials == MaterialMode.Replace && Carries(entry, material));
            return ours && original != null && slot < original.Length ? original[slot] : material;
        }

        /// <summary>Whether a material holds one of the bundle's textures the swap handed out.</summary>
        internal static bool Carries(SwapEntry entry, Material material) =>
            material.GetTexturePropertyNames().Any(n => Handed(entry, material.GetTexture(n)));

        private static bool Handed(SwapEntry entry, Texture texture) => texture && entry.Textures.Contains(texture);

        /// <summary>
        /// A material the swap did not make but that holds its textures - a copy the game made of one of ours, such as a
        /// starred creature's tinted body - gets back the textures of the material ours was copied from.
        /// </summary>
        internal static void Untexture(SwapEntry entry, Material material)
        {
            if (!material || entry.Made.ContainsKey(material) || entry.Placed.Contains(material)) return;
            foreach (string name in material.GetTexturePropertyNames().Where(n => Handed(entry, material.GetTexture(n))).ToList())
            {
                Material source = SourceOf(entry, material, name);
                material.SetTexture(name, source ? source.GetTexture(name) : null);
                if (!source) continue;
                material.SetTextureScale(name, source.GetTextureScale(name));
                material.SetTextureOffset(name, source.GetTextureOffset(name));
            }
        }

        /// <summary>
        /// The material whose texture in that slot goes back on a copy the game made of ours: among our copies wearing
        /// the same bundle texture there, one made from a material that wears none of the bundle's, the one named like
        /// the game's copy first (a body and its eyes, or two levels of detail, can share one bundle atlas).
        /// </summary>
        private static Material SourceOf(SwapEntry entry, Material material, string name)
        {
            Texture texture = material.GetTexture(name);
            string plain = material.name.Replace(" (Instance)", "");
            List<Material> sources = entry.Made.Where(p => p.Key && p.Value && p.Key.HasTexture(name) && p.Key.GetTexture(name) == texture && !Carries(entry, p.Value))
                .Select(p => p.Value).ToList();
            return sources.FirstOrDefault(s => s.name == plain) ?? sources.FirstOrDefault();
        }

        /// <summary>
        /// LevelEffects keeps one body material per creature and star level for every later spawn. One the game copied
        /// from a swapped body loses the bundle's textures; one copied from a bundle material (replace) is dropped, so
        /// the next starred spawn copies the restored body afresh.
        /// </summary>
        internal static void CleanLevelCache(SwapEntry entry)
        {
            foreach (KeyValuePair<string, Material> pair in LevelEffects.m_materials.Where(p => p.Value && Carries(entry, p.Value)).ToList())
            {
                if (entry.Materials == MaterialMode.Replace) LevelEffects.m_materials.Remove(pair.Key);
                else Untexture(entry, pair.Value);
            }
        }

        /// <summary>
        /// The stage keeps each placement's materials as the originals every dress starts from. A copy of ours among them
        /// (a still copy made of the swapped prefab) becomes the material it was copied from, so no dress puts a destroyed
        /// one back. Bundle materials (replace) stay: an asset placed from the same bundle holds those as its own.
        /// </summary>
        internal static void Unstage(SwapEntry entry)
        {
            foreach (Placement placement in Placements.All)
            {
                foreach (Renderer renderer in placement.Originals.Keys.ToList())
                    placement.Originals[renderer] = placement.Originals[renderer].Select(m => Source(entry, m)).ToArray();
            }
        }
    }
}
