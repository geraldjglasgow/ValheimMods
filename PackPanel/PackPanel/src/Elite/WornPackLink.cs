using System.Collections.Generic;
using EliteCraftingLink;
using PackPanel.Backpacks;

namespace PackPanel.Elite
{
    /// <summary>
    /// The worn backpack as EliteCrafting equipment. EliteCrafting counts the game's equipment fields only, and a pack is
    /// worn in PackPanel's Backpack slot, never as the game's utility, so an equipment provider names it: the pack in the
    /// local player's Backpack slot (<see cref="Backpack.WornItem"/>; none for any other player, whose totals EliteCrafting
    /// does not compute here). Its inscriptions then count like worn gear (Broad Back's carry weight, Pack Mule, Magpie,
    /// Harvester). EliteCrafting is told to rebuild the player's totals when the worn pack changes (checked each frame by
    /// <see cref="BackpackWear"/>) and when a write changes the worn pack's inscriptions (its item-changed listener).
    /// The provider answers from one reused list: EliteCrafting reads it at once and keeps nothing.
    /// </summary>
    public static class WornPackLink
    {
        private static readonly List<ItemDrop.ItemData> answer = new List<ItemDrop.ItemData>(1);
        private static ItemDrop.ItemData lastWorn;

        internal static void Register(List<string> refused)
        {
            EliteSetup.Expect(CraftingHooks.RegisterEquipmentProvider(EliteDefinitions.ProviderId, Provide), "the worn pack as equipment", refused);
            EliteSetup.Expect(CraftingHooks.AddItemChangedListener(OnItemChanged), "the item-changed listener", refused);
        }

        /// <summary>The local player's frame, with the pack it wears now (null for none).</summary>
        public static void Follow(Player player, ItemDrop.ItemData worn)
        {
            if (worn == lastWorn)
                return;
            lastWorn = worn;
            CraftingHooks.InvalidatePlayer(player);
        }

        private static List<ItemDrop.ItemData> Provide(Player player)
        {
            answer.Clear();
            ItemDrop.ItemData pack = Backpack.WornItem(player);
            if (pack != null)
                answer.Add(pack);
            return answer;
        }

        private static void OnItemChanged(ItemDrop.ItemData item, string reason)
        {
            if (item != null && item == lastWorn && Player.m_localPlayer != null)
                CraftingHooks.InvalidatePlayer(Player.m_localPlayer);
        }
    }
}
