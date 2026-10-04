using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using DevBridge.Server;

namespace DevBridge.Sync
{
    /// <summary>
    /// A loaded plugin's BepInEx config entries as they stand in memory. On a player bound by a Charter server those
    /// are the server's values: Charter writes them into the entries without saving the .cfg. Main thread only.
    /// </summary>
    internal static class ConfigDump
    {
        internal static List<Dictionary<string, object>> Plugins() =>
            Chainloader.PluginInfos.Values
                .OrderBy(plugin => plugin.Metadata.Name, StringComparer.OrdinalIgnoreCase)
                .Select(plugin => new Dictionary<string, object>
                {
                    ["guid"] = plugin.Metadata.GUID,
                    ["name"] = plugin.Metadata.Name,
                    ["version"] = plugin.Metadata.Version.ToString(),
                    ["entries"] = plugin.Instance ? plugin.Instance.Config.Count : 0,
                })
                .ToList();

        internal static Dictionary<string, object> Of(string mod, string section)
        {
            PluginInfo plugin = Find(mod);
            ConfigFile config = plugin.Instance ? plugin.Instance.Config : throw new BridgeException($"{plugin.Metadata.Name} did not load");
            return new Dictionary<string, object>
            {
                ["guid"] = plugin.Metadata.GUID,
                ["name"] = plugin.Metadata.Name,
                ["version"] = plugin.Metadata.Version.ToString(),
                ["file"] = config.ConfigFilePath,
                ["charter"] = CharterOf(plugin.Metadata.GUID),
                ["entries"] = Entries(config, section),
            };
        }

        /// <summary>By GUID or name, exactly, else the one plugin whose GUID or name contains the text.</summary>
        private static PluginInfo Find(string mod)
        {
            List<PluginInfo> all = Chainloader.PluginInfos.Values.ToList();
            PluginInfo exact = all.FirstOrDefault(p => Same(p.Metadata.GUID, mod) || Same(p.Metadata.Name, mod));
            if (exact != null) return exact;
            List<PluginInfo> near = all.Where(p => Has(p.Metadata.GUID, mod) || Has(p.Metadata.Name, mod)).ToList();
            if (near.Count == 1) return near[0];
            throw new BridgeException(near.Count == 0
                ? $"no plugin {mod}; /config without mod= lists them"
                : $"{mod} could be {string.Join(", ", near.Select(p => p.Metadata.Name))}");
        }

        private static bool Same(string text, string wanted) => string.Equals(text, wanted, StringComparison.OrdinalIgnoreCase);

        private static bool Has(string text, string wanted) => text != null && text.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0;

        private static List<Dictionary<string, object>> Entries(ConfigFile config, string section) =>
            config.Keys
                .Where(definition => section == null || Has(definition.Section, section))
                .OrderBy(definition => definition.Section, StringComparer.Ordinal)
                .ThenBy(definition => definition.Key, StringComparer.Ordinal)
                .Select(definition => Entry(config[definition]))
                .ToList();

        private static Dictionary<string, object> Entry(ConfigEntryBase entry) => new Dictionary<string, object>
        {
            ["section"] = entry.Definition.Section,
            ["key"] = entry.Definition.Key,
            ["value"] = Toml(entry.BoxedValue, entry.SettingType),
            ["default"] = Toml(entry.DefaultValue, entry.SettingType),
            ["type"] = entry.SettingType.Name,
            ["description"] = (entry.Description?.Description ?? "").Split('\n')[0].Trim(),
        };

        /// <summary>The value as the .cfg would write it.</summary>
        private static string Toml(object value, Type type)
        {
            try { return TomlTypeConverter.ConvertToString(value, type); }
            catch (Exception) { return value?.ToString(); }
        }

        /// <summary>Charter's own status and server difference lines for the mod, when it uses Charter (see ValheimModLibs/Charter).</summary>
        private static Dictionary<string, object> CharterOf(string guid)
        {
            string status = Report("Charter.Status." + guid), diff = Report("Charter.Diff." + guid);
            if (status == null && diff == null) return null;
            return new Dictionary<string, object> { ["status"] = status, ["serverDiff"] = diff };
        }

        private static string Report(string slot)
        {
            if (!(AppDomain.CurrentDomain.GetData(slot) is Func<string> report)) return null;
            try { return report(); }
            catch (Exception error) { return "failed: " + error.Message; }
        }
    }
}
