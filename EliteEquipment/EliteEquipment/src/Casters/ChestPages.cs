using HarmonyLib;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Pages in dungeon chests (features/casters.md, "The spellbook", "Where pages come from"): a chest with loot of its own
    /// (the game's <c>m_defaultItems</c>, which a built chest has none of) inside a dungeon (the game builds them high above
    /// the world, above <see cref="DungeonHeight"/>) gets a page 10% of the time when its loot is first made
    /// (<c>Container.AddDefaultItems</c>, once per chest on its owner as it wakes; each item put in saves the chest's
    /// inventory to its ZDO, so every player opens the same chest). The page from <see cref="PageDrops"/>; a full chest
    /// gets none. Besides it, each chest-only page (<see cref="Spell.ChestOnly"/>: Revive's 2%) is rolled on its own in
    /// every dungeon chest, whatever the biome. Only while the switch is on.
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.AddDefaultItems))]
    public static class ChestPages
    {
        private const float Chance = 0.1f;
        private const float DungeonHeight = 3000f;

        [HarmonyPostfix]
        private static void Postfix(Container __instance)
        {
            if (CastersSettings.On)
                CasterSafe.Run("chest pages", () => Add(__instance));
        }

        private static void Add(Container chest)
        {
            if (!InDungeon(chest))
                return;
            if (PageDrops.Roll(Chance))
                Put(chest, PageDrops.Pick(chest.transform.position));
            foreach (Spell spell in Spells.All)
            {
                if (spell.ChestOnly > 0f && PageDrops.Roll(spell.ChestOnly))
                    Put(chest, ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(spell.Item) : null);
            }
        }

        private static void Put(Container chest, GameObject page)
        {
            if (page != null && chest.m_inventory.CanAddItem(page, 1))
                chest.m_inventory.AddItem(page, 1);
        }

        private static bool InDungeon(Container chest) =>
            chest.transform.position.y > DungeonHeight && chest.m_inventory != null
            && chest.m_defaultItems != null && chest.m_defaultItems.m_drops.Count > 0;
    }
}
