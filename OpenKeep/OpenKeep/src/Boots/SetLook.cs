using System;
using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

namespace OpenKeep.Boots
{
    /// <summary>
    /// The game's own materials for one set's split pieces, matched by the bundle placeholders' names (the workshop's
    /// material keys): a plain name is the leggings' own material of that name (their skinned renderers), or another game
    /// item's (<see cref="BorrowedLooks"/>); a <c>_Body</c>
    /// name is the leggings' body paint (<c>m_armorMaterial</c>'s <c>_LegsTex</c> and its normal and metal maps), which the
    /// workshop laid onto fitted shells over the body, on a cut-out copy of the set's material; the coverage cloth keeps
    /// the bundle's own material (<see cref="LiningLook"/>); a placeholder with a
    /// texture of its own (the workshop's rag shoes and leather soles) is dressed in the set's material. Sets with no
    /// material of their own (leather, troll leather, rags) borrow the iron greaves'. One per set, shared by its pants and
    /// boots, so each game material is copied once.
    /// </summary>
    public sealed class SetLook
    {
        private const string BodySuffix = "_Body";
        private const float Cutoff = 0.5f;
        private const float Gloss = 0.1f;

        private readonly Dictionary<string, Material> natives;
        private readonly Material basis;
        private readonly Material bodyPaint;
        private readonly Dictionary<string, Material> made = new Dictionary<string, Material>();

        public SetLook(GameObject legs, Material bodyPaint, GameObject fallbackLegs, Dictionary<string, Material> borrowed)
        {
            natives = Natives(legs);
            basis = First(natives) ?? First(Natives(fallbackLegs));
            foreach (KeyValuePair<string, Material> entry in borrowed)
                natives[entry.Key] = entry.Value;
            this.bodyPaint = bodyPaint;
        }

        /// <summary>Puts the game materials on a copy of a bundle piece; false when there is no game material to use.</summary>
        public bool Wear(GameObject piece)
        {
            if (basis == null)
                return false;
            foreach (Renderer renderer in piece.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    materials[i] = Pick(materials[i]);
                renderer.sharedMaterials = materials;
            }
            return true;
        }

        private Material Pick(Material placeholder)
        {
            string name = placeholder != null ? placeholder.name.Replace(" (Instance)", "") : "";
            if (!made.TryGetValue(name, out Material material))
                made[name] = material = Make(name, placeholder);
            return material;
        }

        private Material Make(string name, Material placeholder)
        {
            if (LiningLook.Is(placeholder))
                return LiningLook.For(placeholder, basis);
            if (natives.TryGetValue(name, out Material native))
                return native;
            if (name.EndsWith(BodySuffix, StringComparison.Ordinal))
                return bodyPaint != null ? Painted(name) : basis;
            if (placeholder != null && placeholder.mainTexture != null)
                return CutOut(GameMaterials.Plain(GameMaterials.Dress(basis, placeholder), Gloss));
            Plugin.Log.LogWarning($"OpenKeep: no game material named {name} for the split leggings; the set's own is used");
            return basis;
        }

        /// <summary>The leggings' own materials by name, from every skinned renderer under the prefab.</summary>
        private static Dictionary<string, Material> Natives(GameObject legs)
        {
            var found = new Dictionary<string, Material>();
            if (legs == null)
                return found;
            foreach (SkinnedMeshRenderer renderer in legs.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null && !found.ContainsKey(material.name))
                        found[material.name] = material;
                }
            }
            return found;
        }

        private static Material First(Dictionary<string, Material> found)
        {
            foreach (Material material in found.Values)
                return material;
            return null;
        }

        /// <summary>The body paint's leg maps on a copy of the set's material, cut out where the paint is clear.</summary>
        private Material Painted(string name)
        {
            var material = new Material(basis) { name = name, color = Color.white };
            material.mainTexture = Map(bodyPaint, "_LegsTex");
            material.mainTextureScale = Vector2.one;
            material.mainTextureOffset = Vector2.zero;
            SetMap(material, "_BumpMap", Map(bodyPaint, "_LegsBumpMap"));
            SetMap(material, "_MetallicGlossMap", Map(bodyPaint, "_LegsMetal"));
            SetMap(material, "_EmissionMap", null);
            material.DisableKeyword("_EMISSION");
            return CutOut(material);
        }

        private static Material CutOut(Material material)
        {
            if (material.HasProperty("_Cutoff"))
                material.SetFloat("_Cutoff", Cutoff);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", 0f);
            return material;
        }

        private static Texture Map(Material material, string property) =>
            material.HasProperty(property) ? material.GetTexture(property) : null;

        private static void SetMap(Material material, string property, Texture texture)
        {
            if (material.HasProperty(property))
                material.SetTexture(property, texture);
        }
    }
}
