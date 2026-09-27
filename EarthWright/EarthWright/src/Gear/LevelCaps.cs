using EarthWright.Brush;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Gear
{
    /// <summary>
    /// What the level of the held terrain tool allows, handed to the Brush and Paths modules through
    /// <see cref="BrushCaps"/>: the largest radius ("Radius Per Level") and which features are unlocked ("Level Unlocks").
    /// A feature is an entry's name (EarthWright's without "ew_", e.g. "lower", "ramp", "uproot"; the game's by prefab
    /// name, e.g. "paved_road_v2"), a shape ("square", "rectangle", "ring", "frame") or a level style ("step",
    /// "instant"); names are compared without case and without a leading "ew_". Both settings are off while empty, and
    /// nothing is limited while no terrain tool is in hand.
    /// </summary>
    public static class LevelCaps
    {
        private static CachedNumbers radii;
        private static CachedLevels unlocks;

        public static void Install()
        {
            radii = new CachedNumbers(GearSettings.RadiusPerLevel);
            unlocks = new CachedLevels(GearSettings.LevelUnlocks);
            BrushCaps.LevelMaxRadius = MaxRadius;
            BrushCaps.LevelUnlocks = Unlocked;
            // The click itself is refused for a locked entry, so entries that send no terrain edit (clear, custom) are gated too.
            BrushCaps.EntryRefusal = id => Refusal(id, null, null);
        }

        /// <summary>The radius cap for the held tool's level; float.MaxValue when levels do not limit it.</summary>
        public static float MaxRadius()
        {
            if (!HeldTool.IsTerrainTool)
                return float.MaxValue;
            float? cap = SettingLists.ForLevel(radii.Current, HeldTool.Level);
            return cap.HasValue && cap.Value > 0f ? cap.Value : float.MaxValue;
        }

        /// <summary>A feature is unlocked unless the setting names it with a level above the held tool's.</summary>
        public static bool Unlocked(string feature)
        {
            return !HeldTool.IsTerrainTool || RequiredLevel(feature) <= HeldTool.Level;
        }

        /// <summary>The level a feature needs, or 0 when it is not gated.</summary>
        public static int RequiredLevel(string feature)
        {
            string key = Normalize(feature);
            return key != null && unlocks.Current.TryGetValue(key, out int level) ? level : 0;
        }

        /// <summary>
        /// Null when the held tool's level allows this entry with this shape and level style (null: not used), else the
        /// refusal naming the level needed.
        /// </summary>
        public static string Refusal(string entryId, BrushShape? shape, LevelStyle? style)
        {
            if (!HeldTool.IsTerrainTool || unlocks.Current.Count == 0)
                return null;
            int need = RequiredLevel(entryId);
            if (shape.HasValue && shape.Value != BrushShape.Circle)
                need = Mathf.Max(need, RequiredLevel(shape.Value.ToString()));
            if (style.HasValue && style.Value != LevelStyle.Ease)
                need = Mathf.Max(need, RequiredLevel(style.Value.ToString()));
            return need > HeldTool.Level ? GearWords.LevelLocked + " " + need : null;
        }

        /// <summary>Lower case without a leading "ew_", so "ew_lower", "Lower" and "lower" are the same feature.</summary>
        public static string Normalize(string feature)
        {
            if (string.IsNullOrWhiteSpace(feature))
                return null;
            string key = feature.Trim().ToLowerInvariant();
            return key.StartsWith("ew_") ? key.Substring(3) : key;
        }
    }
}
