using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using DevBridge.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevBridge.Director
{
    /// <summary>
    /// The scene files a film is planned and shot from: one JSON per scene in the scenes folder (name, title, order,
    /// status, notes, origin, yaw, length, setup steps, cues), and the takes recorded from them in the footage folder
    /// (preview/ and final/: name.mp4, name.json with what fired when, name_sheet.png).
    /// </summary>
    internal static class SceneLibrary
    {
        private static ConfigEntry<string> scenes, footage;

        internal static void Bind(ConfigFile config)
        {
            string videos = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "ValheimTrailers");
            scenes = config.Bind("Director", "Scenes folder", Path.Combine(videos, "scenes"),
                "Where the director's scene files are (one JSON per scene); the Studio's Scenes tab lists and edits them.");
            footage = config.Bind("Director", "Footage folder", videos,
                "Where takes are recorded: preview/ and final/ beneath it.");
        }

        internal static string Folder => Path.GetFullPath(Environment.ExpandEnvironmentVariables(scenes.Value));
        internal static string Footage => Path.GetFullPath(Environment.ExpandEnvironmentVariables(footage.Value));

        internal static List<Dictionary<string, object>> List()
        {
            if (!Directory.Exists(Folder)) return new List<Dictionary<string, object>>();
            return Directory.GetFiles(Folder, "*.json").Select(Summary).Where(s => s != null)
                .OrderBy(s => (int)s["order"]).ThenBy(s => (string)s["name"]).ToList();
        }

        private static Dictionary<string, object> Summary(string file)
        {
            JObject scene;
            try { scene = JObject.Parse(File.ReadAllText(file)); }
            catch (JsonException) { return null; }
            string name = Path.GetFileNameWithoutExtension(file);
            return new Dictionary<string, object>
            {
                ["name"] = name, ["title"] = scene.Value<string>("title") ?? name, ["order"] = scene.Value<int?>("order") ?? 99,
                ["status"] = scene.Value<string>("status") ?? "planned", ["length"] = scene.Value<float?>("length") ?? 0f,
                ["notes"] = scene.Value<string>("notes") ?? "", ["cues"] = (scene["cues"] as JArray)?.Count ?? 0,
                ["takes"] = Takes(name),
            };
        }

        internal static JObject Read(string name) => JObject.Parse(File.ReadAllText(PathOf(name)));

        /// <summary>Saves a scene after checking it parses as a shot; returns its summary.</summary>
        internal static Dictionary<string, object> Save(string name, string json)
        {
            JObject scene;
            try { scene = JObject.Parse(json); }
            catch (JsonReaderException error) { throw new BridgeException("scene JSON: " + error.Message); }
            ShotPlan.Parse(scene.ToString(), BridgeRequest.Make("/shot", new Dictionary<string, string> { ["record"] = "0" }));
            Directory.CreateDirectory(Folder);
            File.WriteAllText(PathOf(name), scene.ToString(Formatting.Indented));
            return Summary(PathOf(name));
        }

        internal static string PathOf(string name)
        {
            if (string.IsNullOrEmpty(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains(".."))
                throw new BridgeException($"not a scene name: {name}");
            return Path.Combine(Folder, name + ".json");
        }

        /// <summary>The base path (no extension) of a scene's take of one kind: preview or final.</summary>
        internal static string TakeBase(string name, string kind)
        {
            if (kind != "preview" && kind != "final") throw new BridgeException("kind is preview or final");
            PathOf(name);
            return Path.Combine(Footage, kind, name);
        }

        private static Dictionary<string, object> Takes(string name)
        {
            var takes = new Dictionary<string, object>();
            foreach (string kind in new[] { "preview", "final" })
            {
                string mp4 = TakeBase(name, kind) + ".mp4";
                if (File.Exists(mp4)) takes[kind] = File.GetLastWriteTime(mp4).ToString("yyyy-MM-dd HH:mm");
            }
            return takes;
        }
    }
}
