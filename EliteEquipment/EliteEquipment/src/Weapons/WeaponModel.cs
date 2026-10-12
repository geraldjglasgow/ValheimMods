using UnityEngine;

namespace EliteEquipment.Weapons
{
    /// <summary>
    /// A held weapon's model (swords, knives, axes, atgeirs, greataxes, the bow, the spear): the workshop built it in the
    /// game weapon's <c>attach</c> frame (the fist at the origin), so it goes under the copy's <c>attach</c> at identity, its
    /// colliders with it, in place of the game's model. The upgrade glow, trail and anything else under <c>attach</c> stay.
    /// </summary>
    public static class WeaponModel
    {
        private const string Holder = "attach";
        private const string Name = "ee_model";

        public static void Wear(GameObject item, GameObject model, Material material, WeaponSet set)
        {
            Transform attach = item.transform.Find(Holder);
            if (attach == null)
            {
                Plugin.Log.LogWarning($"EliteEquipment: the {set.Base} copy has no {Holder}; the {set.Name} shows the game's model");
                return;
            }
            WeaponParts.RemoveGameModel(attach);
            GameObject ours = WeaponParts.Place(model, attach, Vector3.zero, Quaternion.identity, material);
            ours.name = Name;
            WeaponParts.Glow(attach, ours.GetComponentInChildren<MeshRenderer>(true));
            WeaponParts.Trail(attach, set);
            WeaponParts.NoLod(item);
        }
    }
}
