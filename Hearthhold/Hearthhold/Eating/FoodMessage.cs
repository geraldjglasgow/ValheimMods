using HarmonyLib;

namespace Hearthhold
{
    /// <summary>
    /// The eat message with a starred dish's boosted values. Player.EatFood (and FeastMaster's copy of it for extra
    /// food slots) sends " +X $item_food_health  +Y $item_food_stamina  +Z $item_food_eitr " as a Center message
    /// through Player.Message, built from the item's base values. While a starred dish is being eaten, that exact
    /// text is swapped for the same text with each value raised by the dish's star bonus, its full-strength values.
    /// Any other message, and an eat message another mod has already changed, passes untouched.
    /// </summary>
    public static class FoodMessage
    {
        /// <summary>The starred dish being eaten, or null.</summary>
        private static ItemDrop.ItemData eating;

        [HarmonyPatch(typeof(Player), nameof(Player.EatFood))]
        private static class Scope
        {
            // First, so the scope is open before a prefix that eats in the game's place (FeastMaster's food slots).
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(ItemDrop.ItemData item, out ItemDrop.ItemData __state)
            {
                __state = eating;
                eating = Stars.Get(item) > 0 ? item : null;
            }

            // Restores the outer scope: auto-eating can eat again inside EatFood's own food update.
            [HarmonyFinalizer]
            private static void Finalizer(ItemDrop.ItemData __state) => eating = __state;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Message))]
        private static class Rewrite
        {
            [HarmonyPrefix]
            private static void Prefix(MessageHud.MessageType type, ref string msg)
            {
                if (eating?.m_shared == null || type != MessageHud.MessageType.Center)
                    return;
                if (msg == Text(eating.m_shared, 0f))
                    msg = Text(eating.m_shared, EatBonus.Food(Stars.Get(eating)));
            }
        }

        /// <summary>The eat message as the game builds it; with a bonus, each value raised and rounded to one decimal.</summary>
        private static string Text(ItemDrop.ItemData.SharedData shared, float bonus)
        {
            return Part(shared.m_food, bonus, "$item_food_health")
                + Part(shared.m_foodStamina, bonus, "$item_food_stamina")
                + Part(shared.m_foodEitr, bonus, "$item_food_eitr");
        }

        private static string Part(float value, float bonus, string key)
        {
            if (value <= 0f)
                return "";
            string number = bonus > 0f ? EatBonus.Boosted(value, bonus).ToString("0") : value.ToString();
            return " +" + number + " " + key + " ";
        }
    }
}
