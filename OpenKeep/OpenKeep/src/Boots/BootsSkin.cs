using UnityEngine;

namespace OpenKeep.Boots
{
    /// <summary>
    /// A bundle piece made into what the game's armour attach (<c>VisEquipment.AttachArmor</c>) wears: an inactive child
    /// named <c>attach_skin</c> (the game instantiates it under the body and binds its skinned meshes to the body's
    /// bones), holding the piece's Male and Female meshes in the game's materials and <see cref="SkinSex"/> to show the
    /// right one. The boots' skin sits under the boots item; the split trousers' under a holder of its own on the bench,
    /// worn in place of the leggings' own skin (<see cref="LegsLook"/>), so the game's prefabs are never changed.
    /// </summary>
    public static class BootsSkin
    {
        public const string Worn = "attach_skin";

        /// <summary>The skin under <paramref name="parent"/>, or null when the piece or its materials are missing.</summary>
        public static GameObject Make(GameObject parent, GameObject piece, SetLook look)
        {
            if (piece == null || look == null)
                return null;
            GameObject skin = Object.Instantiate(piece, parent.transform, false);
            skin.name = Worn;
            skin.SetActive(false);
            foreach (Transform part in skin.transform)
                part.gameObject.SetActive(true);
            if (!look.Wear(skin))
            {
                Object.DestroyImmediate(skin);
                return null;
            }
            skin.AddComponent<SkinSex>();
            return skin;
        }

        /// <summary>The Male (else any) variant's renderer: the dropped item's look.</summary>
        public static SkinnedMeshRenderer MaleMesh(GameObject skin)
        {
            Transform male = skin != null ? skin.transform.Find("Male") : null;
            Transform root = male != null ? male : skin != null ? skin.transform : null;
            return root != null ? root.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
        }
    }
}
