using System;
using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

using Object = UnityEngine.Object;

namespace OpenKeep.Boots
{
    /// <summary>
    /// A dropped pair of boots looks like boots: the leggings' ground model is hidden and the boots' own mesh shown in its
    /// place, unskinned (its rest pose: standing on the ground as on a body). The mesh is turned and scaled into the player
    /// body's space through the bind pose both share at the left foot, and the body renderer's place in the player prefab,
    /// once the scene's prefabs are known (<see cref="FitAll"/>), centred where the leggings' model was; the item keeps the
    /// leggings' collider. Boots with no mesh of their own (the painted ones) keep the leggings' ground model. Every peer.
    /// </summary>
    public static class BootsDrop
    {
        private const string Child = "ok_boots_drop";
        private const string Foot = "LeftFoot";
        private static readonly Dictionary<BootSet, Vector3> centres = new Dictionary<BootSet, Vector3>();
        private static bool fitted;

        public static void Prepare(BootSet set, GameObject item, GameObject skin)
        {
            SkinnedMeshRenderer source = skin != null ? skin.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
            if (source == null || source.sharedMesh == null)
                return;
            centres[set] = ModelBounds.In(item, item.transform).center;
            foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>(true))
            {
                if ((renderer is MeshRenderer || renderer is SkinnedMeshRenderer) && !renderer.transform.IsChildOf(skin.transform))
                    renderer.enabled = false;
            }
            var drop = new GameObject(Child) { layer = item.layer };
            drop.transform.SetParent(item.transform, false);
            drop.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
            MeshRenderer look = drop.AddComponent<MeshRenderer>();
            look.sharedMaterials = source.sharedMaterials;
            Lods(item, new Renderer[] { look });
        }

        /// <summary>Once per process, with the scene's prefabs: every dropped pair at the body's scale and turn.</summary>
        public static void FitAll(List<GameObject> prefabs)
        {
            if (fitted)
                return;
            GameObject player = prefabs.Find(p => p != null && p.name == "Player");
            SkinnedMeshRenderer body = player != null ? player.GetComponent<VisEquipment>()?.m_bodyModel : null;
            if (body == null || body.sharedMesh == null)
                return;
            fitted = true;
            Matrix4x4 place = player.transform.worldToLocalMatrix * body.transform.localToWorldMatrix;
            int foot = Array.FindIndex(body.bones, bone => bone != null && bone.name == Foot);
            Matrix4x4[] bodyBinds = body.sharedMesh.bindposes;
            foreach (KeyValuePair<BootSet, Vector3> pair in centres)
                Fit(pair.Key.Item, place * Space(pair.Key.Item, bodyBinds, foot), pair.Value);
        }

        private static void Fit(GameObject item, Matrix4x4 local, Vector3 centre)
        {
            Transform drop = item != null ? item.transform.Find(Child) : null;
            if (drop == null)
                return;
            drop.localPosition = Vector3.zero;
            drop.localRotation = local.rotation;
            drop.localScale = local.lossyScale;
            drop.localPosition = centre - ModelBounds.In(drop.gameObject, item.transform).center;
        }

        /// <summary>
        /// From the boots mesh's space to the body mesh's: both are bound to the same foot bone at rest, so the body's
        /// inverse bind pose there times the boots' own gives it (identity when the game exported both alike).
        /// </summary>
        private static Matrix4x4 Space(GameObject item, Matrix4x4[] bodyBinds, int foot)
        {
            Transform drop = item != null ? item.transform.Find(Child) : null;
            Matrix4x4[] binds = drop != null ? drop.GetComponent<MeshFilter>().sharedMesh.bindposes : null;
            if (binds == null || foot < 0 || foot >= binds.Length || foot >= bodyBinds.Length)
                return Matrix4x4.identity;
            return bodyBinds[foot].inverse * binds[foot];
        }

        /// <summary>The item's level-of-detail group draws the boots instead of the leggings' model.</summary>
        private static void Lods(GameObject item, Renderer[] shown)
        {
            LODGroup group = item.GetComponent<LODGroup>();
            if (group == null)
                return;
            LOD[] lods = group.GetLODs();
            if (lods.Length == 0)
                return;
            lods[0].renderers = shown;
            for (int i = 1; i < lods.Length; i++)
                lods[i].renderers = new Renderer[0];
            group.SetLODs(lods);
        }
    }
}
