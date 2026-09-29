using System.Collections.Generic;
using System.Linq;
using BundlePrefabs;
using EliteCreaturesPack.Core;
using UnityEngine;

namespace EliteCreaturesPack.Crossbow
{
    /// <summary>
    /// Puts the bundle's kit on the Skeleton copy. The kit's top-level children are mounts named after the Skeleton's
    /// bones (LeftHand, RightHand, Hips); each is parented to its bone with no offset, so every part keeps the offset the
    /// workshop measured on the same skeleton at the game's scale (AssetWorkshop Crossbow/XbowKit). The kit carries its
    /// own bone bolts: one in the groove, one in the right fingers' pinch and one in each quiver slot, which
    /// <see cref="XbowRig"/> shows and hides. Every part wears the Skeleton's own material with the part's baked
    /// textures (<see cref="Dress"/>), so the bone crossbow and bolts are lit and grimed like the skeleton.
    /// </summary>
    public static class XbowKit
    {
        public const string GrooveBolt = "ecp_xbow_bolt_groove";
        public const string HandBolt = "ecp_xbow_bolt_hand";
        public const string QuiverBolt = "ecp_xbow_bolt_quiver_";

        public static void Wear(Transform visual, GameObject kitPrefab, Material skin)
        {
            // Beside the Visual, not in it, so the bone search below never finds the kit's own mounts; under the
            // inactive copy, so nothing wakes.
            GameObject kit = Object.Instantiate(kitPrefab, visual.parent, false);
            Dress(kit, skin);
            foreach (Transform mount in kit.transform.Cast<Transform>().ToArray())
            {
                Hang(mount, visual);
            }
            Object.DestroyImmediate(kit);
            GameMaterials.Find(visual, HandBolt)?.gameObject.SetActive(false);
        }

        /// <summary>The Skeleton's own body material, which the kit's parts copy so they are lit like the creature.</summary>
        public static Material Skin(Transform visual) =>
            visual.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(r => r.bounds.size.sqrMagnitude).First().sharedMaterial;

        /// <summary>Every renderer under `model` onto a copy of the skin material wearing its own baked textures.</summary>
        public static void Dress(GameObject model, Material skin)
        {
            var dressed = new Dictionary<Material, Material>();
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Material placeholder = renderer.sharedMaterial;
                if (!dressed.TryGetValue(placeholder, out Material material))
                {
                    material = GameMaterials.Plain(GameMaterials.Dress(skin, placeholder), 0.1f);
                    dressed[placeholder] = material;
                }
                renderer.sharedMaterial = material;
            }
        }

        private static void Hang(Transform mount, Transform visual)
        {
            Transform? bone = GameMaterials.Find(visual, mount.name);
            if (bone == null)
            {
                Log.Warn($"Skeleton crossbowman: the Skeleton has no {mount.name} bone; what hangs there is left off.");
                Object.DestroyImmediate(mount.gameObject);
                return;
            }
            mount.SetParent(bone, false);
            mount.localPosition = Vector3.zero;
            mount.localRotation = Quaternion.identity;
            mount.localScale = Vector3.one;
        }
    }
}
