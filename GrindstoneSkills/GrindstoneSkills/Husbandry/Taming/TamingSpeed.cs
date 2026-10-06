using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Taming speed and Taming Levels. The game tames on the creature's owner: every 3 s, while the creature is fed and
    /// not alerted, <c>Tameable.TamingUpdate</c> calls <c>DecreaseRemainingTime(3)</c>, which doubles the step for each
    /// player within the creature's taming range (60 m) who has the taming boost and subtracts it from the time left.
    /// A prefix scales the step by 1 + the Taming Speed share of the best keeper within that same range, so it stacks
    /// with the game's boost. When the creature is listed in Taming Levels and no keeper in range reaches the level, the
    /// step is 0 and taming makes no progress (the creature still eats and settles as usual).
    /// </summary>
    public static class TamingSpeed
    {
        [HarmonyPatch(typeof(Tameable), nameof(Tameable.DecreaseRemainingTime))]
        private static class Step
        {
            [HarmonyPrefix]
            private static void Prefix(Tameable __instance, ref float time)
            {
                time = HookGuard.Run("taming speed", static taming => Scaled(taming.tameable, taming.step), (tameable: __instance, step: time), time);
            }
        }

        private static float Scaled(Tameable tameable, float step) => tameable == null ? step : step * Factor(tameable);

        /// <summary>
        /// The factor on taming progress the best keeper within the taming range of <paramref name="tameable"/> gives
        /// now: 0 when Taming Levels blocks it, 1 while Husbandry is off. The hover text uses it too.
        /// </summary>
        public static float Factor(Tameable tameable)
        {
            if (!HusbandrySkill.Active)
                return 1f;
            float level = Keeper.BestLevel(tameable.transform.position, tameable.m_tamingSpeedMultiplierRange);
            if (level < TamingLevels.Required(Herd.PrefabName(tameable)))
                return 0f;
            return 1f + HusbandrySkill.Share(HusbandryTamingSettings.TamingSpeed.Value, level);
        }
    }
}
