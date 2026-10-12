using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

using Object = UnityEngine.Object;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Puts a workshop held model (the wand, the focus) where the game item's model was: under its <c>attach</c> child,
    /// which the game draws in the hand, on the back, on an item stand and on the ground. The game's model and collider
    /// go; the upgrade glow and the equipped marker stay, the glow reshaped to the new model. The workshop builds the model
    /// on fixed axes with a <c>grip</c> point where the fist closes; it is turned the way the game holds its kind
    /// (<see cref="InFist"/> for the wand, <see cref="OnHand"/> for a buckler-like focus) and moved so the grip sits on
    /// the attach point. Mesh parts wear the game item's material with
    /// the bake; <c>glow*</c> parts keep the bundle's own emissive material.
    /// </summary>
    public static class HeldModel
    {
        private const string Holder = "attach";
        private const string Grip = "grip";
        private const float Gloss = 0.1f;
        private static readonly string[] Keep = { "UpgraderGlow", "equiped" };
        /// <summary>A wand or anything gripped like the game's club and torch: length +Y laid along the attach's +Z.</summary>
        public static readonly Quaternion InFist = Quaternion.Euler(90f, 0f, 0f);

        /// <summary>
        /// A hand-ward held like the game's bucklers (front +Z, up +Y in the workshop): the face out from the arm, turned 29
        /// degrees towards the attach's +X, the top up the forearm (the Bronze and Iron Buckler, read in game 2026-10-10,
        /// as the shields lay their own).
        /// </summary>
        public static readonly Quaternion OnHand =
            Quaternion.LookRotation(new Vector3(0.490f, -0.870f, -0.055f), new Vector3(0.012f, -0.056f, 0.998f));

        public static void Wear(GameObject item, GameObject game, GameObject model, Quaternion hold)
        {
            Transform holder = item.transform.Find(Holder);
            if (holder == null || model == null)
            {
                Plugin.Log.LogWarning($"EliteEquipment: {item.name} keeps the {game.name} look (no attach or no workshop model)");
                return;
            }
            Clear(holder);
            GameObject copy = Object.Instantiate(model, holder, false);
            copy.name = "model";
            foreach (Collider collider in copy.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            foreach (Transform part in copy.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = holder.gameObject.layer;
            Dress(copy, GameMaterials.Borrow(game, Holder));
            Place(copy.transform, hold, item.name);
            Bounds bounds = ModelBounds.In(copy, holder);
            Collide(holder, bounds);
            Glow(holder, bounds);
        }

        /// <summary>
        /// The workshop model's <c>tip</c> point (where spells leave a wand) under an equipped attach, or null. Looked for
        /// inside the model only: the game item's kept <c>equiped</c> marker holds its melee trail, whose own <c>tip</c>
        /// is about a metre out (the Club's swing, found first until 2026-10-11).
        /// </summary>
        public static Transform Tip(Transform attach)
        {
            Transform model = attach != null ? attach.Find("model") : null;
            return model != null ? GameMaterials.Find(model, "tip") : null;
        }

        private static void Clear(Transform holder)
        {
            for (int i = holder.childCount - 1; i >= 0; i--)
            {
                Transform child = holder.GetChild(i);
                if (System.Array.IndexOf(Keep, child.name) < 0)
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void Dress(GameObject model, Material game)
        {
            var dressed = new Dictionary<Material, Material>();
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Material placeholder = renderer.sharedMaterial;
                if (placeholder == null)
                    continue;
                if (!dressed.TryGetValue(placeholder, out Material material))
                {
                    material = Look(renderer, placeholder, game);
                    dressed[placeholder] = material;
                }
                renderer.sharedMaterial = material;
            }
        }

        private static Material Look(Renderer renderer, Material placeholder, Material game)
        {
            if (renderer.name.StartsWith("glow") || game == null)
            {
                var glow = new Material(placeholder);
                glow.EnableKeyword("_EMISSION");
                return glow;
            }
            return GameMaterials.Plain(GameMaterials.Dress(game, placeholder), Gloss);
        }

        /// <summary>The grip on the attach point; without a grip point the model's base is.</summary>
        private static void Place(Transform model, Quaternion hold, string name)
        {
            model.localRotation = hold;
            model.localScale = Vector3.one;
            Transform grip = model.Find(Grip);
            if (grip == null)
                Plugin.Log.LogWarning($"EliteEquipment: {name}'s model has no {Grip} point; it is held by its origin");
            model.localPosition = grip != null ? -(hold * grip.localPosition) : Vector3.zero;
        }

        private static void Collide(Transform holder, Bounds bounds)
        {
            var go = new GameObject("collider");
            go.transform.SetParent(holder, false);
            go.layer = holder.gameObject.layer;
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = Vector3.Max(bounds.size, Vector3.one * 0.05f);
        }

        private static void Glow(Transform holder, Bounds bounds)
        {
            Transform glow = holder.Find("UpgraderGlow");
            if (glow == null)
                return;
            foreach (ParticleSystem particles in glow.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.ShapeModule shape = particles.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.position = bounds.center;
                shape.rotation = Vector3.zero;
                shape.scale = bounds.size;
            }
        }
    }
}
