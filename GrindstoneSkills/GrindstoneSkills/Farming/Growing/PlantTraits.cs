using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A crop's room, heat and cold tolerance and biomes follow its planter's level. Plant.UpdateHealth, on every client
    /// on every slow-update pass, decides the plant's status from its own instance fields: m_growRadius (room), m_tolerateHeat (the
    /// Ashlands), m_tolerateCold (Mountain, Deep North) and m_biome. Before each call the fields are set from the prefab's
    /// values and the planter's level in the ZDO, so every client agrees and turning Farming off restores the game's:
    /// <list type="bullet">
    /// <item>room shrinks by the planter's share of Grow Space Reduction At Level 100;</item>
    /// <item>heat tolerance from Heat Tolerance Level;</item>
    /// <item>from Cold Tolerance Level, a crop that grows in the Meadows also grows in the Mountain and Deep North
    /// (barley, flax and the Mistlands mushrooms keep their biomes).</item>
    /// </list>
    /// Only crops (<see cref="CropCatalog"/>) planted by a player with Farming; tree saplings keep the game's rules.
    /// </summary>
    public static class PlantTraits
    {
        private const float MaxReduction = 0.8f;
        private const Heightmap.Biome Cold = Heightmap.Biome.Mountain | Heightmap.Biome.DeepNorth;

        [HarmonyPatch(typeof(Plant), nameof(Plant.UpdateHealth))]
        private static class Health
        {
            [HarmonyPrefix]
            private static void Prefix(Plant __instance) => HookGuard.Run("Farming traits", () => Apply(__instance));
        }

        private static void Apply(Plant plant)
        {
            CropPlant crop = CropCatalog.OfPlant(plant);
            if (crop?.Plant == null)
                return;
            ZDO zdo = PlantKeys.Of(plant);
            bool on = FarmSkill.Active && PlantKeys.IsPlanted(zdo);
            float level = PlantKeys.Level(zdo);
            plant.m_growRadius = on ? Radius(crop, level) : crop.Plant.m_growRadius;
            plant.m_tolerateHeat = crop.Plant.m_tolerateHeat || (on && FarmSkill.Reached(FarmingPerkSettings.HeatToleranceLevel.Value, level));
            plant.m_tolerateCold = crop.Plant.m_tolerateCold || (on && ColdHardy(crop, level));
            plant.m_biome = on ? Biomes(crop, level) : crop.Plant.m_biome;
        }

        /// <summary>The room a crop planted at this level needs around it.</summary>
        public static float Radius(CropPlant crop, float level)
        {
            float reduction = Mathf.Min(MaxReduction, FarmSkill.Share(FarmingPerkSettings.GrowSpaceAt100.Value, level));
            return crop.Plant.m_growRadius * (1f - reduction);
        }

        /// <summary>The biomes a crop planted at this level grows in.</summary>
        public static Heightmap.Biome Biomes(CropPlant crop, float level) =>
            ColdHardy(crop, level) ? crop.Plant.m_biome | Cold : crop.Plant.m_biome;

        private static bool ColdHardy(CropPlant crop, float level) =>
            (crop.Plant.m_biome & Heightmap.Biome.Meadows) != 0 && FarmSkill.Reached(FarmingPerkSettings.ColdToleranceLevel.Value, level);
    }
}
