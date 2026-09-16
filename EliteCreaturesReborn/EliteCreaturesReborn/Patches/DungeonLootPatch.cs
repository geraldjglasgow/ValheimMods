using System;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using HarmonyLib;
using PatchGuard;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Regenerates a dungeon chest's loot on its own slower timer, so a crypt is worth walking back into. The chest
    /// refills only when it has been emptied, only inside a generated room, and only once the timer has passed since
    /// it was last filled - a chest a player is still working through is never topped up under them.
    ///
    /// OWNER ONLY, at the moment the chest loads: the same point the game itself fills a fresh chest, so a returning
    /// player finds it already stocked rather than watching items appear. The fill time lives in the chest's own ZDO,
    /// which the game replicates and saves, so the clock survives a restart and cannot be reset by another machine.
    /// </summary>
    [HarmonyPatch(typeof(Container), "Awake")]
    public static class DungeonLootPatch
    {
        private static void Postfix(Container __instance) =>
            Guard.Run("Container.Awake loot regen", () => Consider(__instance));

        private static void Consider(Container container)
        {
            RespawnRules rules = RuleState.Active.Respawn;
            if (!rules.DungeonLoot || container == null || !Eligible(container))
            {
                return;
            }
            ZDO zdo = container.m_nview.GetZDO();
            if (!Due(zdo, rules.DungeonLootDays))
            {
                return;
            }
            Refill(container, zdo);
        }

        private static bool Eligible(Container container)
        {
            return container.m_nview != null && container.m_nview.IsValid() && container.m_nview.IsOwner()
                && container.m_defaultItems != null && container.m_defaultItems.m_drops.Count > 0
                && container.GetInventory() != null && container.GetInventory().NrOfItems() == 0
                && SpawnerRespawnPatch.InDungeon(container);
        }

        /// <summary>True once the configured world days have passed since the last fill. An unstamped chest is stamped now.</summary>
        private static bool Due(ZDO zdo, float days)
        {
            long ticks = zdo.GetLong(TraitKeys.LootFilledAt, 0L);
            DateTime now = ZNet.instance.GetTime();
            if (ticks == 0L)
            {
                zdo.Set(TraitKeys.LootFilledAt, now.Ticks); // first sight: start its clock, do not refill
                return false;
            }
            return (now - new DateTime(ticks)).TotalDays >= days;
        }

        // Adds the chest's own drop table again, exactly as the game does when it first fills it, then restamps the
        // clock and saves so the new contents and the new time replicate together.
        private static void Refill(Container container, ZDO zdo)
        {
            foreach (ItemDrop.ItemData item in container.m_defaultItems.GetDropListItems())
            {
                container.GetInventory().AddItem(item);
            }
            zdo.Set(TraitKeys.LootFilledAt, ZNet.instance.GetTime().Ticks);
            container.Save();
            Log.Diag($"{container.m_name}: dungeon loot regenerated");
        }
    }
}
