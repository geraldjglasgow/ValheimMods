using UnityEngine;

namespace EliteEquipment.Weapons
{
    /// <summary>
    /// Fist weapons (a copy of the Flesh Rippers): our one mesh holds both claws in the right hand's attach frame, the right
    /// claw where z is above -0.10 and the left one mirrored across z = -0.10. In the hand, the copy's <c>attach_skin</c>
    /// mesh is rebuilt from it, the right half on the right hand bone and the left half turned 180 degrees about X and moved
    /// 0.2 m back into the left hand's frame (<see cref="HandSkin"/>). On the ground, the copy's <c>attach</c> (which draws
    /// the dropped look itself) shows our mesh and our box.
    /// </summary>
    public static class WeaponFist
    {
        private const float Middle = -0.10f;

        private static readonly Matrix4x4 LeftFromRight = Matrix4x4.Rotate(Quaternion.Euler(180f, 0f, 0f)) * Matrix4x4.Translate(new Vector3(0f, 0f, 0.2f));

        public static void Wear(GameObject item, GameObject model, Material material, WeaponSet set)
        {
            SkinnedMeshRenderer skin = item.transform.Find("attach_skin")?.GetComponentInChildren<SkinnedMeshRenderer>(true);
            MeshFilter ours = model.transform.Find("model")?.GetComponentInChildren<MeshFilter>(true);
            if (skin == null || ours == null)
            {
                Plugin.Log.LogWarning($"EliteEquipment: the {set.Name} is missing a part (attach_skin or model); it keeps the game's look");
                return;
            }
            skin.sharedMesh = Held(skin.sharedMesh, ours.sharedMesh, set.Asset);
            if (material != null)
                skin.sharedMaterials = new[] { material };
            Dropped(item.transform.Find("attach"), model.transform, ours.sharedMesh, material);
        }

        private static Mesh Held(Mesh game, Mesh claws, string name)
        {
            var skin = new HandSkin(game);
            Matrix4x4 right = skin.Hand(HandSkin.RightHand, HandSkin.RightAttach);
            Matrix4x4 left = skin.Hand(HandSkin.LeftHand, HandSkin.LeftAttach) * LeftFromRight;
            skin.Add(claws, vertex => vertex.z > Middle ? (HandSkin.RightHand, right) : (HandSkin.LeftHand, left));
            return skin.Finish(name + "_held");
        }

        /// <summary>The game's dropped claws are drawn by <c>attach</c> itself: our mesh, material and box take their place.</summary>
        private static void Dropped(Transform attach, Transform root, Mesh claws, Material material)
        {
            MeshFilter filter = attach != null ? attach.GetComponent<MeshFilter>() : null;
            if (filter == null)
                return;
            filter.sharedMesh = claws;
            MeshRenderer renderer = attach.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterials = new[] { material };
            BoxCollider box = attach.GetComponent<BoxCollider>();
            Transform source = root.Find("col_box_claws");
            BoxCollider ours = source != null ? source.GetComponent<BoxCollider>() : null;
            if (box == null || ours == null)
                return;
            box.center = WeaponParts.Relative(root, source).MultiplyPoint3x4(ours.center);
            box.size = ours.size;
        }
    }
}
