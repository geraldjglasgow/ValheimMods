using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;

namespace PlayerGrid
{
    /// <summary>
    /// PackPanel, the inventory mod (a bigger grid, labelled slots, a key ring, backpacks, a tacklebox): whether it runs
    /// on this peer, by its plugin GUID in BepInEx's chainloader, checked on first use (after every plugin has loaded)
    /// and cached for the process. Nothing of PackPanel is referenced; a consumer reads what it publishes: its config
    /// entries (<see cref="Entry{T}"/>) and the main grid in the character's custom data (<see cref="PackPanelGrid"/>).
    /// </summary>
    public static class PackPanelLink
    {
        public const string Guid = "milkyteam.packpanel";

        private static bool detected;
        private static ConfigFile? config;

        public static bool Present
        {
            get
            {
                if (!detected)
                {
                    detected = true;
                    config = Chainloader.PluginInfos.TryGetValue(Guid, out PluginInfo info) && info.Instance != null ? info.Instance.Config : null;
                }
                return config != null;
            }
        }

        /// <summary>PackPanel is loaded and its master switch (<c>1. Inventory / Enabled</c>) is on.</summary>
        public static bool LaysOutInventory
        {
            get
            {
                ConfigEntry<bool>? enabled = Entry<bool>("1. Inventory", "Enabled");
                return enabled != null && enabled.Value;
            }
        }

        /// <summary>One of PackPanel's config entries (the server's value while connected), or null without PackPanel.</summary>
        public static ConfigEntry<T>? Entry<T>(string section, string key) =>
            Present && config!.TryGetEntry(section, key, out ConfigEntry<T> entry) ? entry : null;
    }
}
