using System;
using System.Collections.Generic;
using MagicaCloth2;
using UnityEngine;

using Object = UnityEngine.Object;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// A chest's hanging cloth (<see cref="ChestCloth"/>) worn as fabric: drawn on the body's bones beside the chest, and
    /// moved by the game's own cloth simulation the way capes are (MagicaCloth, its settings copied from the linen cape's),
    /// fixed at the belt row and free below (the mesh's second texture channel against a red-and-green paint map), pushed
    /// by a collider round each thigh (<see cref="Thighs"/>). Without a cape to
    /// copy, or if the simulation will not build, the cloth still hangs, weighted to the belt. Per client, never networked.
    /// </summary>
    internal static class ChestClothSim
    {
        private const string Template = "CapeLinen";

        // The thigh colliders, metres: a thigh and the leggings round, reaching a little past the hip joint and the knee.
        private const float ThighRadius = 0.09f;
        private const float ThighReach = 0.04f;

        // How much the world's wind moves it: a cape's 0.15 blows a short, heavy tabard aside for good.
        private const float Wind = 0.05f;

        private static readonly string[][] Legs = { new[] { "LeftUpLeg", "LeftLeg" }, new[] { "RightUpLeg", "RightLeg" } };

        private static Texture2D paint;

        /// <summary>The cloth drawn and simulated beside <paramref name="chest"/>; the object to destroy when it comes off.</summary>
        public static GameObject Attach(VisEquipment vis, SkinnedMeshRenderer chest, Mesh cloth)
        {
            var holder = new GameObject("EE_ChestCloth") { layer = chest.gameObject.layer };
            Transform body = vis.m_bodyModel.transform;
            holder.transform.SetParent(body.parent, false);
            holder.transform.localPosition = body.localPosition;
            holder.transform.localRotation = body.localRotation;
            holder.transform.localScale = body.localScale;
            SkinnedMeshRenderer renderer = holder.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = cloth;
            renderer.bones = chest.bones;
            renderer.rootBone = chest.rootBone;
            renderer.localBounds = chest.localBounds;
            renderer.sharedMaterial = chest.sharedMaterial;
            renderer.shadowCastingMode = chest.shadowCastingMode;
            Simulate(vis, holder, renderer);
            return holder;
        }

        private static void Simulate(VisEquipment vis, GameObject holder, SkinnedMeshRenderer renderer)
        {
            try
            {
                var sim = new GameObject("EE_ChestCloth_Cloth");
                sim.SetActive(false);
                sim.transform.SetParent(holder.transform, false);
                MagicaCloth cloth = sim.AddComponent<MagicaCloth>();
                Configure(cloth, renderer, Thighs(renderer, holder.AddComponent<ClothColliders>()));
                sim.SetActive(true);
                cloth.Initialize();
                cloth.DisableAutoBuild();
                if (!cloth.BuildAndRun())
                    Plugin.Log.LogWarning("EliteEquipment: the chest's cloth simulation did not build; it hangs without moving");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"EliteEquipment: the chest's cloth will hang without moving: {e.Message}");
            }
        }

        /// <summary>
        /// The cape's settings, then this cloth's renderer, paint map and the player's colliders. The game's own
        /// SetupCloth is not used: its bone remap needs pre-built data a cloth made at runtime does not have, and this
        /// cloth is already on the body's bones.
        /// </summary>
        private static void Configure(MagicaCloth cloth, SkinnedMeshRenderer renderer, List<ColliderComponent> colliders)
        {
            MagicaCloth template = Cape();
            if (template != null)
                cloth.SerializeData.Import(template, true);
            ClothSerializeData data = cloth.SerializeData;
            data.clothType = ClothProcess.ClothType.MeshCloth;
            data.sourceRenderers = new List<Renderer> { renderer };
            data.rootBones = new List<Transform>();
            data.paintMode = ClothSerializeData.PaintMode.Texture_Fixed_Move;
            data.paintMaps = new List<Texture2D> { Paint() };
            data.paintMapUvChannel = 1;
            data.wind.influence = Wind;
            data.colliderCollisionConstraint.colliderList = new List<ColliderComponent>(colliders ?? new List<ColliderComponent>());
        }

        /// <summary>
        /// A collider round each thigh, on the thigh bone from the hip joint to the knee where the bind pose puts them, so
        /// both sit on their legs alike. The game's own cape colliders will not do: they are 22 cm round so a cape drapes
        /// wide, and placed the same on both mirrored thigh bones, the left one reaches the middle and pushed the tabard
        /// aside for good. Destroyed with the holder.
        /// </summary>
        private static List<ColliderComponent> Thighs(SkinnedMeshRenderer renderer, ClothColliders made)
        {
            var thighs = new List<ColliderComponent>();
            foreach (string[] leg in Legs)
            {
                int hip = Bone(renderer, leg[0]), knee = Bone(renderer, leg[1]);
                if (hip >= 0 && knee >= 0)
                    thighs.Add(Thigh(renderer, made, hip, knee));
            }
            return thighs;
        }

        private static ColliderComponent Thigh(SkinnedMeshRenderer renderer, ClothColliders made, int hip, int knee)
        {
            Matrix4x4[] binds = renderer.sharedMesh.bindposes;
            Vector3 top = binds[hip].inverse.MultiplyPoint3x4(Vector3.zero), bottom = binds[knee].inverse.MultiplyPoint3x4(Vector3.zero);
            var holder = new GameObject("EE_ChestClothThigh");
            holder.transform.SetParent(renderer.bones[hip], false);
            holder.transform.localPosition = binds[hip].MultiplyPoint3x4((top + bottom) / 2f);
            holder.transform.localRotation = Quaternion.FromToRotation(Vector3.up, binds[hip].MultiplyVector(bottom - top).normalized);
            holder.transform.localScale = Vector3.one;
            var capsule = holder.AddComponent<MagicaCapsuleCollider>();
            capsule.direction = MagicaCapsuleCollider.Direction.Y;
            float scale = holder.transform.lossyScale.x, length = BodySurface.ToRest(bottom - top).magnitude + 2f * ThighReach;
            capsule.SetSize(ThighRadius / scale, ThighRadius / scale, length / scale);
            capsule.UpdateParameters();
            made.Made.Add(holder);
            return capsule;
        }

        private static int Bone(SkinnedMeshRenderer renderer, string name)
        {
            int index = Array.FindIndex(renderer.bones, bone => bone != null && bone.name == name);
            return index < renderer.sharedMesh.bindposes.Length ? index : -1;
        }

        private static MagicaCloth Cape()
        {
            GameObject cape = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(Template) : null;
            return cape != null ? cape.GetComponentInChildren<MagicaCloth>(true) : null;
        }

        /// <summary>Two texels: red (fixed) on the left, green (free) on the right, read at the cloth's second texture channel.</summary>
        private static Texture2D Paint()
        {
            if (paint != null)
                return paint;
            paint = new Texture2D(2, 1, TextureFormat.RGBA32, false) { name = "EE_ChestClothPaint", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            paint.SetPixels32(new[] { new Color32(255, 0, 0, 255), new Color32(0, 255, 0, 255) });
            paint.Apply(false, false);
            return paint;
        }

        public static void Detach(GameObject holder)
        {
            if (holder != null)
                Object.Destroy(holder);
        }
    }
}
