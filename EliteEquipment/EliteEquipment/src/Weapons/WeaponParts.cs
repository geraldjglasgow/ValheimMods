using BundlePrefabs;
using UnityEngine;

using Object = UnityEngine.Object;

namespace EliteEquipment.Weapons
{
    /// <summary>
    /// The pieces every weapon model needs once it is in a copied game weapon: the game item's own material (its UVs lie
    /// on that atlas), the copy's layer, the game model out, the upgrade glow and swing trail pointed at ours, no LOD group
    /// left drawing the game's mesh.
    /// </summary>
    public static class WeaponParts
    {
        /// <summary>The look item's body material as it is, its painted styles off; null (with a warning) when missing.</summary>
        public static Material Material(GameObject look, WeaponSet set)
        {
            Material borrowed = look != null ? GameMaterials.Borrow(look, set.LookPart) : null;
            if (borrowed == null)
                Plugin.Log.LogWarning($"EliteEquipment: no {set.Look}/{set.LookPart} material for the {set.Name}; it draws untextured");
            return borrowed != null ? GameMaterials.Unstyled(borrowed) : null;
        }

        /// <summary>A copy of a bundle part under a parent at a local pose, in the material, on the parent's layer.</summary>
        public static GameObject Place(GameObject source, Transform parent, Vector3 position, Quaternion rotation, Material material)
        {
            GameObject copy = Object.Instantiate(source, parent, false);
            copy.transform.localPosition = position;
            copy.transform.localRotation = rotation;
            copy.transform.localScale = Vector3.one;
            if (material != null)
                GameMaterials.Apply(copy, placeholder => material);
            foreach (Transform part in copy.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = parent.gameObject.layer;
            return copy;
        }

        /// <summary>Removes the game's model: every direct child that draws a mesh or holds a light (its collider goes with it).</summary>
        public static void RemoveGameModel(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                if (child.GetComponent<MeshFilter>() != null || child.GetComponent<SkinnedMeshRenderer>() != null || child.GetComponent<Light>() != null)
                    Object.DestroyImmediate(child);
            }
        }

        /// <summary>The upgrade glow's particle systems emit from our mesh instead of the removed one.</summary>
        public static void Glow(Transform attach, Renderer ours)
        {
            if (ours == null)
                return;
            foreach (ParticleSystem system in attach.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.ShapeModule shape = system.shape;
                if (shape.shapeType == ParticleSystemShapeType.MeshRenderer && ours is MeshRenderer mesh)
                    shape.meshRenderer = mesh;
                else if (shape.shapeType == ParticleSystemShapeType.SkinnedMeshRenderer && ours is SkinnedMeshRenderer skin)
                    shape.skinnedMeshRenderer = skin;
            }
        }

        /// <summary>The swing trail's ends moved along our blade, when the set gives them (attach frame).</summary>
        public static void Trail(Transform attach, WeaponSet set)
        {
            Transform trail = GameMaterials.Find(attach, "trail");
            Transform start = trail != null ? trail.Find("base") : null;
            Transform tip = trail != null ? trail.Find("tip") : null;
            if (start == null || tip == null || !set.TrailBase.HasValue || !set.TrailTip.HasValue)
                return;
            start.position = attach.TransformPoint(set.TrailBase.Value);
            tip.position = attach.TransformPoint(set.TrailTip.Value);
        }

        /// <summary>A LOD group on the copy would keep switching to the removed game mesh: it goes.</summary>
        public static void NoLod(GameObject item)
        {
            LODGroup lod = item.GetComponent<LODGroup>();
            if (lod != null)
                Object.DestroyImmediate(lod);
        }

        /// <summary>A transform's matrix in another's space (bundle parts placed relative to their frame).</summary>
        public static Matrix4x4 Relative(Transform frame, Transform part) => frame.worldToLocalMatrix * part.localToWorldMatrix;
    }
}
