using System.Collections.Generic;
using EliteCrafting.Core;
using HarmonyLib;
using UnityEngine;

namespace EliteCrafting.Items
{
    /// <summary>
    /// The load warning of rarity.md section 2 (DECISIONS.md RAR-6): every item that resolves to a slot but stacks
    /// (another mod raised its max stack size) is named in the log once per prefab. Stack sizes are not forced back;
    /// such an item is simply no magic base, and the item-state writer refuses it. Scanned when an object database is
    /// set up (every peer) and again when the local player spawns (a client), after other mods' synced settings apply.
    /// </summary>
    [HarmonyPatch]
    internal static class StackableGearWarning
    {
        private static readonly HashSet<string> Warned = new HashSet<string>(System.StringComparer.Ordinal);

        public static void Scan(ObjectDB? db)
        {
            if (db?.m_items == null)
            {
                return;
            }
            foreach (GameObject prefab in db.m_items)
            {
                ItemDrop? drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (drop != null && IsStackableGear(drop.m_itemData) && Warned.Add(prefab!.name))
                {
                    Log.Warn($"{prefab.name} ({drop.m_itemData.m_shared.m_name}) is gear but stacks to "
                        + $"{drop.m_itemData.m_shared.m_maxStackSize}: it cannot become magic, and magic copies of it "
                        + "can lose their affixes when the game merges them into a stack");
                }
            }
        }

        private static bool IsStackableGear(ItemDrop.ItemData item) =>
            item.m_shared != null && item.m_shared.m_maxStackSize > 1
            && ItemSlots.Classify(item).Slot != ItemSlot.None && !ItemSlots.IsStone(item);

        // Local player's client only.
        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        [HarmonyPostfix]
        private static void PlayerSpawned(Player __instance)
        {
            if (__instance == Player.m_localPlayer)
            {
                Scan(ObjectDB.instance);
            }
        }
    }
}
