using System;
using BundlePrefabs;
using EliteCreaturesReborn.Util;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesReborn.Creatures.BatFiend
{
    /// <summary>
    /// The Bat-fiend's model from its embedded bundle (ValheimAssets Assets/Creatures/HelsCreatures/BatFiend,
    /// Tools/v004/HCBatFiendBundleV004.cs): the prefab <c>ecr_batfiend</c>, whose root carries the one Animator on the
    /// controller <c>ecr_batfiend_animator</c> (Generic, twelve clips, hits as <c>OnAttackTrigger</c> events, a
    /// footstep curve on the walk and run), the armature node and its 39 bones under their Blender names, and the skinned
    /// body on the placeholder <c>ecr_batfiend_skin</c>. The placeholder becomes the game Wolf's own body material wearing
    /// the bundle's albedo and normal map (<see cref="GameMaterials.Dress"/>), so the game's creature shader lights and
    /// star-tints it. Loaded and dressed once into an inactive template the creature and its corpse copy; a missing
    /// bundle or part is logged and nothing is built.
    /// </summary>
    internal static class BatFiendModel
    {
        public const string Bundle = "ecr_batfiend", Asset = "ecr_batfiend", Skin = "ecr_batfiend_skin", HeadBone = "Head";
        private const float Gloss = 0.1f;

        /// <summary>The dressed model, inactive on the bench; null until loaded.</summary>
        public static GameObject? Template { get; private set; }

        /// <summary>Loads and dresses the model once; false (logged) when the bundle or a part of it is missing.</summary>
        public static bool Load(Material skin, int layer)
        {
            if (Template != null)
            {
                return true;
            }
            GameObject? model = Model();
            if (model == null)
            {
                return false;
            }
            Dress(model, skin);
            Layer(model.transform, layer);
            Template = model;
            return true;
        }

        /// <summary>A bench copy of the bundle's prefab with its animator set up, or null (logged).</summary>
        private static GameObject? Model()
        {
            GameObject? prefab = Prefab();
            if (prefab == null)
            {
                return null;
            }
            GameObject model = PrefabBench.Copy(prefab, "ecr_batfiend_model");
            Animator? animator = model.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null || Body(model) == null)
            {
                Log.Error($"Bat-fiend not built: the {Asset} model has no Animator with a controller, or no skinned body.");
                Object.DestroyImmediate(model);
                return null;
            }
            (animator.applyRootMotion, animator.cullingMode) = (false, AnimatorCullingMode.AlwaysAnimate);
            return model;
        }

        private static GameObject? Prefab()
        {
            try
            {
                return EmbeddedBundle.Prefab(EmbeddedBundle.Load(typeof(BatFiendModel).Assembly, Bundle), Asset);
            }
            catch (Exception e)
            {
                Log.Error($"Bat-fiend not built: its bundle {Bundle} did not load ({e.Message}).");
                return null;
            }
        }

        /// <summary>The skin placeholder becomes the game skin wearing its textures; a model without it keeps the bundle's.</summary>
        private static void Dress(GameObject model, Material skin)
        {
            Material? placeholder = Body(model)!.sharedMaterial;
            if (placeholder == null || placeholder.name.Replace(" (Instance)", "") != Skin)
            {
                Log.Warn($"Bat-fiend: its body has no material named {Skin}; it is drawn as the bundle left it.");
                return;
            }
            Material dressed = GameMaterials.Plain(GameMaterials.Dress(skin, placeholder), Gloss);
            GameMaterials.Apply(model, name => name == Skin ? dressed : null);
        }

        private static void Layer(Transform node, int layer)
        {
            node.gameObject.layer = layer;
            foreach (Transform child in node)
            {
                Layer(child, layer);
            }
        }

        /// <summary>A copy of the dressed model under <paramref name="parent"/>, named Visual (the name the game's Character looks for).</summary>
        public static GameObject Copy(Transform parent)
        {
            GameObject visual = Object.Instantiate(Template!, parent, false);
            visual.name = "Visual";
            (visual.transform.localPosition, visual.transform.localRotation, visual.transform.localScale) = (Vector3.zero, Quaternion.identity, Vector3.one);
            return visual;
        }

        /// <summary>The model's head bone, or null.</summary>
        public static Transform? Head(Transform model) => GameMaterials.Find(model, HeadBone);

        /// <summary>The model's one skinned body.</summary>
        public static SkinnedMeshRenderer? Body(GameObject model) => model.GetComponentInChildren<SkinnedMeshRenderer>(true);

        /// <summary>The game Wolf's body material: the one its star tints go on, else the first creature-shaded one, or null.</summary>
        public static Material? WolfSkin(GameObject wolf)
        {
            LevelEffects? level = wolf.GetComponentInChildren<LevelEffects>(true);
            if (level != null && level.m_mainRender != null && Creature(level.m_mainRender.sharedMaterial))
            {
                return level.m_mainRender.sharedMaterial;
            }
            foreach (Renderer renderer in wolf.GetComponentsInChildren<Renderer>(true))
            {
                if ((renderer is SkinnedMeshRenderer || renderer is MeshRenderer) && Creature(renderer.sharedMaterial))
                {
                    return renderer.sharedMaterial;
                }
            }
            return null;
        }

        private static bool Creature(Material? material) =>
            material != null && material.shader != null && material.shader.name.IndexOf("Creature", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
