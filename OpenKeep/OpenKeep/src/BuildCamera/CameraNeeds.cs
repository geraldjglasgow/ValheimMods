using System.Collections.Generic;
using BepInEx.Configuration;
using OpenKeep.Core;

namespace OpenKeep.BuildCamera
{
    /// <summary>
    /// The comfort conditions of the camera, read where the player stands: the game's Resting effect (a fire, a roof,
    /// no enemy near; <c>SEMan.s_statusEffectResting</c>) and its comfort level (<c>Player.GetComfortLevel</c>, which
    /// the game recounts every 2 s).
    /// </summary>
    public static class CameraNeeds
    {
        public static bool Met(Player player, ConfigEntry<bool> resting, ConfigEntry<int> comfort)
        {
            return !MissesResting(player, resting) && player.GetComfortLevel() >= comfort.Value;
        }

        /// <summary>What is missing as one localized line, or null when nothing is.</summary>
        public static string Describe(Player player, ConfigEntry<bool> resting, ConfigEntry<int> comfort)
        {
            if (Met(player, resting, comfort))
                return null;
            List<string> parts = new List<string>();
            if (MissesResting(player, resting))
                parts.Add(Language.Localize(CameraModule.Resting));
            int have = player.GetComfortLevel();
            if (have < comfort.Value)
                parts.Add(string.Format(Language.Localize(CameraModule.Comfort), comfort.Value, have));
            return string.Join(", ", parts);
        }

        private static bool MissesResting(Player player, ConfigEntry<bool> resting)
        {
            return resting.Value && !player.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectResting);
        }
    }
}
