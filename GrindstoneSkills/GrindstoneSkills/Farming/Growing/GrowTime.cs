using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Faster growth. Plant.GetGrowTime gives the plant's seeded grow time (between m_growTime and m_growTimeMax); the
    /// game compares the time since planting with it on every client (the half-grown look) and on the owner (growing).
    /// The postfix divides it by the plant's speed, read from its ZDO so every client agrees: the planter's share of
    /// Growth Speed At Level 100 for anything a player planted (tree saplings too), plus Compost Growth Speed for a
    /// fertilized crop. Rain and tending add growth by moving the plant's clock instead (<see cref="PlantClock"/>).
    /// </summary>
    [HarmonyPatch(typeof(Plant), nameof(Plant.GetGrowTime))]
    public static class GrowTime
    {
        [HarmonyPostfix]
        private static void Postfix(Plant __instance, ref float __result)
        {
            if (!FarmSkill.Active)
                return;
            float speed = Speed(PlantKeys.Of(__instance));
            if (speed > 1f)
                __result /= speed;
        }

        /// <summary>How many times faster than the game the plant grows: 1 for a plant no player with Farming planted.</summary>
        public static float Speed(ZDO zdo)
        {
            float speed = 1f;
            if (PlantKeys.IsPlanted(zdo))
                speed += FarmSkill.Share(FarmingPerkSettings.GrowthSpeedAt100.Value, PlantKeys.Level(zdo));
            if (CompostSettings.Enabled.Value && PlantKeys.Fed(zdo))
                speed += Mathf.Max(0f, CompostSettings.GrowthSpeed.Value) / 100f;
            return speed;
        }
    }
}
