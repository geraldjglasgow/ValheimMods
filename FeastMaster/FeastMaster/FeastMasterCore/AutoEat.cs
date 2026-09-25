using HarmonyLib;

namespace FeastMaster
{
    /// <summary>
    /// Auto Eat: when a food runs out, another of the same food is eaten from the inventory, if there is one.
    /// Player.UpdateFood counts every food down by one second per tick and removes the first that reaches 0, so
    /// the first food at or below one second before the call is the one that can run out; if it is gone after the
    /// call, the same food (matched by its shared name) is eaten through Humanoid.UseItem, the inventory's path,
    /// with its sound and animation. Local player only; foods live on the player's own client. The server's
    /// Allow Auto Eat and the player's own Auto Eat must both be on.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateFood))]
    public static class AutoEatPatch
    {
        // Eating calls UpdateFood again (forced); that nested call must not start a second auto-eat.
        private static bool eating;

        [HarmonyPrefix]
        public static void Prefix(Player __instance, out Player.Food __state)
        {
            __state = eating ? null : Expiring(__instance);
        }

        [HarmonyPostfix]
        public static void Postfix(Player __instance, Player.Food __state)
        {
            if (__state == null || __instance.m_foods.Contains(__state))
                return;
            Inventory inventory = __instance.GetInventory();
            ItemDrop.ItemData item = inventory.GetItem(__state.m_item.m_shared.m_name);
            if (item == null)
                return;
            eating = true;
            try
            {
                // As from the inventory: otherwise UseItem would use the food on whatever the player is looking at.
                __instance.UseItem(inventory, item, fromInventoryGui: true);
            }
            finally
            {
                eating = false;
            }
        }

        private static Player.Food Expiring(Player player)
        {
            if (player != Player.m_localPlayer || !Settings.AllowAutoEat.Value || !Settings.AutoEat.Value)
                return null;
            foreach (Player.Food food in player.m_foods)
            {
                if (food.m_time <= 1f)
                    return food;
            }
            return null;
        }
    }
}
