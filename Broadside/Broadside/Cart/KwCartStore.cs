using HarmonyLib;
using PatchGuard;

namespace Broadside
{
    /// <summary>
    /// The game's cart without its own storage (2026-11-03, the user's decision: the cart loses its storage and carries
    /// crates; a cart that already holds items keeps them reachable until emptied, so nothing is lost). Its Container stays
    /// on it (the game's, its items in the cart's ZDO), but nothing goes in any more, by any way the game puts an item in
    /// (<see cref="KwCartBed.Refuses"/>, asked by <see cref="KwPileFilter"/>'s inventory patches, never while the Container
    /// loads from its ZDO): while it holds goods, E opens it to take them out, its hover saying so; once empty, E on its load
    /// space is the cart's own use (taking hold of it, as on the cart's frame) and the hover is the cart's, with a line on how
    /// it carries goods now. A crate riding the cart adds its unload line to its own hover (<see cref="KwCartUnload"/>).
    /// One postfix on <c>Container.GetHoverText</c> for both, leaving at a field read for every other chest.
    /// </summary>
    [HarmonyPatch]
    public static class KwCartStore
    {
        /// <summary>
        /// From <c>Container.Interact</c>'s prefix: an empty cart storage hands the use to the cart itself. Whether it did
        /// (<paramref name="result"/> the cart's answer).
        /// </summary>
        public static bool Forward(Container box, Humanoid who, bool hold, bool alt, out bool result)
        {
            result = false;
            if (box.m_wagon == null || KwCartBed.Stores == 0 || !KwCartBed.Refuses(box.GetInventory()) || box.GetInventory().NrOfItems() > 0)
                return false;
            result = box.m_wagon.Interact(who, hold, alt);
            return true;
        }

        [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText)), HarmonyPostfix]
        private static void Hover(Container __instance, ref string __result)
        {
            if (__instance.m_wagon != null)
                __result = Guard.Run("Broadside cart storage hover", static (box, text) => Storage(box, text), __instance, __result);
            else if (KwCartBed.Carried > 0 && __instance.TryGetComponent(out KwCartRider rider) && rider.On != null)
                __result += Localization.instance.Localize(KwCartText.UnloadLine);
        }

        // The cart's storage: emptied, the cart's own hover; holding goods, open to take them out; another cart's, the game's.
        private static string Storage(Container box, string text)
        {
            if (KwCartBed.Stores == 0 || !KwCartBed.Refuses(box.GetInventory()))
                return text;
            if (box.GetInventory().NrOfItems() == 0)
                return Localization.instance.Localize(KwCartText.Empty(box.m_wagon.GetHoverText()));
            return Localization.instance.Localize(KwCartText.Draining(box.m_name));
        }
    }
}
