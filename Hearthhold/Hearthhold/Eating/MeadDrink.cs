using HarmonyLib;

namespace Hearthhold
{
    /// <summary>
    /// Finds the status effect a player gets from drinking a starred mead and hands it to <see cref="MeadBoost"/>.
    /// Player.ConsumeItem (the inventory and hotbar) and ItemDrop.Eat both call SEMan.AddStatusEffect with the item's
    /// shared m_consumeStatusEffect before the item is removed; that overload clones the prefab, runs Setup on the clone
    /// and returns it, or returns null when the effect was already active (CanConsumeItem refuses a mead whose effect
    /// or category is active, so a drink that gets this far normally adds a new one). A scope opened around the two
    /// callers holds the starred item's effect and stars; the postfix boosts the returned clone only when it is that
    /// effect, once per drink. Outside a scope every added status effect (wet, shelter, rested...) costs one reference
    /// test. A consumable counts when it has stars and a status effect: stars only exist on Hearthhold's star items, so
    /// in practice the fermenter's meads (Kitchen.IsFermented) and any starred dish with an effect.
    /// </summary>
    public static class MeadDrink
    {
        private struct Drink
        {
            public StatusEffect Effect;
            public int Stars;
        }

        /// <summary>The starred drink being consumed; Effect is null outside a scope or once boosted.</summary>
        private static Drink current;

        private static Drink Open(ItemDrop.ItemData item)
        {
            Drink previous = current;
            StatusEffect effect = item?.m_shared?.m_consumeStatusEffect;
            int stars = effect != null ? Stars.Get(item) : 0;
            current = stars > 0 ? new Drink { Effect = effect, Stars = stars } : default;
            return previous;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.ConsumeItem))]
        private static class Consumed
        {
            [HarmonyPrefix]
            private static void Prefix(ItemDrop.ItemData item, out Drink __state) => __state = Open(item);

            [HarmonyFinalizer]
            private static void Finalizer(Drink __state) => current = __state;
        }

        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Eat))]
        private static class EatenFromGround
        {
            [HarmonyPrefix]
            private static void Prefix(ItemDrop __instance, out Drink __state) => __state = Open(__instance.m_itemData);

            [HarmonyFinalizer]
            private static void Finalizer(Drink __state) => current = __state;
        }

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.AddStatusEffect),
            typeof(StatusEffect), typeof(bool), typeof(int), typeof(float), typeof(short))]
        private static class Added
        {
            [HarmonyPostfix]
            private static void Postfix(SEMan __instance, StatusEffect statusEffect, StatusEffect __result)
            {
                if ((object)current.Effect == null || (object)statusEffect != current.Effect || __result == null)
                    return;
                if (!(__instance.m_character is Player))
                    return;
                int stars = current.Stars;
                current.Effect = null;
                HookGuard.Run("boost starred mead", () => MeadBoost.Apply(__result, stars));
            }
        }
    }
}
