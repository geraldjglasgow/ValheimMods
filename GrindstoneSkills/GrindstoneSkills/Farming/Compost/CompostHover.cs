using System.Globalization;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A compost bin's hover (the game's container hover: its name and the open key) also shows its compost, e.g.
    /// "Compost 23 / 100", read from the bin's ZDO on the viewing client.
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
    public static class CompostHover
    {
        [HarmonyPostfix]
        private static void Postfix(Container __instance, ref string __result)
        {
            CompostBin bin = __instance.GetComponentInParent<CompostBin>();
            if (bin == null || !bin.IsValid || string.IsNullOrEmpty(__result))
                return;
            int points = Mathf.FloorToInt(bin.Points);
            __result += "\nCompost " + points.ToString(CultureInfo.InvariantCulture) + " / " + CompostBin.Capacity.ToString(CultureInfo.InvariantCulture);
            if (!CompostBin.Working)
                __result += " (resting)";
        }
    }
}
