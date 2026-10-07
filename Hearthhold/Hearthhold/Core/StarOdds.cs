using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// The odds of 0 to 3 stars. A roll's effective level is the actor's skill level (0 to 100) plus what the source
    /// brings (<see cref="IngredientBonus"/>, <see cref="CreatureBonus"/>, <see cref="FishBonus"/>) plus the day's fortune
    /// (<see cref="Fortune"/>), from 0 to 150. The chances are interpolated between four tables, then a profession's floor
    /// (<see cref="Professions"/>) or the ingredients' floor (<see cref="IngredientFloor"/>) lifts anything below it. Fixed numbers, not settings.
    /// <code>
    /// effective   plain  bronze  silver  gold
    ///     0        90%     9%     1%     0%
    ///    50        55%    30%    12%     3%
    ///   100        25%    38%    26%    11%
    ///   150         5%    35%    35%    25%
    /// </code>
    /// </summary>
    public static class StarOdds
    {
        public const float MaxEffective = 150f;
        public const float PerIngredientStar = 20f;
        public const float PerCreatureStar = 20f;
        public const float MaxCreatureBonus = 60f;
        public const float PerFishLevel = 15f;

        private const float Step = 50f;

        private static readonly float[][] Tables =
        {
            new[] { 0.90f, 0.09f, 0.01f, 0.00f },
            new[] { 0.55f, 0.30f, 0.12f, 0.03f },
            new[] { 0.25f, 0.38f, 0.26f, 0.11f },
            new[] { 0.05f, 0.35f, 0.35f, 0.25f },
        };

        /// <summary>Ingredients' average stars (0 to 3) as levels: 20 per star.</summary>
        public static float IngredientBonus(float averageStars) => Mathf.Clamp(averageStars, 0f, Stars.Max) * PerIngredientStar;

        /// <summary>
        /// The fewest stars a dish gets from its ingredients: their average stars rounded down. Starred ingredients never
        /// cook into worse food; the cook's level and the bonus can only raise it.
        /// </summary>
        public static int IngredientFloor(float averageStars) => Mathf.Clamp(Mathf.FloorToInt(averageStars + 0.001f), 0, Stars.Max);

        /// <summary>A creature's level (1 = no stars) as levels: 20 per creature star, at most 60.</summary>
        public static float CreatureBonus(int creatureLevel) => Mathf.Clamp((creatureLevel - 1) * PerCreatureStar, 0f, MaxCreatureBonus);

        /// <summary>A fish's level (its quality, 1 to 5) as levels: 15 per level above 1.</summary>
        public static float FishBonus(int fishLevel) => Mathf.Clamp(fishLevel - 1, 0, 4) * PerFishLevel;

        /// <summary>The effective level of a roll today, 0 to 150.</summary>
        public static float Effective(float level, float bonus) =>
            Mathf.Clamp(Sane(level) + Sane(bonus) + Fortune.Levels(), 0f, MaxEffective);

        /// <summary>The chance of exactly <paramref name="stars"/> at an effective level, before any floor.</summary>
        public static float Chance(float effective, int stars)
        {
            float position = Mathf.Clamp(effective, 0f, MaxEffective) / Step;
            int low = Mathf.Min((int)position, Tables.Length - 2);
            float t = position - low;
            int index = Mathf.Clamp(stars, 0, Stars.Max);
            return Mathf.Lerp(Tables[low][index], Tables[low + 1][index], t);
        }

        /// <summary>Rolls the stars of one item: level and bonus as above, never below <paramref name="floor"/>.</summary>
        public static int Roll(float level, float bonus, int floor)
        {
            float effective = Effective(level, bonus);
            float roll = Random.value;
            int stars = Stars.Max;
            for (int i = 0; i < Stars.Max; i++)
            {
                roll -= Chance(effective, i);
                if (roll < 0f)
                {
                    stars = i;
                    break;
                }
            }
            return Mathf.Clamp(Mathf.Max(stars, floor), 0, Stars.Max);
        }

        /// <summary>A level or bonus that came over the network or from another mod: never negative, NaN or infinite.</summary>
        public static float Sane(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
    }
}
