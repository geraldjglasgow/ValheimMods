using BepInEx.Configuration;
using EarthWright.Core;
using SyncedConfig;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// Gentle slopes (section "6. Height Limits", off by default): after a height edit, the ground around the edited
    /// area is relaxed so no step between neighbouring points is steeper than the configured angle. Synced: the owner
    /// of the ground does the relaxing, and every owner must do it the same way.
    /// </summary>
    public static class SlopeSettings
    {
        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<float> MaxAngle { get; private set; }
        public static ConfigEntry<float> Radius { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            Enabled = synced.Bind(Sections.Limits, "Gentle Slopes", false,
                "When on, digging also lowers the ground around the hole and raising also lifts the ground around the mound, so no slope next to an edit is steeper than 'Gentle Slope Angle'. Ground under building pieces is left alone when the edit skips buildings.");
            MaxAngle = synced.Bind(Sections.Limits, "Gentle Slope Angle", 40f,
                "The steepest slope, in degrees, that gentle slopes leave next to an edit.",
                acceptableValues: new AcceptableValueRange<float>(10f, 80f));
            Radius = synced.Bind(Sections.Limits, "Gentle Slope Radius", 6f,
                "How far around an edit, in metres, gentle slopes may reshape the ground.",
                acceptableValues: new AcceptableValueRange<float>(1f, 20f));
        }

        public static bool On => Enabled != null && Enabled.Value;

        /// <summary>The largest height difference allowed per metre of horizontal distance.</summary>
        public static float Gradient => Mathf.Tan(Mathf.Clamp(MaxAngle != null ? MaxAngle.Value : 40f, 1f, 89f) * Mathf.Deg2Rad);

        public static float RadiusValue => Radius != null ? Radius.Value : 6f;
    }
}
