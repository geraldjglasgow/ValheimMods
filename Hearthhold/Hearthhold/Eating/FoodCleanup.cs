using HarmonyLib;

namespace Hearthhold
{
    /// <summary>
    /// Drops the stars of foods that are no longer active, so the custom data and the foods agree. Foods leave in
    /// five places: UpdateFood removes one that ran out, ClearFood (the puke command, item sets) and OnDeath clear
    /// them all, RemoveOneFood (the puke status effect) removes one at random, and EatFood replaces the most depleted
    /// one (pruned by <see cref="FoodEaten"/>). Player.Load reads the foods before the custom data and skips a food
    /// whose prefab no longer exists, so the keys are pruned once after it has read both. Every prune follows one of
    /// these, so it never runs on a player whose foods have not been loaded yet.
    /// </summary>
    public static class FoodCleanup
    {
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateFood))]
        private static class Expired
        {
            [HarmonyPrefix]
            private static void Prefix(Player __instance, out int __state) => __state = __instance.m_foods.Count;

            [HarmonyPostfix]
            private static void Postfix(Player __instance, int __state)
            {
                if (__instance.m_foods.Count < __state)
                    FoodStars.Prune(__instance);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.ClearFood))]
        private static class Cleared
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance) => FoodStars.Prune(__instance);
        }

        [HarmonyPatch(typeof(Player), nameof(Player.RemoveOneFood))]
        private static class RemovedOne
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance, bool __result)
            {
                if (__result)
                    FoodStars.Prune(__instance);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
        private static class Died
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance) => FoodStars.Prune(__instance);
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Load))]
        private static class Loaded
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance) => FoodStars.Prune(__instance);
        }
    }
}
