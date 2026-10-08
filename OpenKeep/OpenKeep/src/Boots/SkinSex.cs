using UnityEngine;

namespace OpenKeep.Boots
{
    /// <summary>
    /// On a worn split piece (the game's armour attach made it from <see cref="BootsSkin"/>): shows its <c>Male</c> or
    /// <c>Female</c> mesh for the body it is on, by the model index the character's VisEquipment draws (the ZDO's on every
    /// client), and follows a change (the character editor). The game binds only the active meshes to the body's bones,
    /// so every mesh is bound again here before one is hidden. A piece on no character (a dropped item) does nothing.
    /// </summary>
    public sealed class SkinSex : MonoBehaviour
    {
        private VisEquipment vis;
        private Transform male;
        private Transform female;
        private int shown = -1;

        private void Awake()
        {
            vis = GetComponentInParent<VisEquipment>();
            male = transform.Find("Male");
            female = transform.Find("Female");
        }

        private void LateUpdate()
        {
            if (vis == null || vis.m_bodyModel == null)
                return;
            int sex = vis.m_currentModelIndex == 1 ? 1 : 0;
            if (sex == shown)
                return;
            if (shown < 0)
                Bind(vis.m_bodyModel);
            shown = sex;
            if (male != null)
                male.gameObject.SetActive(sex == 0);
            if (female != null)
                female.gameObject.SetActive(sex == 1);
        }

        private void Bind(SkinnedMeshRenderer body)
        {
            foreach (SkinnedMeshRenderer skin in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skin.rootBone = body.rootBone;
                skin.bones = body.bones;
            }
        }
    }
}
