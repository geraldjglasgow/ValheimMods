using System.Collections.Generic;
using EliteCraftingLink;

namespace EliteEquipment.Boots
{
    /// <summary>
    /// The worn boots as EliteCrafting equipment. EliteCrafting counts the game's equipment fields and its providers'
    /// items, and boots are worn outside the fields (<see cref="WornBoots"/>), so without this an inscription on worn
    /// boots did nothing. Putting a pair on or off runs the game's SetupEquipment, and a write to a worn pair is seen as
    /// equipped (the game's IsItemEquiped answers yes for it), so EliteCrafting rebuilds on its own. The provider answers
    /// from one reused list: EliteCrafting reads it at once and keeps nothing. Silent without EliteCrafting.
    /// </summary>
    public static class WornBootsLink
    {
        private const string ProviderId = "EliteEquipment.Boots";
        private static readonly List<ItemDrop.ItemData> answer = new List<ItemDrop.ItemData>(1);

        public static void Register()
        {
            if (CraftingLink.Present && !CraftingHooks.RegisterEquipmentProvider(ProviderId, Provide))
                Plugin.Log.LogWarning("EliteCrafting refused the worn boots as equipment; their inscriptions do not count (see its log).");
        }

        private static List<ItemDrop.ItemData> Provide(Player player)
        {
            answer.Clear();
            ItemDrop.ItemData pair = WornBoots.Of(player);
            if (pair != null)
                answer.Add(pair);
            return answer;
        }
    }
}
