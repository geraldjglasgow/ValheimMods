using System.Collections.Generic;
using System.Linq;
using BundlePrefabs;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesPack.Mountains
{
    /// <summary>Private copies retain vanilla animation events and projectile networking.</summary>
    public static class MountainAttacks
    {
        private sealed class Entry
        {
            public MountainKind Kind = null!;
            public GameObject Item = null!;
            public HitData.DamageTypes Damage;
            public string Word = "";
        }
        private static readonly List<Entry> Entries = new List<Entry>();
        public static IEnumerable<GameObject> Items => Entries.Select(e => e.Item);
        public static GameObject[] Build(MountainKind kind, Humanoid original, Harmony harmony)
        {
            var items = new List<GameObject>();
            foreach (GameObject source in original.m_defaultItems.Where(i => i != null))
            {
                if (source.GetComponent<ItemDrop>() == null) continue;
                GameObject item = PrefabBench.Copy(source, kind.Creature + "_attack_" + items.Count);
                var shared = item.GetComponent<ItemDrop>().m_itemData.m_shared;
                string word = "$item_ecp_" + kind.Id.ToLowerInvariant() + "_attack";
                shared.m_name = word;
                var entry = new Entry { Kind = kind, Item = item, Damage = Profile(kind, shared), Word = word };
                Entries.Add(entry);
                Apply(shared, entry);
                ItemPrefabs.Register(harmony, item);
                items.Add(item);
            }
            return items.ToArray();
        }
        private static HitData.DamageTypes Profile(MountainKind kind, ItemDrop.ItemData.SharedData shared)
        {
            HitData.DamageTypes damage = shared.m_damages.Clone();
            if (kind.Boss) damage.m_frost += 18f;
            if (kind.Flying)
            {
                damage.m_blunt += damage.m_frost * .65f;
                damage.m_frost *= .35f;
            }
            if (kind.Id == "IceCrawler") damage.m_frost += 3f;
            if (kind.Id == "Rimeback") shared.m_attackForce *= 1.3f;
            if (kind.Id == "Rimeback") ScaleReach(shared, kind.Scale);
            if (kind.Id == "IceCrawler") ScaleReach(shared, 1.4f);
            if (kind.Boss)
            {
                shared.m_aiAttackRange *= 1.35f;
                shared.m_attack.m_attackRange *= 1.35f;
            }
            return damage;
        }
        private static void ScaleReach(ItemDrop.ItemData.SharedData shared, float scale)
        {
            shared.m_aiAttackRange *= scale;
            shared.m_aiAttackRangeMin *= scale;
            shared.m_attack.m_attackRange *= scale;
            shared.m_attack.m_attackHeight *= scale;
            shared.m_attack.m_attackOffset *= scale;
            shared.m_attack.m_attackRayWidth *= scale;
        }
        private static void Apply(ItemDrop.ItemData.SharedData shared, Entry entry)
        {
            var damage = entry.Damage.Clone();
            damage.Modify(MountainSettings.For(entry.Kind).Damage);
            shared.m_damages = damage;
        }
        public static void Refresh()
        {
            foreach (Entry entry in Entries) Apply(entry.Item.GetComponent<ItemDrop>().m_itemData.m_shared, entry);
            foreach (Character character in Character.GetAllCharacters())
            {
                if (!(character is Humanoid humanoid)) continue;
                MountainKind? kind = MountainKind.Find(Utils.GetPrefabName(character.gameObject));
                if (kind == null) continue;
                foreach (ItemDrop.ItemData item in humanoid.GetInventory().GetAllItems())
                {
                    // Match the exact private prefab, as each vanilla rig may carry several attacks.
                    Entry? entry = Entries.FirstOrDefault(e => e.Kind == kind && item.m_dropPrefab == e.Item);
                    if (entry != null) Apply(item.m_shared, entry);
                }
            }
        }
    }
}
