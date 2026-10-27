using System.Collections.Generic;
using System.IO;
using BepInEx;
using DevBridge.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevBridge.Studio
{
    /// <summary>
    /// The studio's favourites and groups, in one JSON file beside DevBridge's .cfg (DevBridge.studio.json), so they
    /// outlast the browser, a restart and the page's token: {"items": {"favourites": [prefab], "groups": [{"name",
    /// "items": [{"prefab", "amount", "level"}]}]}, "commands": {"favourites": [command], "groups": [{"name",
    /// "commands": [command]}]}}. The page owns that shape; the file only has to hold one JSON object.
    /// </summary>
    internal static class StudioSaved
    {
        private const int Largest = 1 << 20;

        internal static string FilePath => Path.Combine(Paths.ConfigPath, "DevBridge.studio.json");

        internal static JObject Read()
        {
            if (!File.Exists(FilePath)) return new JObject();
            try { return JObject.Parse(File.ReadAllText(FilePath)); }
            catch (JsonException error) { throw new BridgeException($"{FilePath} is not a JSON object ({error.Message}): fix or delete it"); }
        }

        internal static Dictionary<string, object> Write(string json)
        {
            if (json == null || json.Length > Largest) throw new BridgeException("POST the favourites and groups as one JSON object under 1 MB");
            JObject saved;
            try { saved = JObject.Parse(json); }
            catch (JsonException error) { throw new BridgeException("not a JSON object: " + error.Message); }
            File.WriteAllText(FilePath, saved.ToString(Formatting.Indented));
            return new Dictionary<string, object> { ["file"] = FilePath };
        }
    }
}
