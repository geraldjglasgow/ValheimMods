using BundlePrefabs;
using UnityEngine;

using Object = UnityEngine.Object;

namespace EliteEquipment.Weapons
{
    /// <summary>
    /// A thrown weapon's projectile (the Black Metal Spear): the base spear throws a shared game projectile
    /// (<c>projectile_wolffang</c>), so ours is a copy of it, registered on every peer, wearing our model where the game's
    /// sits (the set's thrown pose) with our colliders taken off (a projectile hits by its own cast), and the item's
    /// secondary attack throws the copy. The game's own spears keep throwing theirs.
    /// </summary>
    public static class WeaponProjectile
    {
        public static GameObject Build(WeaponSet set, GameObject item, GameObject model, Material material)
        {
            ItemDrop.ItemData.SharedData shared = item.GetComponent<ItemDrop>().m_itemData.m_shared;
            GameObject game = shared.m_secondaryAttack?.m_attackProjectile;
            if (!set.Thrown || game == null)
                return null;
            GameObject copy = PrefabBench.Copy(game, "EE_projectile_" + set.Asset.Substring(3));
            WeaponParts.RemoveGameModel(copy.transform);
            GameObject ours = WeaponParts.Place(model, copy.transform, set.ThrownPosition, Quaternion.Euler(set.ThrownRotation), material);
            foreach (Collider collider in ours.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            shared.m_secondaryAttack.m_attackProjectile = copy;
            return copy;
        }
    }
}
