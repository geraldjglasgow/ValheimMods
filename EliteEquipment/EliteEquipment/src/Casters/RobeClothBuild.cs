using System.Collections.Generic;
using MagicaCloth2;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// A seer's robe skirt as the game's own cloth (MagicaCloth, the capes' simulation): the skirt's own renderer
    /// (<see cref="RobeClothMesh"/>) as a mesh cloth, each vertex fixed or moving by <see cref="RobeClothFixed"/> (a
    /// per-vertex list, no paint map), pushed by <see cref="RobeClothColliders"/>, and set to feel like a heavy, damped robe
    /// rather than a cape: heavier and slower, little wind, its shape kept firmly near the hips and freer toward the hem (each
    /// setting eases over the top <see cref="Eased"/> of the skirt's depth, about 10 cm). Its fabric keeps the lengths of the
    /// pose it was built in, not the skinned skirt's (which the shins pass through), so only the legs widen it. The linen cape's settings come first for everything not set
    /// here. The game's SetupCloth is not used: its bone remap needs pre-built data a cloth made at runtime does not have,
    /// and the robe is already on the body's bones. Not built yet: the caller starts it.
    /// </summary>
    internal static class RobeClothBuild
    {
        /// <summary>
        /// How far each cloth particle keeps from a collider, metres: room for the skirt's lining and hem, which follow the
        /// particles a few centimetres off them.
        /// </summary>
        public const float Particle = 0.025f;

        /// <summary>Camera distance in metres beyond which the cloth rests and the plain skinned skirt is drawn.</summary>
        public const float Reach = 40f;

        private const string Template = "CapeLinen";

        // Metres within which the cloth's reduction merges vertices into one particle: the lining (0.8 cm inside the outside)
        // and the hem's rim move as one with the outside. MagicaCloth takes it as a share of the mesh's longest side, worked out
        // per skirt. 3.5 cm until 2026-11-03 merged the held vertices along the skirt's top with their neighbours (their bone
        // weights added, their place averaged), so the skirt's top drifted up to 5 cm off the robe's band: the dark "horizontal
        // cuts" (measured offline, Swamp robe; 1.5 cm: none). A thicker lining (the Mountain fur's, 3.6 cm) is its own
        // particles, pushed by the legs like the outside and drawn from inside only.
        internal const float Merge = 0.015f;

        // The share of the skirt's depth (0 at the fixed band, 1 at the hem) over which it eases from held to free.
        private const float Eased = 0.15f;

        public static MagicaCloth Make(Transform worn, SkinnedMeshRenderer skirt, RobeClothMesh parts, List<ColliderComponent> colliders)
        {
            var sim = new GameObject("EE_RobeCloth") { layer = worn.gameObject.layer };
            sim.SetActive(false);
            sim.transform.SetParent(worn, false);
            MagicaCloth cloth = sim.AddComponent<MagicaCloth>();
            Source(cloth, skirt, parts.Attributes, colliders);
            Feel(cloth.SerializeData, parts.Side);
            Hold(cloth.SerializeData);
            Push(cloth.SerializeData);
            Cull(cloth.SerializeData.cullingSettings);
            sim.SetActive(true);
            cloth.Initialize();
            cloth.DisableAutoBuild();
            return cloth;
        }

        private static void Source(MagicaCloth cloth, SkinnedMeshRenderer skirt, VertexAttribute[] attributes, List<ColliderComponent> colliders)
        {
            MagicaCloth template = Cape();
            if (template != null)
                cloth.SerializeData.Import(template, true);
            ClothSerializeData data = cloth.SerializeData;
            data.clothType = ClothProcess.ClothType.MeshCloth;
            data.sourceRenderers = new List<Renderer> { skirt };
            data.rootBones = new List<Transform>();
            data.paintMode = ClothSerializeData.PaintMode.Manual;
            data.paintMaps = new List<Texture2D>();
            data.colliderCollisionConstraint.colliderList = colliders;
            data.colliderCollisionConstraint.collisionBones = new List<Transform>();
            cloth.GetSerializeData2().vertexAttributeList = new List<VertexAttribute[]> { attributes };
        }

        /// <summary>Weight and air: heavier than a cape, damped so it settles at once, little wind; its own fabric lengths.</summary>
        private static void Feel(ClothSerializeData data, float side)
        {
            data.gravity = 8f;
            data.gravityFalloff = 0.5f;
            data.damping = new CurveSerializeData(0.2f);
            data.radius = new CurveSerializeData(Particle);
            data.animationPoseRatio = 0.1f;
            data.blendWeight = 1f;
            data.reductionSetting.simpleDistance = Mathf.Clamp(Merge / Mathf.Max(side, 0.1f), 0.001f, 0.2f);
            data.reductionSetting.shapeDistance = 0f;
            data.wind.influence = 0.05f;
        }

        /// <summary>
        /// Its shape: firm near the hips, freer toward the hem; never more than 80 cm off the skinned skirt and no angle
        /// limit, so a stride's knee and foot carry the hem with them (the workshop's cloth test over the eight robes,
        /// 2026-11-03: 40 cm and a 60 degree limit let a walking leg through in up to 24% of frames, a jogging one in 37-100%;
        /// this, mostly 0-11% and 30-58%).
        /// </summary>
        private static void Hold(ClothSerializeData data)
        {
            data.angleRestorationConstraint.useAngleRestoration = true;
            data.angleRestorationConstraint.stiffness = new CurveSerializeData(0.3f, Ease(1f, 0.6f, 0.35f));
            data.angleRestorationConstraint.velocityAttenuation = 0.8f;
            data.angleLimitConstraint.useAngleLimit = false;
            data.distanceConstraint.stiffness = new CurveSerializeData(1f);
            data.tetherConstraint.distanceCompression = 0.4f;
            data.triangleBendingConstraint.stiffness = 1f;
            data.motionConstraint.useMaxDistance = true;
            data.motionConstraint.maxDistance = new CurveSerializeData(0.8f, Ease(0.05f, 0.5f, 1f));
            data.motionConstraint.useBackstop = false;
        }

        /// <summary>
        /// The legs push it by their edges (a shin cannot slip between two particles); it trails a running body only a little,
        /// so a stride has less of it to push, and a particle may move as fast as a swinging foot pushes it.
        /// </summary>
        private static void Push(ClothSerializeData data)
        {
            data.colliderCollisionConstraint.mode = ColliderCollisionConstraint.Mode.Edge;
            data.colliderCollisionConstraint.friction = 0.3f;
            InertiaConstraint.SerializeData inertia = data.inertiaConstraint;
            inertia.worldInertia = 0.4f;
            inertia.localInertia = 1f;
            inertia.movementSpeedLimit = new CheckSliderSerializeData(true, 4f);
            inertia.particleSpeedLimit = new CheckSliderSerializeData(true, 5f);
            inertia.teleportMode = InertiaConstraint.TeleportMode.Reset;
            inertia.teleportDistance = 1f;
            inertia.teleportRotation = 90f;
        }

        /// <summary>Off screen it rests and starts again from the skinned skirt; beyond <see cref="Reach"/> the plain skirt is drawn.</summary>
        private static void Cull(CullingSettings culling)
        {
            culling.cameraCullingMode = CullingSettings.CameraCullingMode.Reset;
            culling.cameraCullingMethod = CullingSettings.CameraCullingMethod.AutomaticRenderer;
            culling.distanceCullingLength = new CheckSliderSerializeData(true, Reach);
            culling.distanceCullingFadeRatio = 0.2f;
            culling.distanceCullingReferenceObject = null;
        }

        private static AnimationCurve Ease(float top, float eased, float hem) =>
            new AnimationCurve(new Keyframe(0f, top), new Keyframe(Eased, eased), new Keyframe(1f, hem));

        private static MagicaCloth Cape()
        {
            GameObject cape = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(Template) : null;
            return cape != null ? cape.GetComponentInChildren<MagicaCloth>(true) : null;
        }
    }
}
