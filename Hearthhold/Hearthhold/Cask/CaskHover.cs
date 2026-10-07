using System.Collections.Generic;
using HarmonyLib;

namespace Hearthhold
{
    /// <summary>
    /// The Aging Cask's hover: under the container's own text, the rule and, while anything inside still ages, the time
    /// until the next star (the soonest slot). Read from the records in the cask's ZDO, so every player sees it.
    /// </summary>
    public static class CaskHover
    {
        public const string Rule = "Meads and wines gain a star every 2 days";

        /// <summary>The rule, plus "Next star in N days" for the slot closest to its next star.</summary>
        public static string Line(List<CaskRecord> records, double now, EnvMan env)
        {
            double period = CaskAging.Period(env);
            double soonest = double.MaxValue;
            foreach (CaskRecord record in records)
            {
                if (record.Stars < Stars.Max)
                    soonest = System.Math.Min(soonest, record.Start + period - now);
            }
            if (soonest == double.MaxValue || env.m_dayLengthSec <= 0)
                return Rule;
            double days = System.Math.Max(0.0, soonest) / env.m_dayLengthSec;
            return $"{Rule}\nNext star in {days:0.0} days";
        }

        [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
        private static class HoverText
        {
            [HarmonyPostfix]
            private static void Postfix(Container __instance, ref string __result)
            {
                if (__instance.TryGetComponent(out AgingCask cask))
                    __result += "\n" + cask.HoverLine();
            }
        }
    }
}
