using System.Collections.Generic;
using BepInEx.Configuration;
using Hotkeys;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Sailing's actives as other mods see them through <see cref="Api.SailingApi"/>: Wind Call (level 25) and the
    /// lookout (level 50), by id, with the local player's key, unlock and cooldown, and a line on what each does with
    /// the current settings.
    /// </summary>
    public static class SailingAbilities
    {
        public const string WindCall = "windcall";
        public const string Lookout = "lookout";

        /// <summary>The actives the local player has unlocked, Wind Call first.</summary>
        public static string[] Unlocked()
        {
            List<string> ids = new List<string>(2);
            float level = SailingSkill.Local();
            if (WindCallInput.Unlockable && level >= WindCallSettings.Level.Value)
                ids.Add(WindCall);
            if (LookoutInput.Unlockable && level >= LookoutSettings.Level.Value)
                ids.Add(Lookout);
            return ids.ToArray();
        }

        public static string Name(string id) => id == WindCall ? "Wind Call" : id == Lookout ? "Lookout" : "";

        /// <summary>The player's key for it, short: "K", "Shift+O"; empty when unbound.</summary>
        public static string Key(string id)
        {
            ConfigEntry<KeyboardShortcut> key = id == WindCall ? WindCallSettings.Key : id == Lookout ? LookoutSettings.Key : null;
            return key != null ? KeyNames.Short(key.Value) : "";
        }

        public static float CooldownLeft(string id) =>
            id == WindCall ? WindCallInput.CooldownLeft : id == Lookout ? LookoutInput.CooldownLeft : 0f;

        public static float CooldownLength(string id) =>
            Mathf.Max(0f, id == WindCall ? WindCallSettings.Cooldown.Value : id == Lookout ? LookoutSettings.Cooldown.Value : 0f);

        public static string Description(string id)
        {
            if (id == WindCall)
                return $"Turns the wind to blow the way you look for {WindCallSettings.Duration.Value:0} s, for everyone " +
                    $"aboard. Use it while you steer. Cooldown {Seconds(WindCallSettings.Cooldown.Value)}.";
            if (id == Lookout)
                return $"Sends a pulse {LookoutSettings.Radius.Value:0} m out from the ship: everyone aboard sees the name " +
                    $"tags of the enemies it reaches for {LookoutSettings.Duration.Value:0} s. " +
                    $"Cooldown {Seconds(LookoutSettings.Cooldown.Value)}.";
            return "";
        }

        private static string Seconds(float seconds)
        {
            int whole = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            return whole >= 60 && whole % 60 == 0 ? $"{whole / 60} min" : $"{whole} s";
        }
    }
}
