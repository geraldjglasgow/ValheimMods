using UnityEngine;

namespace EliteEquipment.Weapons
{
    /// <summary>
    /// Dual axes (a copy of the Berserkir Axes): in the hand, the copy's <c>attach_skin</c> mesh is rebuilt from our right
    /// axe (bundle <c>model</c>, in the RightHand_Attach frame) on the right hand bone and our left axe (bundle <c>left</c>,
    /// in the LeftHand_Attach frame) on the left (<see cref="HandSkin"/>). On the ground, the copy's <c>attach</c> shows our
    /// two axes where the game lays its own pair, each with its box.
    /// </summary>
    public static class WeaponPair
    {
        private static readonly Vector3 RightDropped = new Vector3(0.0591f, 0.0066f, 0.1622f);
        private static readonly Quaternion RightDroppedTurn = new Quaternion(-0.0364f, -0.9718f, 0.0410f, 0.2292f);
        private static readonly Vector3 LeftDropped = new Vector3(-0.2084f, -0.0119f, 0.1941f);
        private static readonly Quaternion LeftDroppedTurn = new Quaternion(0.9676f, 0.1477f, 0.2044f, 0.0116f);

        public static void Wear(GameObject item, GameObject model, Material material, WeaponSet set)
        {
            SkinnedMeshRenderer skin = item.transform.Find("attach_skin")?.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Transform right = model.transform.Find("model");
            Transform left = model.transform.Find("left");
            if (skin == null || right == null || left == null)
            {
                Plugin.Log.LogWarning($"EliteEquipment: the {set.Prefab} is missing a part (attach_skin, model or left); it keeps the game's look");
                return;
            }
            skin.sharedMesh = Held(skin.sharedMesh, model.transform, right, left, set.Asset);
            if (material != null)
                skin.sharedMaterials = new[] { material };
            Dropped(item.transform.Find("attach"), model.transform, right, left, material);
        }

        private static Mesh Held(Mesh game, Transform root, Transform right, Transform left, string name)
        {
            var skin = new HandSkin(game);
            AddAxe(skin, root, right, HandSkin.RightHand, HandSkin.RightAttach);
            AddAxe(skin, left, left, HandSkin.LeftHand, HandSkin.LeftAttach);
            return skin.Finish(name + "_held");
        }

        /// <summary>Every mesh under the axe's part, from its frame through the hand's attach point into the hand bone.</summary>
        private static void AddAxe(HandSkin skin, Transform frame, Transform part, int bone, Matrix4x4 attach)
        {
            foreach (MeshFilter filter in part.GetComponentsInChildren<MeshFilter>(true))
            {
                Matrix4x4 matrix = skin.Hand(bone, attach) * WeaponParts.Relative(frame, filter.transform);
                skin.Add(filter.sharedMesh, vertex => (bone, matrix));
            }
        }

        private static void Dropped(Transform attach, Transform root, Transform right, Transform left, Material material)
        {
            if (attach == null)
                return;
            WeaponParts.RemoveGameModel(attach);
            GameObject r = WeaponParts.Place(right.gameObject, attach, RightDropped, RightDroppedTurn, material);
            GameObject l = WeaponParts.Place(left.gameObject, attach, LeftDropped, LeftDroppedTurn, material);
            Box(root.Find("col_box_axe"), root, r.transform);
            Box(root.Find("col_box_left_axe"), left, l.transform);
        }

        /// <summary>A bundle box collider copied onto a dropped axe, kept where it sits in that axe's frame.</summary>
        private static void Box(Transform source, Transform frame, Transform axe)
        {
            BoxCollider box = source != null ? source.GetComponent<BoxCollider>() : null;
            if (box == null)
                return;
            Matrix4x4 m = WeaponParts.Relative(frame, source);
            BoxCollider copy = axe.gameObject.AddComponent<BoxCollider>();
            copy.center = m.MultiplyPoint3x4(box.center);
            copy.size = box.size;
        }
    }
}
