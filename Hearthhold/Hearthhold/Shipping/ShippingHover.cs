using System;
using HarmonyLib;
using Translations;

namespace Hearthhold
{
    /// <summary>
    /// The Shipping Crate's hover text: what selling does, what the contents would fetch now and the last sale (from the
    /// ZDO, so every player sees it). The value is worked out again only when the container loads a new revision or the
    /// words change.
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
    public static class ShippingHover
    {
        private static Container cachedFor;
        private static uint cachedRevision;
        private static int cachedVersion = -1;
        private static string cachedValue = "";
        private static Container hovered;
        private static readonly Func<string> lines = () => Lines(hovered);

        [HarmonyPostfix]
        private static void Postfix(Container __instance, ref string __result)
        {
            if (!ShippingPiece.On || __instance == null || !__instance.TryGetComponent(out ShippingCrate _))
                return;
            hovered = __instance;
            __result += HookGuard.Run("shipping crate hover", lines, "");
            hovered = null;
        }

        private static string Lines(Container container)
        {
            string text = "\n<color=#c8b48c>" + Lang.Get("hh_crate_hover") + "</color>" + Value(container);
            ZDO zdo = container.m_nview != null && container.m_nview.IsValid() ? container.m_nview.GetZDO() : null;
            int items = zdo != null ? zdo.GetInt(ShippingKeys.SoldItems) : 0;
            if (items > 0)
                text += "\n" + Lang.Format("hh_crate_last_sale", items, zdo.GetInt(ShippingKeys.SoldCoins));
            return text;
        }

        private static string Value(Container container)
        {
            if (container == cachedFor && container.m_lastRevision == cachedRevision && cachedVersion == Lang.Version)
                return cachedValue;
            cachedFor = container;
            cachedRevision = container.m_lastRevision;
            cachedVersion = Lang.Version;
            (int items, int coins) = ShippingPrices.Total(container.GetInventory());
            cachedValue = items > 0 ? "\n" + Lang.Format("hh_crate_at_dawn", items, coins) : "";
            return cachedValue;
        }
    }
}
