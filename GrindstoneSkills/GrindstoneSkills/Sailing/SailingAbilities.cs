using System.Collections.Generic;
using BepInEx.Configuration;
using Hotkeys;
using Translations;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Sailing's actives as other mods see them through <see cref="Api.SailingApi"/>: Wind Call (level 25), the
    /// lookout (level 50) and Fog Lift (level 75), by id, with the local player's key, unlock and cooldown, and a line on what each does with
    /// the current settings. Names and lines are in the local player's language, built when asked.
    /// </summary>
    public static class SailingAbilities
    {
        public const string WindCall = "windcall";
        public const string Lookout = "lookout";
        public const string FogLift = "foglift";

        /// <summary>The actives the local player has unlocked, Wind Call first.</summary>
        public static string[] Unlocked()
        {
            List<string> ids = new List<string>(3);
            float level = SailingSkill.Local();
            if (WindCallInput.Unlockable && level >= WindCallSettings.Level.Value)
                ids.Add(WindCall);
            if (LookoutInput.Unlockable && level >= LookoutSettings.Level.Value)
                ids.Add(Lookout);
            if (FogLiftInput.Unlockable && level >= FogLiftInput.Level)
                ids.Add(FogLift);
            return ids.ToArray();
        }

        public static string Name(string id) =>
            id == WindCall ? Lang.Get("gs_sail_ability_windcall") : id == Lookout ? Lang.Get("gs_sail_ability_lookout")
                : id == FogLift ? Lang.Get("gs_sail_ability_foglift") : "";

        /// <summary>The player's key for it, short: "K", "Shift+O"; empty when unbound.</summary>
        public static string Key(string id)
        {
            ConfigEntry<KeyboardShortcut> key = id == WindCall ? WindCallSettings.Key : id == Lookout ? LookoutSettings.Key
                : id == FogLift ? SailingSettings.FogLiftKey : null;
            return key != null ? KeyNames.Short(key.Value) : "";
        }

        public static float CooldownLeft(string id) =>
            id == WindCall ? WindCallInput.CooldownLeft : id == Lookout ? LookoutInput.CooldownLeft : id == FogLift ? FogLiftInput.CooldownLeft : 0f;

        public static float CooldownLength(string id) =>
            Mathf.Max(0f, id == WindCall ? WindCallSettings.Cooldown.Value : id == Lookout ? LookoutSettings.Cooldown.Value
                : id == FogLift ? FogLiftInput.Cooldown : 0f);

        public static string Description(string id)
        {
            if (id == WindCall)
                return Lang.Format("gs_sail_ability_windcall_description", WindCallSettings.Duration.Value.ToString("0"),
                    Seconds(WindCallSettings.Cooldown.Value));
            if (id == Lookout)
                return Lang.Format("gs_sail_ability_lookout_description", LookoutSettings.Radius.Value.ToString("0"),
                    LookoutSettings.Duration.Value.ToString("0"), Seconds(LookoutSettings.Cooldown.Value));
            if (id == FogLift)
                return Lang.Format("gs_sail_ability_foglift_description", SkillPage.Percent(FogLiftView.Thinner),
                    FogLiftMist.Radius.ToString("0"), Seconds(FogLiftInput.Duration), Seconds(FogLiftInput.Cooldown));
            return "";
        }

        private static string Seconds(float seconds)
        {
            int whole = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            return whole >= 60 && whole % 60 == 0 ? Lang.Format("gs_sail_minutes", whole / 60) : Lang.Format("gs_sail_seconds", whole);
        }
    }
}
