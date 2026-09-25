using System.Collections.Generic;
using HarmonyLib;

namespace FeastMaster
{
    /// <summary>
    /// Fermenter brew time. The barrel stores only its start time in the ZDO; GetStatus compares the elapsed world
    /// time with m_fermentationDuration on every peer (hover, visuals, interact) and on the ZDO owner before a tap.
    /// The field is replaced for that one call, so an edit reaches barrels already brewing and 0 leaves the barrel's
    /// own time.
    /// </summary>
    [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.GetStatus))]
    public static class FermentationTimePatch
    {
        [HarmonyPrefix]
        public static void Prefix(Fermenter __instance, out ScaledFields __state)
        {
            __state = default;
            float time = Settings.FermentationTime.Value;
            if (time > 0f)
                __state = CostRules.Swap(ref __instance.m_fermentationDuration, time);
        }

        [HarmonyFinalizer]
        public static void Finalizer(Fermenter __instance, ScaledFields __state)
        {
            CostRules.Restore(ref __instance.m_fermentationDuration, __state);
        }
    }

    /// <summary>
    /// Fermenter batch yield: every conversion's m_producedItems replaced for one call and restored afterwards.
    /// The batch is spawned on the barrel's ZDO owner in DelayedTap, 1.5 s after a tap. (The game's DropAllItems
    /// also reads the count but nothing calls it: a destroyed barrel loses its batch.)
    /// </summary>
    public static class FermenterYield
    {
        public static int[] Swap(Fermenter fermenter)
        {
            int yield = Settings.BatchYield.Value;
            if (yield <= 0)
                return null;
            List<Fermenter.ItemConversion> conversions = fermenter.m_conversion;
            int[] saved = new int[conversions.Count];
            for (int i = 0; i < conversions.Count; i++)
            {
                saved[i] = conversions[i].m_producedItems;
                conversions[i].m_producedItems = yield;
            }
            return saved;
        }

        public static void Restore(Fermenter fermenter, int[] saved)
        {
            if (saved == null)
                return;
            List<Fermenter.ItemConversion> conversions = fermenter.m_conversion;
            for (int i = 0; i < saved.Length && i < conversions.Count; i++)
                conversions[i].m_producedItems = saved[i];
        }
    }

    [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.DelayedTap))]
    public static class TapYieldPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Fermenter __instance, out int[] __state) => __state = FermenterYield.Swap(__instance);

        [HarmonyFinalizer]
        public static void Finalizer(Fermenter __instance, int[] __state) => FermenterYield.Restore(__instance, __state);
    }
}
