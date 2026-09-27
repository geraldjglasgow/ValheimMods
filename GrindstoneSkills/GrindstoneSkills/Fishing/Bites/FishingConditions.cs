using System.Collections.Generic;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The time and weather fish like. Day time and weather are the same on every machine (the environment follows the
    /// world's clock and seed), so the fish's owner and the angler agree without anything being sent.
    /// <list type="bullet">
    /// <item>Dawn and dusk: the game's own curve (Fish uses it for jumping): 1 at a quarter and three quarters of the day,
    /// fading to 0 at noon and midnight. Bites grow by Dawn And Dusk Bite Bonus times it.</item>
    /// <item>Rain (EnvMan.IsWet): bites grow by Rain Bite Bonus.</item>
    /// <item>Night (EnvMan.IsNight): the big-one chance grows by Night Big One Bonus.</item>
    /// </list>
    /// </summary>
    public static class FishingConditions
    {
        /// <summary>0..1: how close the day is to the height of dawn or dusk.</summary>
        public static float DawnDusk()
        {
            EnvMan env = EnvMan.instance;
            if (env == null)
                return 0f;
            float day = env.GetDayFraction();
            return Mathf.Clamp01(1f - Mathf.Abs(Mathf.Abs(day * 2f - 1f) - 0.5f) * 2f);
        }

        public static float BiteFactor()
        {
            float dawnDusk = 1f + FishSkill.Percent(FishingBiteSettings.DawnDuskBonus.Value) * DawnDusk();
            return EnvMan.IsWet() ? dawnDusk * (1f + FishSkill.Percent(FishingBiteSettings.RainBonus.Value)) : dawnDusk;
        }

        public static float BigOneFactor() =>
            EnvMan.IsNight() ? 1f + FishSkill.Percent(FishingBigFishSettings.NightBonus.Value) : 1f;

        /// <summary>"Good fishing: dusk, rain" or "Quiet water", for the angler's species sense.</summary>
        public static string Describe()
        {
            List<string> good = new List<string>();
            if (DawnDusk() >= 0.5f && EnvMan.instance != null)
                good.Add(EnvMan.instance.GetDayFraction() < 0.5f ? "dawn" : "dusk");
            if (EnvMan.IsWet())
                good.Add("rain");
            if (EnvMan.IsNight())
                good.Add("night, when the big ones feed");
            return good.Count == 0 ? "Quiet water." : "Good fishing: " + string.Join(", ", good) + ".";
        }
    }
}
