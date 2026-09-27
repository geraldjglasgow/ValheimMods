using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// A crop's star roll when it ripens, with the dishes' odds table (<see cref="StarOdds"/>). The effective level is the
    /// planter's level, plus Seed Levels Per Star for each star of the seed it was planted from, plus Companion Levels for
    /// each companion kind nearby (<see cref="Companions"/>), plus Compost Star Levels when a bin fertilized it.
    /// </summary>
    public static class CropRoll
    {
        /// <summary>What a growing crop brings to its roll, read from its ZDO before it ripens.</summary>
        public struct Grower
        {
            public float Level;
            public int SeedStars;
            public bool Fed;

            public static Grower From(ZDO zdo) =>
                new Grower { Level = PlantKeys.Level(zdo), SeedStars = PlantKeys.SeedStars(zdo), Fed = PlantKeys.Fed(zdo) };
        }

        /// <summary>Whether crops roll stars at all, and this plant's crops carry them.</summary>
        public static bool Rolls(CropPlant crop) => FarmSkill.Active && FarmingSettings.CropStars.Value && crop != null && crop.CarriesStars;

        public static float Effective(Grower grower, int companions)
        {
            float level = Mathf.Max(0f, grower.Level);
            level += Mathf.Max(0, grower.SeedStars) * Mathf.Max(0f, FarmingSettings.SeedLevelsPerStar.Value);
            level += Mathf.Max(0, companions) * Mathf.Max(0f, FarmingSettings.CompanionLevels.Value);
            if (grower.Fed && CompostSettings.Enabled.Value)
                level += Mathf.Max(0f, CompostSettings.StarLevels.Value);
            return level;
        }

        /// <summary>The effective level of a growing crop now, companions counted at its position.</summary>
        public static float EffectiveNow(Plant plant, CropPlant crop, ZDO zdo) =>
            Effective(Grower.From(zdo), Companions.Count(plant.transform.position, crop));

        /// <summary>Whether a crop planted at this level ripens into a giant, rolled once.</summary>
        public static bool RollGiant(float planterLevel) =>
            Random.value < FarmSkill.Share(FarmingSettings.GiantChanceAt100.Value, planterLevel);
    }
}
