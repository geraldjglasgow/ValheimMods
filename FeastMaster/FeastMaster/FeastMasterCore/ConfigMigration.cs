using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;

namespace FeastMaster
{
    /// <summary>
    /// Carries a value over when one of FeastMaster's own entries moves to another section or gets a new key. BepInEx keeps the
    /// entries of the file nobody bound in its private OrphanedEntries dictionary and writes them back on save;
    /// taking the old entry out of it and writing its text into the new entry moves the value and lets the old
    /// section disappear from the file on the next save. A file that already has the new entry keeps it.
    /// </summary>
    public static class ConfigMigration
    {
        private static readonly MethodInfo orphansGetter = AccessTools.PropertyGetter(typeof(ConfigFile), "OrphanedEntries");

        /// <summary>The old entry's serialized text, removed from the orphans, or null when there is nothing to migrate.</summary>
        public static string TakeOrphan(ConfigFile config, string oldSection, string newSection, string key)
        {
            return Take(config, new ConfigDefinition(oldSection, key), new ConfigDefinition(newSection, key));
        }

        /// <summary>The same for an entry renamed within its section.</summary>
        public static string TakeRenamed(ConfigFile config, string section, string oldKey, string newKey)
        {
            return Take(config, new ConfigDefinition(section, oldKey), new ConfigDefinition(section, newKey));
        }

        /// <summary>Writes migrated text into a freshly bound entry; nothing happens for null.</summary>
        public static void Apply(ConfigEntryBase entry, string text)
        {
            if (text != null)
                entry.SetSerializedValue(text);
        }

        /// <summary>Writes a migrated number divided by <paramref name="divisor"/>, for an entry whose unit changed; nothing happens for null or unreadable text.</summary>
        public static void ApplyDivided(ConfigEntry<float> entry, string text, float divisor)
        {
            if (text != null && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                entry.Value = value / divisor;
        }

        private static string Take(ConfigFile config, ConfigDefinition old, ConfigDefinition current)
        {
            Dictionary<ConfigDefinition, string> orphans = Orphans(config);
            if (orphans == null || !orphans.TryGetValue(old, out string text))
                return null;
            orphans.Remove(old);
            return orphans.ContainsKey(current) ? null : text;
        }

        private static Dictionary<ConfigDefinition, string> Orphans(ConfigFile config)
        {
            if (orphansGetter == null)
            {
                FeastMaster.Log.LogWarning("ConfigFile.OrphanedEntries not found; old config sections are not migrated.");
                return null;
            }
            return orphansGetter.Invoke(config, null) as Dictionary<ConfigDefinition, string>;
        }
    }
}
