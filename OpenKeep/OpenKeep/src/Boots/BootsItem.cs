using System;
using System.Collections.Generic;
using BundlePrefabs;
using UnityEngine;

using Object = UnityEngine.Object;
using SharedData = ItemDrop.ItemData.SharedData;

namespace OpenKeep.Boots
{
    /// <summary>
    /// One boots item prefab: a copy of its leggings on the bench (net view, rigidbody, sounds, quality levels,
    /// durability, set and equip timing all as the leggings'), still a Legs item so tooltips, armour displays and other
    /// mods treat it as armour (wearing it is OpenKeep's, <see cref="WornBoots"/>). Its worn skin is the leggings' own
    /// with each mesh cut down to the boots part (<see cref="SkinSplit"/>), in the game's materials; boots that the game
    /// only paints (leather, troll leather, and the rag shoes' own paint) have no skin and paint the feet (<see cref="BodyPaint"/>).
    /// Its own name, the workshop's boots icon (<see cref="BootsIcons"/>; without it one cut from the leggings',
    /// <see cref="SpriteCrop"/>), a fifth of the leggings' stats and weight
    /// (<see cref="BootsStats"/>), no resistances, no equip effect, no body paint of the leggings'. The ground look is the
    /// boots' mesh (<see cref="BootsDrop"/>).
    /// </summary>
    public static class BootsItem
    {
        private const string Description = "$ok_boots_desc";
        // The part of a leggings icon the boots are cut from: its lower part, where the game draws the feet.
        private static readonly Rect IconPart = new Rect(0f, 0f, 1f, 0.42f);

        public static GameObject Build(BootSet set, GameObject legs, Dictionary<Mesh, Mesh[]> cuts)
        {
            GameObject item = PrefabBench.Copy(legs, set.Prefab);
            GameObject skin = Dress(item, cuts);
            Describe(item.GetComponent<ItemDrop>().m_itemData.m_shared, set, legs);
            BootsDrop.Prepare(set, item, skin);
            return item;
        }

        /// <summary>Keeps the copied skin with its boots parts, and drops every other attached piece; null without a cut.</summary>
        private static GameObject Dress(GameObject item, Dictionary<Mesh, Mesh[]> cuts)
        {
            GameObject skin = null;
            for (int i = item.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = item.transform.GetChild(i).gameObject;
                if (child.name == SkinSplit.Skin && cuts.Count > 0)
                    skin = child;
                else if (child.name.StartsWith("attach", StringComparison.Ordinal))
                    Object.DestroyImmediate(child);
            }
            if (skin != null)
                Wear(skin, cuts);
            return skin;
        }

        private static void Wear(GameObject skin, Dictionary<Mesh, Mesh[]> cuts)
        {
            foreach (SkinnedMeshRenderer part in skin.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (part.sharedMesh != null && cuts.TryGetValue(part.sharedMesh, out Mesh[] parts))
                    part.sharedMesh = parts[1];
                else
                    Object.DestroyImmediate(part.gameObject);
            }
        }

        private static void Describe(SharedData shared, BootSet set, GameObject legs)
        {
            SharedData own = legs.GetComponent<ItemDrop>().m_itemData.m_shared;
            shared.m_name = set.Token;
            shared.m_description = Description;
            shared.m_armorMaterial = null;
            shared.m_equipStatusEffect = null;
            shared.m_damageModifiers = new List<HitData.DamageModPair>();
            shared.m_weight = BootsShare.LegsWeight(legs.name, own) * BootsStats.BootsShare;
            BootsStats.WriteBoots(shared, legs.name);
            Sprite icon = BootsIcons.Boots(set) ?? Cut(own);
            if (icon != null)
                shared.m_icons = new[] { icon };
        }

        // Without the workshop's icon: the lower part of the leggings' own.
        private static Sprite Cut(SharedData legs)
        {
            Sprite icon = legs.m_icons != null && legs.m_icons.Length > 0 ? legs.m_icons[0] : null;
            return SpriteCrop.Fit(icon, IconPart, "OpenKeep_boots_cut");
        }
    }
}
