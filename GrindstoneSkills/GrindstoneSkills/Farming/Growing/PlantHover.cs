using System.Text;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A plant's hover (Plant.GetHoverText, the game's name and health status) gets, while Farming is on:
    /// <list type="bullet">
    /// <item>when it is healthy, the time until it ripens ("Ripe in 12 min", "Ripe soon") with Farming's speed;</item>
    /// <item>"Fertilized" when a compost bin fed it;</item>
    /// <item>for a plant a player planted, "[E] Tend" or "Tended today" while tending is on.</item>
    /// </list>
    /// Everything is read from the plant's ZDO on the viewing client. The lines are kept for the plant in view and
    /// rebuilt after 0.25 s, or at once when its ZDO changes (tended, fertilized).
    /// </summary>
    [HarmonyPatch(typeof(Plant), nameof(Plant.GetHoverText))]
    public static class PlantHover
    {
        private const float RefreshSeconds = 0.25f;

        private static readonly StringBuilder text = new StringBuilder();
        private static Plant lastPlant;
        private static uint lastRevision;
        private static string lastLines = "";
        private static float refreshAt;

        [HarmonyPostfix]
        private static void Postfix(Plant __instance, ref string __result)
        {
            if (FarmSkill.Active && !string.IsNullOrEmpty(__result))
                __result += HookGuard.Run("Farming plant hover", static plant => Cached(plant), __instance, "");
        }

        /// <summary>The lines for this plant, rebuilt when the plant or its ZDO changed or the refresh time passed.</summary>
        private static string Cached(Plant plant)
        {
            ZDO zdo = PlantKeys.Of(plant);
            if (zdo == null)
                return "";
            float now = Time.time;
            if (ReferenceEquals(plant, lastPlant) && zdo.DataRevision == lastRevision && now < refreshAt)
                return lastLines;
            lastPlant = plant;
            lastRevision = zdo.DataRevision;
            refreshAt = now + RefreshSeconds;
            lastLines = "";
            lastLines = Lines(plant, zdo);
            return lastLines;
        }

        private static string Lines(Plant plant, ZDO zdo)
        {
            text.Clear();
            if (plant.GetStatus() == Plant.Status.Healthy)
                text.Append('\n').Append(Ripening(PlantClock.Left(plant)));
            if (PlantKeys.Fed(zdo))
                text.Append("\nFertilized");
            string tend = Tending.Enabled && PlantKeys.IsPlanted(zdo) ? TendLine(plant, zdo) : "";
            if (tend.Length > 0)
                text.Append('\n').Append(tend);
            return text.ToString();
        }

        private static string Ripening(double seconds)
        {
            if (seconds <= 0.0)
                return "Ripe soon";
            if (seconds < 60.0)
                return "Ripe in under a minute";
            int minutes = Mathf.CeilToInt((float)(seconds / 60.0));
            return minutes < 60 ? $"Ripe in {minutes} min" : $"Ripe in {minutes / 60} h {minutes % 60} min";
        }

        private static string TendLine(Plant plant, ZDO zdo)
        {
            int day = EnvMan.instance != null ? EnvMan.instance.GetDay() : 0;
            if (PlantKeys.TendedDay(zdo) >= day)
                return "Tended today";
            return Tending.CanTend(plant, zdo, day) ? Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] Tend") : "";
        }
    }
}
