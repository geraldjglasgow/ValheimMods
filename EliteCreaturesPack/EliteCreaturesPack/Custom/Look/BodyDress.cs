using UnityEngine;

namespace EliteCreaturesPack.Custom.Look
{
    /// <summary>
    /// A body's tint and texture, put on its prefab once (on a machine that draws): every material of every renderer it
    /// is drawn with (particles, lines and trails aside), the materials it may switch to (<c>MaterialVariation</c>, the Hen's
    /// colours) and, for the player's body, the base material of each body model (<c>VisEquipment.m_models</c>, whose main
    /// texture the game puts back whenever the body model is set). The tint replaces each material's tint colour; the
    /// texture replaces the body texture wherever a material draws it. Shared copies from <see cref="MaterialRecipes"/>,
    /// so every creature of the prefab draws with the same few materials. The game's star looks copy these materials and
    /// turn their hue on top (<c>LevelEffects</c>), as do Elite Creatures Reborn's, so a tinted creature's stars still show.
    /// </summary>
    internal static class BodyDress
    {
        private const string MainTexture = "_MainTex";

        /// <summary>
        /// The main renderer of a body: the player's body model, or the one the game recolours for star levels
        /// (<c>LevelEffects.m_mainRender</c>), or else its skinned mesh with the most vertices.
        /// </summary>
        public static Renderer? MainRenderer(GameObject body)
        {
            VisEquipment? vis = body.GetComponent<VisEquipment>();
            if (vis != null && vis.m_bodyModel != null && vis.m_models.Length > 0)
            {
                return vis.m_bodyModel;
            }
            foreach (LevelEffects effects in body.GetComponentsInChildren<LevelEffects>(true))
            {
                if (effects.m_mainRender != null)
                {
                    return effects.m_mainRender;
                }
            }
            return Largest(body);
        }

        /// <summary>"The body texture": the main texture of the main renderer's first material, or null.</summary>
        public static Texture? BodyTexture(GameObject body)
        {
            Material? first = FirstMaterial(MainRenderer(body));
            return first != null && first.HasProperty(MainTexture) ? first.GetTexture(MainTexture) : null;
        }

        /// <summary>Whether a tint can colour the main renderer's first material (the player's body cannot).</summary>
        public static bool BodyTakesTint(GameObject body) => MaterialRecipes.CanTint(FirstMaterial(MainRenderer(body)));

        public static void Dress(GameObject body, Color? tint, Texture? from, Texture? to)
        {
            foreach (Renderer renderer in body.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is ParticleSystemRenderer) && !(renderer is LineRenderer) && !(renderer is TrailRenderer))
                {
                    renderer.sharedMaterials = Dressed(renderer.sharedMaterials, tint, from, to);
                }
            }
            foreach (MaterialVariation variation in body.GetComponentsInChildren<MaterialVariation>(true))
            {
                foreach (MaterialVariation.MaterialEntry entry in variation.m_materials)
                {
                    entry.m_material = entry.m_material != null ? MaterialRecipes.Dressed(entry.m_material, tint, from, to) : entry.m_material;
                }
            }
            Models(body, tint, to);
        }

        /// <summary>The player's body models: each model's own skin texture is the body texture.</summary>
        private static void Models(GameObject body, Color? tint, Texture? to)
        {
            VisEquipment? vis = body.GetComponent<VisEquipment>();
            foreach (VisEquipment.PlayerModel model in vis != null ? vis.m_models : new VisEquipment.PlayerModel[0])
            {
                Material? material = model.m_baseMaterial;
                if (material != null)
                {
                    Texture? own = material.HasProperty(MainTexture) ? material.GetTexture(MainTexture) : null;
                    model.m_baseMaterial = MaterialRecipes.Dressed(material, tint, own, to);
                }
            }
        }

        private static Material[] Dressed(Material[] materials, Color? tint, Texture? from, Texture? to)
        {
            Material[] dressed = new Material[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                dressed[i] = materials[i] != null ? MaterialRecipes.Dressed(materials[i], tint, from, to) : materials[i];
            }
            return dressed;
        }

        private static Material? FirstMaterial(Renderer? renderer) =>
            renderer != null && renderer.sharedMaterials.Length > 0 ? renderer.sharedMaterials[0] : null;

        private static Renderer? Largest(GameObject body)
        {
            SkinnedMeshRenderer? best = null;
            foreach (SkinnedMeshRenderer renderer in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMesh != null && (best == null || renderer.sharedMesh.vertexCount > best.sharedMesh.vertexCount))
                {
                    best = renderer;
                }
            }
            return best;
        }
    }
}
