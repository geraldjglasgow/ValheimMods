using System.Collections.Generic;
using System.Linq;
using BundlePrefabs;
using EliteCreaturesPack.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesPack.Headsman
{
    /// <summary>
    /// What the bundle gives the Crypt Executioner (AssetWorkshop assets/ecp_headsman, HeadsmanBundle): the kit, whose
    /// top-level child is a mount named after the Skeleton's bone it hangs from (RightHand), holding the axe as the right
    /// fist holds it, measured on the game's Skeleton; the axe is its 29 pieces bottom up (<see cref="HeadsmanAxe"/>).
    /// Every part wears the Skeleton's own material with the axe's baked texture, so it is lit and grimed like the bones.
    /// And the animator controller, its stagger slot filled with the Skeleton's own stagger clip.
    /// </summary>
    public static class HeadsmanKit
    {
        public const string KitName = "ecp_headsman_kit", AxeName = "ecp_headsman_axe", ControllerName = "ecp_headsman_animator";
        private const string StaggerSlot = "ecp_headsman_stagger", GameStagger = "stagger2";

        /// <summary>The kit, dressed; its axe is the pattern for every copy of the axe (thrown, shattered).</summary>
        public static GameObject? Kit { get; private set; }

        /// <summary>The kit's axe: the pieces in order, in the axe's own frame.</summary>
        public static Transform? Axe { get; private set; }

        public static RuntimeAnimatorController? Controller { get; private set; }

        public static void Load(AssetBundle bundle, Material skin)
        {
            GameObject kit = PrefabBench.Copy(EmbeddedBundle.Prefab(bundle, KitName), KitName);
            Dress(kit, skin);
            Kit = kit;
            Axe = GameMaterials.Find(kit.transform, AxeName);
            Controller = bundle.LoadAsset<RuntimeAnimatorController>(ControllerName);
        }

        /// <summary>The Skeleton's own body material, which the axe copies so it is lit like the creature.</summary>
        public static Material Skin(Transform visual) =>
            visual.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(r => r.bounds.size.sqrMagnitude).First().sharedMaterial;

        /// <summary>Every renderer under `model` onto a copy of the skin material wearing its own baked texture.</summary>
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

        /// <summary>The kit hung on the creature's bones, each mount on the bone it is named after, with no offset.</summary>
        public static void Wear(Transform visual)
        {
            GameObject kit = Object.Instantiate(Kit!, visual.parent, false);
            foreach (Transform mount in kit.transform.Cast<Transform>().ToArray())
            {
                Transform? bone = GameMaterials.Find(visual, mount.name);
                if (bone == null)
                {
                    Log.Warn($"Crypt Executioner: the Skeleton has no {mount.name} bone; its axe is left off.");
                    continue;
                }
                mount.SetParent(bone, false);
                (mount.localPosition, mount.localRotation, mount.localScale) = (Vector3.zero, Quaternion.identity, Vector3.one);
            }
            Object.DestroyImmediate(kit);
        }

        /// <summary>The creature's animator plays the bundle's controller, the Skeleton's stagger in the stagger slot.</summary>
        public static void Animate(Animator animator)
        {
            RuntimeAnimatorController skeleton = animator.runtimeAnimatorController;
            var ours = new AnimatorOverrideController(Controller) { name = ControllerName };
            AnimationClip? slot = Controller!.animationClips.FirstOrDefault(c => c.name == StaggerSlot);
            AnimationClip? stagger = skeleton.animationClips.FirstOrDefault(c => c.name == GameStagger);
            if (slot != null && stagger != null)
            {
                ours[slot] = stagger;
            }
            else
            {
                Log.Warn("Crypt Executioner: no stagger clip to borrow from the Skeleton; it stands through a stagger.");
            }
            animator.runtimeAnimatorController = ours;
        }

        /// <summary>A copy of the axe (its pieces, dressed) under `parent`, in the axe's own frame at the parent's origin, `scale` times the model.</summary>
        public static Transform CopyAxe(Transform parent, string name, float scale)
        {
            GameObject axe = Object.Instantiate(Axe!.gameObject, parent, false);
            axe.name = name;
            (axe.transform.localPosition, axe.transform.localRotation, axe.transform.localScale) = (Vector3.zero, Quaternion.identity, Vector3.one * scale);
            return axe.transform;
        }
    }
}
