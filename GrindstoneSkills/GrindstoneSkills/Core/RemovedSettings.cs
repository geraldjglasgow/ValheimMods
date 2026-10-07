using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;

namespace GrindstoneSkills
{
    /// <summary>
    /// BepInEx keeps every line of the .cfg that no setting binds (its OrphanedEntries) and writes it back on each save,
    /// so settings a version removed would stay in players' files forever. Forgetting them before the file is first
    /// written drops them; the hot reload reads the file afterwards, where they are gone.
    /// </summary>
    public static class RemovedSettings
    {
        private static readonly PropertyInfo Orphans =
            typeof(ConfigFile).GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void Forget(ConfigFile config)
        {
            if (Orphans?.GetValue(config) is Dictionary<ConfigDefinition, string> orphans && orphans.Count > 0)
            {
                GrindstoneSkills.Log.LogInfo($"Removed {orphans.Count} settings this version no longer has from the .cfg.");
                orphans.Clear();
            }
        }
    }
}
