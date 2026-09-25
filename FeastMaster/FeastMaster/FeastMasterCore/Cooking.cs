using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;

namespace FeastMaster
{
    /// <summary>The station's own values, replaced for one cooking tick.</summary>
    public sealed class CookingSwap
    {
        public bool CanOvercook;
        public float[] CookTimes;
    }

    /// <summary>
    /// Cook time and burning. UpdateCooking runs every second on every peer but cooks only on the station's ZDO
    /// owner: it adds the elapsed world time to each slot's cooked time (kept in the ZDO), marks a slot done past
    /// the recipe's m_cookTime and burnt past twice that while m_canOvercookItems is set. Both are replaced for
    /// that one call on the owner and restored afterwards, so edits reach food already on the fire.
    /// </summary>
    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.UpdateCooking))]
    public static class CookingPatch
    {
        [HarmonyPrefix]
        public static void Prefix(CookingStation __instance, out CookingSwap __state) => __state = CookingRules.Swap(__instance);

        [HarmonyFinalizer]
        public static void Finalizer(CookingStation __instance, CookingSwap __state) => CookingRules.Restore(__instance, __state);
    }

    public static class CookingRules
    {
        /// <summary>Applies the configured values on the owner; null (nothing replaced) on every other peer.</summary>
        public static CookingSwap Swap(CookingStation station)
        {
            ZNetView view = station.m_nview;
            if (view == null || !view.IsValid() || !view.IsOwner())
                return null;
            CookingSwap saved = new CookingSwap { CanOvercook = station.m_canOvercookItems };
            station.m_canOvercookItems &= Settings.FoodCanBurn.Value;
            CookTimes.TryGet(view.GetZDO().GetPrefab(), out Dictionary<ItemDrop, ConfigEntry<float>> entries);
            saved.CookTimes = SwapTimes(station.m_conversion, entries, Settings.CookTimeMultiplier.Value);
            return saved;
        }

        /// <summary>Each recipe's time becomes its station entry (or its own time) times the multiplier.</summary>
        private static float[] SwapTimes(List<CookingStation.ItemConversion> recipes, Dictionary<ItemDrop, ConfigEntry<float>> entries, float multiplier)
        {
            float[] times = new float[recipes.Count];
            for (int i = 0; i < recipes.Count; i++)
            {
                CookingStation.ItemConversion recipe = recipes[i];
                if (recipe == null)
                    continue;
                times[i] = recipe.m_cookTime;
                ConfigEntry<float> entry = null;
                bool configured = entries != null && recipe.m_from != null && entries.TryGetValue(recipe.m_from, out entry);
                recipe.m_cookTime = (configured ? entry.Value : recipe.m_cookTime) * multiplier;
            }
            return times;
        }

        public static void Restore(CookingStation station, CookingSwap saved)
        {
            if (saved == null)
                return;
            station.m_canOvercookItems = saved.CanOvercook;
            List<CookingStation.ItemConversion> recipes = station.m_conversion;
            for (int i = 0; i < saved.CookTimes.Length && i < recipes.Count; i++)
            {
                if (recipes[i] != null)
                    recipes[i].m_cookTime = saved.CookTimes[i];
            }
        }
    }
}
