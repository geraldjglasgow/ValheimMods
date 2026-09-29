using System.Collections.Generic;
using System.Linq;
using BundlePrefabs;
using UnityEngine;

namespace EliteCreaturesPack.Swamp
{
    /// <summary>Private copies of vanilla attack items preserve animation events, hit detection and network ownership.</summary>
    public static class SwampAttacks
    {
        private static readonly Dictionary<string, (SwampKind kind, GameObject item, HitData.DamageTypes damage)> Attacks =
            new Dictionary<string, (SwampKind, GameObject, HitData.DamageTypes)>();
        public static IEnumerable<GameObject> Items => Attacks.Values.Select(value => value.item);

        public static GameObject[] Build(SwampKind kind, Humanoid source, ZNetScene scene)
        {
            IEnumerable<GameObject> originals = source.m_defaultItems.Concat(source.m_randomWeapon)
                .Concat(source.m_randomSets.SelectMany(set => set.m_items));
            originals = originals.Where(item => item != null && item.GetComponent<ItemDrop>() != null).Distinct();
            if (kind.Id == "ReedStalker")
                originals = originals.Where(item => Melee(item.GetComponent<ItemDrop>().m_itemData.m_shared)).Take(1);
            return originals.Select((item, index) => Copy(kind, item, index, scene)).ToArray();
        }

        private static bool Melee(ItemDrop.ItemData.SharedData shared) =>
            shared.m_damages.GetTotalDamage() > 0f && shared.m_attack.m_attackType != Attack.AttackType.Projectile;

        private static GameObject Copy(SwampKind kind, GameObject source, int index, ZNetScene scene)
        {
            GameObject item = PrefabBench.Copy(source, kind.Creature + "_attack_" + index);
            ItemDrop.ItemData.SharedData shared = item.GetComponent<ItemDrop>().m_itemData.m_shared;
            shared.m_name = "$" + kind.Word + "_attack_" + index;
            Shape(kind, shared);
            if (kind.Id == "ReedStalker") Spear(item, scene.GetPrefab("SpearBronze"));
            Attacks[shared.m_name] = (kind, item, shared.m_damages.Clone());
            Apply(shared);
            return item;
        }

        private static void Shape(SwampKind kind, ItemDrop.ItemData.SharedData shared)
        {
            shared.m_attack.m_attackRange *= kind.Scale;
            shared.m_aiAttackRange *= kind.Scale;
            if (kind.Jarl)
            {
                shared.m_attackForce *= 1.7f;
                shared.m_aiAttackInterval *= 1.25f;
            }
            if (kind.Id == "ReedStalker") Hunter(shared);
            if (kind.Id == "BogMaw") shared.m_aiAttackInterval *= 1.35f;
            if (kind.Id == "FenCrawler") shared.m_aiAttackInterval *= 0.8f;
            if (kind.Night) shared.m_attackForce *= 1.5f;
        }

        private static void Hunter(ItemDrop.ItemData.SharedData shared)
        {
            float physical = shared.m_damages.m_slash + shared.m_damages.m_blunt + shared.m_damages.m_pierce;
            shared.m_damages.m_slash = shared.m_damages.m_blunt = 0f;
            shared.m_damages.m_pierce = physical;
            shared.m_attack.m_attackRange = 2.7f;
            shared.m_aiAttackRange = 2.5f;
            shared.m_attackForce *= 0.6f;
            shared.m_aiAttackInterval = 3.5f;
        }

        private static void Spear(GameObject item, GameObject? spear)
        {
            Transform? slot = item.transform.Find("attach");
            Transform? model = spear?.transform.Find("attach");
            if (slot == null || model == null) return;
            foreach (Transform child in slot.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            foreach (Renderer renderer in slot.GetComponents<Renderer>()) Object.DestroyImmediate(renderer);
            Object.Instantiate(model.gameObject, slot, false).name = "ecp_reed_spear";
        }

        public static void Apply(ItemDrop.ItemData.SharedData shared)
        {
            if (!Attacks.TryGetValue(shared.m_name, out var data)) return;
            shared.m_damages = data.damage.Clone();
            shared.m_damages.Modify(SwampSettings.For(data.kind).Damage);
        }

        public static void Refresh()
        {
            foreach (GameObject item in Items) Apply(item.GetComponent<ItemDrop>().m_itemData.m_shared);
            foreach (Character character in Character.GetAllCharacters())
            {
                if (character is Humanoid humanoid && SwampKind.Find(Utils.GetPrefabName(character.gameObject)) != null)
                    foreach (ItemDrop.ItemData item in humanoid.GetInventory().GetAllItems()) Apply(item.m_shared);
            }
        }
    }
}
