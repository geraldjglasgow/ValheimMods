using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Plants grow faster in the rain. Plant.UpdateHealth runs from Plant.SUpdate on every slow-update pass (the game's
    /// 10 s gate there never closes, so that is several times a second), so time is measured here: at most once per 10 s
    /// of game time per plant, the plant's owner credits a healthy plant a player planted with Rain Growth Bonus percent of
    /// the time since the last credit, while the weather is wet (EnvMan.IsWet: the environment at the owner's client,
    /// which stands near the plant, since owners are the nearby players). Dry spells move the mark without crediting, and
    /// a long gap (a hitch, an ownership change) counts at most 30 s. A plant under a roof reads "no sun" and gains
    /// nothing, as it does not grow.
    /// </summary>
    [HarmonyPatch(typeof(Plant), nameof(Plant.UpdateHealth))]
    public static class RainGrowth
    {
        private const double Interval = 10.0;
        private const double MaxCredit = 30.0;

        private sealed class Mark
        {
            public double Time;
        }

        private static readonly ConditionalWeakTable<Plant, Mark> marks = new ConditionalWeakTable<Plant, Mark>();

        [HarmonyPostfix]
        private static void Postfix(Plant __instance, double timeSincePlanted)
        {
            if (!FarmSkill.Active || timeSincePlanted < Interval || ZNet.instance == null)
                return;
            double now = ZNet.instance.GetTimeSeconds();
            Mark mark = MarkOf(__instance, now);
            double elapsed = now - mark.Time;
            if (elapsed < Interval)
                return;
            mark.Time = now;
            if (Raining(__instance))
                PlantClock.Advance(__instance, System.Math.Min(elapsed, MaxCredit) * Bonus);
        }

        /// <summary>
        /// The plant's mark, set to now when the plant is first seen. No GetValue lambda: one capturing now would allocate
        /// on every pass of every plant, Farming on or off.
        /// </summary>
        private static Mark MarkOf(Plant plant, double now)
        {
            if (marks.TryGetValue(plant, out Mark mark))
                return mark;
            mark = new Mark { Time = now };
            marks.Add(plant, mark);
            return mark;
        }

        private static float Bonus => Mathf.Max(0f, FarmingPerkSettings.RainBonus.Value) / 100f;

        private static bool Raining(Plant plant) =>
            Bonus > 0f && EnvMan.IsWet() && plant.GetStatus() == Plant.Status.Healthy && plant.m_nview.IsOwner()
            && PlantKeys.IsPlanted(PlantKeys.Of(plant));
    }
}
