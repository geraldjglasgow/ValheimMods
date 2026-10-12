using BundlePrefabs;
using UnityEngine;

using Object = UnityEngine.Object;

namespace EliteEquipment.Shields
{
    /// <summary>
    /// Puts a workshop shield where the game item's model was: under its <c>attach</c> child, which the game draws in the
    /// hand, on the back, on an item stand and on the ground. The game's model (and its collider) goes; the workshop's
    /// comes with its own colliders, on the attach child's layer, in the game item's own material wearing the bake. The
    /// workshop builds a model front +Z, up +Y with a <c>grip</c> point where the fist holds it; it is turned as the
    /// game's shields of its kind lie in the hand (<see cref="ShieldKind.Hold"/>) and moved so the grip sits on the
    /// attach point.
    /// </summary>
    public static class ShieldModel
    {
        private const string Holder = "attach";
        private const string ModelName = "model";
        private const string Grip = "grip";


        /// <summary>
        /// The model in place of the game item's, in <paramref name="look"/>'s own material when the set names one (its
        /// UVs lie on that item's atlas), otherwise in <paramref name="game"/>'s material wearing the model's bake.
        /// </summary>
        public static void Wear(GameObject item, GameObject game, GameObject look, GameObject model, ShieldSet set)
        {
            Transform holder = item.transform.Find(Holder) ?? item.transform;
            Material material = look != null ? GameLook(look) : Look(game, model, set);
            for (int i = holder.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(holder.GetChild(i).gameObject);
            GameObject copy = Object.Instantiate(model, holder, false);
            copy.name = ModelName;
            if (material != null)
                GameMaterials.Apply(copy, placeholder => material);
            foreach (Transform part in copy.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = holder.gameObject.layer;
            Place(copy.transform, holder, item.name, set.Kind.Hold);
        }

        /// <summary>
        /// Turned as the game's shields of its kind lie in the hand, the grip on the attach point; without a grip point the
        /// model's middle is.
        /// </summary>
        private static void Place(Transform model, Transform holder, string name, Quaternion hold)
        {
            model.localRotation = hold;
            model.localScale = Vector3.one;
            Transform grip = model.Find(Grip);
            if (grip != null)
            {
                model.localPosition = -(hold * grip.localPosition);
                return;
            }
            Plugin.Log.LogWarning($"EliteEquipment: {name}'s model has no {Grip} point; it is held by its middle");
            model.localPosition = Vector3.zero;
            model.localPosition = -ModelBounds.In(model.gameObject, holder).center;
        }

        /// <summary>The game item's own material as it is (texture, normal map, metal mask, gloss), its painted styles off.</summary>
        private static Material GameLook(GameObject look)
        {
            Material borrowed = GameMaterials.Borrow(look, Holder);
            return borrowed != null ? GameMaterials.Unstyled(borrowed) : null;
        }

        /// <summary>
        /// The game item's material wearing the model's baked albedo and normal map, without the game's own maps, and
        /// shining where the model's metal mask says it is metal (as the biome's shield does).
        /// </summary>
        private static Material Look(GameObject game, GameObject model, ShieldSet set)
        {
            Material borrowed = GameMaterials.Borrow(game, Holder);
            Renderer renderer = model.GetComponentInChildren<Renderer>(true);
            if (borrowed == null || renderer == null || renderer.sharedMaterial == null)
                return null;
            Material placeholder = renderer.sharedMaterial;
            Material plain = GameMaterials.Plain(GameMaterials.Dress(borrowed, placeholder), set.Gloss);
            return GameMaterials.Metal(plain, placeholder, set.MetalGloss);
        }
    }
}
