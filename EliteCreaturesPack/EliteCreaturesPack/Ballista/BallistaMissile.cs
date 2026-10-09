using System.Linq;
using BundlePrefabs;
using EliteCreaturesPack.Core;
using UnityEngine;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// The Bone Missile, the Bone Ballista's ammunition, and its flight. The item is a copy of the game's own unused bone
    /// missile (`TurretBoltBone`: a ballista missile with no name or recipe in the game) wearing the bundle's missile; the
    /// shot a copy of its projectile (`Turret_projectilebone`) wearing it too, its trail kept. An ammo type of its own,
    /// so only the Bone Ballista takes it (the game's ballista keeps its own list). It hits blunt, as bone does. Lighter than a wooden missile, the
    /// Crossbows skill rises with its hits.
    /// </summary>
    public static class BallistaMissile
    {
        public const string ItemName = "ECP_BoneMissile", ShotName = "ECP_BoneMissile_projectile";
        public const string Word = "item_ecp_bonemissile";
        public const string AmmoType = "$ammo_ecp_bonemissile";
        public const float Damage = 50f, Force = 40f, Speed = 45f;
        private const string GameItem = "TurretBoltBone", GameShot = "Turret_projectilebone", Slot = "attach", Trail = "trail";
        private const string Model = "ecp_bone_missile", Icon = "ecp_bone_missile_icon";
        private static readonly Vector3 Box = new Vector3(0.1f, 0.1f, 0.62f);

        /// <summary>How far behind the shot's origin (its nose, where it tests for hits) the missile's centre is drawn.</summary>
        public const float NoseToCentre = 0.3f;

        public static GameObject? Item { get; private set; }
        public static GameObject? Shot { get; private set; }

        public static void Build(ZNetScene scene, AssetBundle bundle, Material? body)
        {
            GameObject? item = scene.GetPrefab(GameItem), shot = scene.GetPrefab(GameShot);
            if (item?.GetComponent<ItemDrop>() == null || shot?.GetComponent<Projectile>() == null || item.transform.Find(Slot) == null)
            {
                Log.Error($"Bone missiles not built: the game has no {GameItem} or {GameShot}.");
                return;
            }
            GameObject look = EmbeddedBundle.Prefab(bundle, Model);
            Shot = Flight(shot, look, body);
            Item = PrefabBench.Copy(item, ItemName);
            Ground(Item.transform.Find(Slot), look, body);
            Describe(Item.GetComponent<ItemDrop>(), bundle);
        }

        /// <summary>The item's own model in the slot the game's drew in, with a box to lie on the ground by.</summary>
        private static void Ground(Transform slot, GameObject look, Material? body)
        {
            foreach (Component part in slot.GetComponents<Component>().Where(c => c is MeshRenderer || c is MeshFilter || c is Collider))
            {
                Object.DestroyImmediate(part);
            }
            (slot.localPosition, slot.localRotation, slot.localScale) = (Vector3.up * 0.05f, Quaternion.identity, Vector3.one);
            Wear(slot, look, body, Vector3.zero);
            slot.gameObject.AddComponent<BoxCollider>().size = Box;
        }

        /// <summary>The game's bone projectile with its models off and ours on, its nose at the origin; its trail stays.</summary>
        private static GameObject Flight(GameObject game, GameObject look, Material? body)
        {
            GameObject shot = PrefabBench.Copy(game, ShotName);
            foreach (Component part in shot.GetComponentsInChildren<Component>(true).Where(c => c is MeshRenderer || c is SkinnedMeshRenderer || c is MeshFilter))
            {
                Object.DestroyImmediate(part);
            }
            Projectile projectile = shot.GetComponent<Projectile>();
            GameObject model = Wear(shot.transform, look, body, Vector3.back * NoseToCentre);
            if (projectile.m_visual != null && projectile.m_visual.name != Trail)
            {
                projectile.m_visual = model;
            }
            return shot;
        }

        private static GameObject Wear(Transform parent, GameObject look, Material? body, Vector3 at)
        {
            GameObject model = Object.Instantiate(look, parent, false);
            model.name = "missile";
            model.transform.localPosition = at;
            foreach (Collider collider in model.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(collider);
            }
            BallistaDress.Dress(model, body);
            BallistaDress.Layer(model, parent.gameObject.layer);
            return model;
        }

        private static void Describe(ItemDrop drop, AssetBundle bundle)
        {
            drop.m_itemData.m_dropPrefab = drop.gameObject;
            ItemDrop.ItemData.SharedData shared = drop.m_itemData.m_shared;
            (shared.m_name, shared.m_description, shared.m_ammoType) = ("$" + Word, "$" + Word + "_description", AmmoType);
            shared.m_damages = new HitData.DamageTypes { m_blunt = Damage };
            (shared.m_attackForce, shared.m_skillType, shared.m_weight) = (Force, Skills.SkillType.Crossbows, 0.3f);
            shared.m_attack.m_attackProjectile = Shot;
            shared.m_attack.m_projectileVel = Speed;
            Sprite? icon = bundle.LoadAsset<Sprite>(Icon);
            if (icon != null)
            {
                shared.m_icons = new[] { icon };
            }
            else
            {
                Log.Warn($"Bone missiles: the bundle has no {Icon}; no inventory icon.");
            }
        }
    }
}
