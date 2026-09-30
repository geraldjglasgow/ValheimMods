using System.Collections.Generic;
using BundlePrefabs;
using EliteCreaturesPack.Core;
using UnityEngine;

namespace EliteCreaturesPack.Arsenal
{
    /// <summary>
    /// The players' bone weapons and the spine. Each weapon is a copy of the bronze-age weapon of its class
    /// (<see cref="ArsenalWeapon.GameItem"/>), so it keeps that weapon's handling, animations, skill, sounds, trail,
    /// durability, weight and upgrades; the game's model comes off its "attach" and the bone one goes in, with a box
    /// collider round it for the ground and the upgrade glow reshaped to it. Its damage, and what each upgrade adds, is
    /// that weapon's times <see cref="DamageFactor"/>: like bronze, a little weaker. The
    /// spine is a copy of the game's bone fragments (a material: its weight, stack and sounds) wearing the bundle's
    /// spine. Every item shows the bundle's icon.
    /// </summary>
    public static class ArsenalItems
    {
        public const string SpineName = "ECP_Spine";
        public const string SpineWord = "ecp_spine";
        private const string GameSpine = "BoneFragments";
        private const float DamageFactor = 0.85f;

        /// <summary>The weapons built, by weapon; one the game lacks the model for is missing.</summary>
        public static readonly Dictionary<ArsenalWeapon, GameObject> Weapons = new Dictionary<ArsenalWeapon, GameObject>();

        public static GameObject? Spine { get; private set; }

        public static void Build(ZNetScene scene, AssetBundle bundle, Material skin)
        {
            Spine = BuildSpine(scene.GetPrefab(GameSpine), bundle, skin);
            foreach (ArsenalWeapon weapon in ArsenalWeapon.All)
            {
                GameObject? item = BuildWeapon(scene.GetPrefab(weapon.GameItem), weapon, bundle, skin);
                if (item != null)
                {
                    Weapons[weapon] = item;
                }
            }
        }

        private static GameObject? BuildWeapon(GameObject? game, ArsenalWeapon weapon, AssetBundle bundle, Material skin)
        {
            if (game == null || game.transform.Find("attach") == null)
            {
                Log.Warn($"Bone {weapon.Key.ToLowerInvariant()} not built: the game has no {weapon.GameItem} with an attach.");
                return null;
            }
            GameObject item = PrefabBench.Copy(game, weapon.Item);
            Transform slot = item.transform.Find("attach");
            ArsenalLook.Clear(slot, "UpgraderGlow", "equiped");
            GameObject model = ArsenalLook.Wear(slot, EmbeddedBundle.Prefab(bundle, weapon.PlayerModel), skin);
            if (weapon.IsBow)
            {
                ArsenalLook.String(slot, ArsenalLook.PlayerBow, skin);
            }
            ArsenalLook.Glow(slot, ArsenalLook.Collide(slot, model));
            ItemDrop drop = item.GetComponent<ItemDrop>();
            drop.m_itemData.m_shared.m_damages.Modify(DamageFactor);
            drop.m_itemData.m_shared.m_damagesPerLevel.Modify(DamageFactor);
            Describe(drop, weapon.Word, bundle.LoadAsset<Sprite>(weapon.Icon));
            return item;
        }

        private static GameObject? BuildSpine(GameObject? game, AssetBundle bundle, Material skin)
        {
            if (game == null || game.transform.Find("attach") == null)
            {
                Log.Error($"Spine not built: the game has no {GameSpine} with an attach; nothing needs one to be made.");
                return null;
            }
            GameObject item = PrefabBench.Copy(game, SpineName);
            Transform slot = item.transform.Find("attach");
            ArsenalLook.Strip(slot);
            ArsenalLook.Collide(slot, ArsenalLook.Wear(slot, EmbeddedBundle.Prefab(bundle, "ecp_spine"), skin));
            Describe(item.GetComponent<ItemDrop>(), SpineWord, bundle.LoadAsset<Sprite>("ecp_spine_icon"));
            return item;
        }

        /// <summary>The item's own name, description and icon (the game item's icon when the bundle lacks one).</summary>
        public static void Describe(ItemDrop drop, string word, Sprite? icon)
        {
            ItemDrop.ItemData.SharedData shared = drop.m_itemData.m_shared;
            shared.m_name = "$item_" + word;
            shared.m_description = "$item_" + word + "_description";
            if (icon != null)
            {
                shared.m_icons = new[] { icon };
            }
            else
            {
                Log.Warn($"Skeleton arsenal: the bundle has no icon for {drop.name}; it shows the game item's.");
            }
        }
    }
}
