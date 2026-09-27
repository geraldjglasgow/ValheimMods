using HarmonyLib;
using OpenKeep.Core;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Fireplace.GetHoverText</c>: a torch put out for the day says so ("Lights at nightfall"), since fuel added by
    /// hand does not light it and vanilla torches have no switch. Every client reads the replicated ZDO; hover only.
    /// </summary>
    [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.GetHoverText))]
    public static class TorchHoverPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Fireplace __instance, ref string __result)
        {
            if (!string.IsNullOrEmpty(__result) && TorchSwitch.OutForDay(__instance))
                __result += "\n" + Language.Localize(TorchFeature.NightfallWord);
        }
    }
}
