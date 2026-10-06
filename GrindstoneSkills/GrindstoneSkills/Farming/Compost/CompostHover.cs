using System.Globalization;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A compost bin's hover (the game's container hover: its name and the open key) also shows its compost, e.g.
    /// "Compost 23 / 100", read from the bin's ZDO on the viewing client. The hover runs every frame: the bin of the
    /// container last looked at is found once, and its line is rebuilt only when the numbers change.
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
    public static class CompostHover
    {
        private static Container lastContainer;
        private static CompostBin lastBin;
        private static int lastPoints = -1;
        private static int lastCapacity = -1;
        private static bool lastWorking;
        private static string lastLine = "";

        [HarmonyPostfix]
        private static void Postfix(Container __instance, ref string __result)
        {
            CompostBin bin = BinOf(__instance);
            if (bin == null || !bin.IsValid || string.IsNullOrEmpty(__result))
                return;
            __result += Line(Mathf.FloorToInt(bin.Points), CompostBin.Capacity, CompostBin.Working);
        }

        private static CompostBin BinOf(Container container)
        {
            if (!ReferenceEquals(container, lastContainer))
            {
                lastContainer = container;
                lastBin = container.GetComponentInParent<CompostBin>();
            }
            return lastBin;
        }

        private static string Line(int points, int capacity, bool working)
        {
            if (points == lastPoints && capacity == lastCapacity && working == lastWorking)
                return lastLine;
            lastPoints = points;
            lastCapacity = capacity;
            lastWorking = working;
            lastLine = "\nCompost " + points.ToString(CultureInfo.InvariantCulture) + " / " + capacity.ToString(CultureInfo.InvariantCulture)
                + (working ? "" : " (resting)");
            return lastLine;
        }
    }
}
