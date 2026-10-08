using System;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;

namespace PackPanel.Core
{
    /// <summary>
    /// Whether OpenKeep 1.8.0 or later runs on this peer: by its plugin GUID in BepInEx's chainloader, checked on first
    /// use (after every plugin has loaded) and cached for the process. The two mods reference nothing of each other and
    /// work alone; together, OpenKeep reads what PackPanel publishes: the main grid (<see cref="Layout.GridContract"/>),
    /// the button strip (<see cref="Panels.ButtonStrip"/>) and the key ring's Key Items and Key Stack from this mod's
    /// config. Here it decides whether the player panel keeps a strip for OpenKeep's buttons, who writes the keys' stack
    /// size (<see cref="Ring.KeyStacks"/>) and whether there is a Feet slot: OpenKeep's boots (prefab names starting
    /// <see cref="BootsPrefix"/>, Legs items that OpenKeep wears itself) get one while its <c>16. Boots / Separate Boots</c>
    /// is on (<see cref="SeparateBoots"/>, the server's value while connected). Older OpenKeep versions know nothing of
    /// PackPanel, so they count as absent; one without that entry has no boots, so no Feet slot.
    /// </summary>
    public static class OpenKeepLink
    {
        public const string Guid = "milkyteam.openkeep";
        public const string BootsPrefix = "OpenKeep_Boots_";
        private const string BootsSection = "16. Boots";
        private const string BootsKey = "Separate Boots";
        private static readonly System.Version First = new System.Version(1, 8, 0);

        private static bool detected;
        private static ConfigFile config;
        private static ConfigEntry<bool> separateBoots;
        private static bool bootsBroken;

        /// <summary>OpenKeep's Separate Boots changed (on this peer or from the server): the layout gains or loses the Feet slot.</summary>
        public static event Action BootsChanged;

        public static bool Present
        {
            get
            {
                if (!detected)
                {
                    detected = true;
                    config = Chainloader.PluginInfos.TryGetValue(Guid, out PluginInfo info) && info.Metadata.Version >= First
                        && info.Instance != null ? info.Instance.Config : null;
                }
                return config != null;
            }
        }

        /// <summary>OpenKeep wears its boots on their own, so the layout has a Feet slot.</summary>
        public static bool SeparateBoots
        {
            get
            {
                ConfigEntry<bool> entry = BootsEntry();
                return entry != null && entry.Value;
            }
        }

        /// <summary>One of OpenKeep's boots, by its prefab name (real leggings are Legs items too).</summary>
        public static bool IsBoots(ItemDrop.ItemData item) =>
            item != null && ItemNames.PrefabName(item).StartsWith(BootsPrefix, StringComparison.Ordinal);

        /// <summary>
        /// OpenKeep's Separate Boots entry, kept once found and watched from then on; looked for again while missing (only
        /// at layout time), so a version without it simply has no Feet slot.
        /// </summary>
        private static ConfigEntry<bool> BootsEntry()
        {
            if (separateBoots != null || bootsBroken || !Present)
                return separateBoots;
            try
            {
                if (!config.TryGetEntry(BootsSection, BootsKey, out ConfigEntry<bool> entry))
                    return null;
                separateBoots = entry;
            }
            catch (InvalidCastException)
            {
                bootsBroken = true;
                Plugin.Log.LogWarning($"OpenKeep's {BootsSection} / {BootsKey} is not a true/false setting; no Feet slot");
                return null;
            }
            separateBoots.SettingChanged += (sender, args) => BootsChanged?.Invoke();
            Plugin.Log.LogInfo($"Feet slot follows OpenKeep's {BootsKey} ({separateBoots.Value})");
            return separateBoots;
        }
    }
}
