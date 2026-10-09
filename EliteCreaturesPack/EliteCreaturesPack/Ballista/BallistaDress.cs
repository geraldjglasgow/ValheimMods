using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// The bundle's models in the game ballista's own body material (`piece_turret`'s renderer `Base`: `Turret_mat`,
    /// `Custom/Piece`) wearing our baked textures, so the bone is lit like the game's pieces; one dressed material per
    /// placeholder, shared by every copy. The game's gloss and style maps, laid out for its own model, come off.
    /// </summary>
    public static class BallistaDress
    {
        private const float Gloss = 0.1f;

        private static readonly Dictionary<Material, Material> dressed = new Dictionary<Material, Material>();

        /// <summary>Every renderer of `model` in `game` (the piece's body material) with its own textures; unchanged without one.</summary>
        public static void Dress(GameObject model, Material? game)
        {
            if (game == null)
            {
                return;
            }
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = For(materials[i], game);
                }
                renderer.sharedMaterials = materials;
            }
        }

        private static Material For(Material placeholder, Material game)
        {
            if (placeholder == null || dressed.ContainsValue(placeholder))
            {
                return placeholder!;
            }
            if (!dressed.TryGetValue(placeholder, out Material made))
            {
                made = dressed[placeholder] = GameMaterials.Plain(GameMaterials.Dress(game, placeholder), Gloss);
            }
            return made;
        }

        /// <summary>Every transform of `part` on `layer`.</summary>
        public static void Layer(GameObject part, int layer)
        {
            foreach (Transform child in part.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
            }
        }
    }
}
