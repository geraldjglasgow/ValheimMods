using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Makes room for legendary fish in the item database. Every fish item has a maximum quality of 5, and the game counts
    /// recipe ingredients only up to an item's maximum (Player.HaveRequirementItems, Player.GetFirstRequiredItem), so a
    /// level 6 fish could not be cleaned. When the item database wakes, every item with a Fish component gets a maximum
    /// of at least 6. Nothing else reads it for fish: no recipe upgrades a fish, stacks already keep levels apart, and the
    /// tooltip already shows a fish's quality. Cleaning a legendary fish then gives the game's own amount for its level
    /// (1 + 3 x 5 raw fish, plus the species' extra). It happens whatever the Fishing switch says, so a legendary fish
    /// caught earlier can still be cleaned after Fishing is turned off.
    /// </summary>
    public static class LegendaryItems
    {
        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        private static class DatabaseAwake
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix() => HookGuard.Run("legendary fish items", Widen);
        }

        /// <summary>Every fish item of the database that just woke (<see cref="FishInfo.Species"/>).</summary>
        private static void Widen()
        {
            foreach (GameObject prefab in FishInfo.Species())
            {
                ItemDrop item = prefab.GetComponent<ItemDrop>();
                if (item.m_itemData.m_shared.m_maxQuality < FishInfo.LegendaryLevel)
                    item.m_itemData.m_shared.m_maxQuality = FishInfo.LegendaryLevel;
            }
        }
    }
}
