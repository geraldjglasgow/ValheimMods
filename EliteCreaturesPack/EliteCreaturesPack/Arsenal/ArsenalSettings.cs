using System.Collections.Generic;
using BepInEx.Configuration;
using SyncedConfig;

namespace EliteCreaturesPack.Arsenal
{
    /// <summary>
    /// Section 8: the arsenal skeletons, a master on/off switch for all of them and one for each. Everything else about them and
    /// the bone weapons is fixed (see <c>features/skeleton-arsenal.md</c>). Synced; read at each spawn, so a reload
    /// takes effect at once.
    /// </summary>
    public static class ArsenalSettings
    {
        public const string Section = "8 - Skeleton Arsenal";

        private static readonly Dictionary<ArsenalWeapon, ConfigEntry<bool>> spawns = new Dictionary<ArsenalWeapon, ConfigEntry<bool>>();
        private static ConfigEntry<bool> enabled = null!;

        /// <summary>Whether new skeletons with this weapon may spawn: the master switch and its own. Ones already in the world stay.</summary>
        public static bool Spawns(ArsenalWeapon weapon) => enabled.Value && spawns.TryGetValue(weapon, out var on) && on.Value;

        public static void Initialize(SyncedConfiguration config)
        {
            enabled = config.Bind(Section, "Enabled", true,
                "All the arsenal skeletons, the master switch. Off: none of them spawns, whatever their own switches below say; "
                + "ones already in the world stay.");
            foreach (ArsenalWeapon weapon in ArsenalWeapon.All)
            {
                spawns[weapon] = config.Bind(Section, "Skeleton " + weapon.Title, true,
                    $"The Skeleton {weapon.Title} (bone {weapon.Key.ToLowerInvariant()}) spawns in place of some of the game's "
                    + "skeletons, wherever and whenever they spawn. Off: no new ones; ones already in the world stay.");
            }
        }
    }
}
