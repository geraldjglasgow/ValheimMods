using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DevBridge.Stage
{
    /// <summary>
    /// What a game creature carries, shown on its still copy through its VisEquipment, slot by slot as Humanoid equips
    /// it: its own gear (its default items and the first of each random weapon, shield, armour and set), none, or the
    /// game items named (a Skeleton holding the game's Battleaxe, say). Only items the ObjectDB knows can be drawn
    /// (VisEquipment looks them up there); creature-only attack items with no model are left out.
    /// </summary>
    internal static class Gear
    {
        /// <summary>Equips the copy by the spec (null or 1: its own; 0: none; else item names); says what was shown and what not.</summary>
        internal static Dictionary<string, List<string>> Wear(GameObject copy, GameObject prefab, string spec)
        {
            var result = new Dictionary<string, List<string>> { ["shown"] = new List<string>(), ["left out"] = new List<string>() };
            VisEquipment vis = copy.GetComponent<VisEquipment>();
            if (!vis || spec == "0" || spec == "false" || spec == "no") return result;
            bool own = spec == null || spec == "1" || spec == "true" || spec == "yes";
            IEnumerable<GameObject> items = own ? Own(prefab.GetComponent<Humanoid>()) : Named(spec);
            foreach (GameObject item in items)
                result[Equip(vis, item) ? "shown" : "left out"].Add(item.name);
            vis.UpdateVisuals(); // attach now rather than next frame, so the row measures the copy with its gear
            return result;
        }

        private static IEnumerable<GameObject> Own(Humanoid humanoid)
        {
            if (!humanoid || humanoid is Player) return Enumerable.Empty<GameObject>();
            IEnumerable<GameObject> firsts = new[] { humanoid.m_randomShield, humanoid.m_randomWeapon, humanoid.m_randomArmor }
                .Where(list => list != null && list.Length > 0).Select(list => list[0]);
            IEnumerable<GameObject> set = humanoid.m_randomSets != null && humanoid.m_randomSets.Length > 0
                ? humanoid.m_randomSets[0].m_items : Enumerable.Empty<GameObject>();
            return (humanoid.m_defaultItems ?? new GameObject[0]).Concat(firsts).Concat(set).Where(i => i).Distinct();
        }

        private static IEnumerable<GameObject> Named(string names) =>
            names.Split(',').Select(n => n.Trim()).Where(n => n.Length > 0).Select(GamePrefabs.Require).ToList();

        private static bool Equip(VisEquipment vis, GameObject item)
        {
            ItemDrop drop = item.GetComponent<ItemDrop>();
            if (!drop || !ObjectDB.instance || !ObjectDB.instance.GetItemPrefab(item.name)) return false;
            int hash = item.name.GetStableHashCode();
            switch (drop.m_itemData.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.Tool:
                case ItemDrop.ItemData.ItemType.Torch:
                case ItemDrop.ItemData.ItemType.Attach_Atgeir: vis.SetRightItem(hash, 1); return true;
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.Shield:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft: vis.SetLeftItem(hash, 0, 1); return true;
                case ItemDrop.ItemData.ItemType.Helmet: vis.SetHelmetItem(hash); return true;
                case ItemDrop.ItemData.ItemType.Chest: vis.SetChestItem(hash); return true;
                case ItemDrop.ItemData.ItemType.Legs: vis.SetLegItem(hash); return true;
                case ItemDrop.ItemData.ItemType.Shoulder: vis.SetShoulderItem(hash, 0, 1); return true;
                case ItemDrop.ItemData.ItemType.Utility: vis.SetUtilityItem(hash); return true;
                default: return false;
            }
        }
    }
}
