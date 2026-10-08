using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

namespace OpenKeep.Boots
{
    /// <summary>
    /// A dropped pair of boots looks like boots: the leggings' ground model is hidden and the boots' own mesh shown in its
    /// place, unskinned (its rest pose: standing on the ground as on a body). The mesh lives in the player body's space, so
    /// it takes the body renderer's place in the player prefab (scale and turn) once the scene's prefabs are known
    /// (<see cref="FitAll"/>), centred where the leggings' model was; the item keeps the leggings' collider. Every peer.
    /// </summary>
    public static class BootsDrop
    {
        private const string Child = "ok_boots_drop";
        private static readonly Dictionary<BootSet, Vector3> centres = new Dictionary<BootSet, Vector3>();
        private static bool fitted;

        public static void Prepare(BootSet set, GameObject item, GameObject skin)
        {
            SkinnedMeshRenderer source = BootsSkin.MaleMesh(skin);
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
            VisEquipment vis = player != null ? player.GetComponent<VisEquipment>() : null;
            if (vis == null || vis.m_bodyModel == null)
                return;
            fitted = true;
            Matrix4x4 body = player.transform.worldToLocalMatrix * vis.m_bodyModel.transform.localToWorldMatrix;
            foreach (KeyValuePair<BootSet, Vector3> pair in centres)
                Fit(pair.Key.Item, body, pair.Value);
        }

        private static void Fit(GameObject item, Matrix4x4 body, Vector3 centre)
        {
            Transform drop = item != null ? item.transform.Find(Child) : null;
            if (drop == null)
                return;
            drop.localPosition = Vector3.zero;
            drop.localRotation = body.rotation;
            drop.localScale = body.lossyScale;
            drop.localPosition = centre - ModelBounds.In(drop.gameObject, item.transform).center;
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
