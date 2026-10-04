using DevBridge.Server;
using DevBridge.Sync;

namespace DevBridge.Routes
{
    /// <summary>/config: a loaded plugin's BepInEx config entries as they stand in memory.</summary>
    internal static class ConfigRoute
    {
        internal static void Register(Router router) => router.Add("/config",
            "/config?mod=<plugin name or GUID>&section=<text>\n" +
            "                       a plugin's config entries in memory (a bound player's hold the server's values): section,\n" +
            "                       key, value, default, type, first line of the description, and Charter's status and server\n" +
            "                       differences; without mod= the plugins with their GUIDs",
            request => request.Json(request.Get("mod") is string mod ? (object)ConfigDump.Of(mod, request.Get("section")) : ConfigDump.Plugins()));
    }
}
