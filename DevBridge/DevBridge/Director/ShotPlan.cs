using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevBridge.Capture;
using DevBridge.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>One timed cue of a shot: its film time and its JSON, with the kind read from its first known key.</summary>
    internal sealed class Cue
    {
        internal static readonly string[] Kinds =
        {
            "camera", "spawn", "adopt", "ai", "attack", "hero", "player", "time", "shake", "call", "eval", "console",
            "light", "sound", "effect", "remove", "hide", "mark", "env", "clean", "end", "sfx", "vfx", "world", "prop", "title", "fade", "extra", "act", "fling", "lights", "anim", "pin", "stand", "focus", "blink",
        };

        internal float T;
        internal string Kind;
        internal JObject Spec;

        /// <summary>A condition that must also hold (checked every frame from T on): near, far, attacking, dead.</summary>
        internal JObject When => Spec["when"] as JObject;

        /// <summary>Seconds after its condition first holds that a waiting cue runs ("delay"), and when that was (-1 not yet).</summary>
        internal float Delay => Spec.Value<float?>("delay") ?? 0f;
        internal float HeldAt = -1f;

        internal static Cue Parse(JObject spec, int index)
        {
            // The kind is the cue's first key (after t and when) that names one, so fields such as "attack" or "light"
            // inside a cue of another kind are only its values.
            string kind = spec.Properties().Select(p => p.Name).FirstOrDefault(name => Kinds.Contains(name))
                ?? throw new BridgeException($"cue {index} has none of: {string.Join(", ", Kinds)}");
            return new Cue { T = spec.Value<float?>("t") ?? 0f, Kind = kind, Spec = spec };
        }
    }

    /// <summary>
    /// A shot: a name, its frame (origin, yaw), its length in film seconds, its cues, and where to record it (none for
    /// a rehearsal at real speed).
    /// </summary>
    internal sealed class ShotPlan
    {
        internal string Name;
        internal ShotFrame Frame;
        internal float Length;
        internal bool Keep;
        internal bool Clean;
        internal RecordPlan Record;
        internal List<Cue> Cues;

        internal static ShotPlan Parse(string json, BridgeRequest request)
        {
            JObject spec;
            try { spec = JObject.Parse(json); }
            catch (JsonReaderException error) { throw new BridgeException("shot JSON: " + error.Message); }
            var plan = new ShotPlan
            {
                Name = spec.Value<string>("name") ?? "shot", Frame = ShotFrame.From(spec),
                Keep = spec.Value<bool?>("keep") ?? false, Clean = spec.Value<bool?>("clean") ?? true, Cues = ParseCues(spec["cues"] as JArray),
            };
            plan.Length = spec.Value<float?>("length") ?? plan.Cues.Select(c => c.T).DefaultIfEmpty(0f).Max() + 1f;
            plan.Record = Recording(spec["record"] as JObject, request);
            return plan;
        }

        private static List<Cue> ParseCues(JArray cues)
        {
            if (cues == null) throw new BridgeException("a shot needs \"cues\": [...]");
            var list = new List<Cue>();
            for (int i = 0; i < cues.Count; i++)
                list.Add(Cue.Parse(cues[i] as JObject ?? throw new BridgeException($"cue {i} is not an object"), i));
            return list.OrderBy(c => c.T).ToList();
        }

        /// <summary>The shot's record settings, overridden by the request: out=, fps=, size=, quality=, record=0.</summary>
        private static RecordPlan Recording(JObject spec, BridgeRequest request)
        {
            if (request.Has("record") && !request.Flag("record")) return null;
            string output = request.Get("out") ?? spec?.Value<string>("out");
            if (output == null) return null;
            var plan = new RecordPlan
            {
                Base = Fmt.WindowsPath(output), Fps = Mathf.Clamp(request.Int("fps", spec?.Value<int?>("fps") ?? 60), 10, 120),
                Quality = request.Int("quality", spec?.Value<int?>("quality") ?? 16), Audio = spec?.Value<bool?>("audio") ?? true,
            };
            plan.Size(request.Get("size") ?? spec?.Value<string>("size"));
            plan.Ffmpeg = VideoJob.Find(request.Get("ffmpeg")) ?? throw new BridgeException("recording needs ffmpeg.exe on the PATH, or ffmpeg=<path>");
            return plan;
        }

        internal static string Read(BridgeRequest request)
        {
            if (request.Has("body")) return request.Get("body");
            string file = request.Get("file") ?? throw new BridgeException("give the shot JSON as the POST body or file=<path>");
            string path = Path.GetFullPath(Fmt.WindowsPath(file));
            return File.Exists(path) ? File.ReadAllText(path) : throw new BridgeException($"no file {path}");
        }
    }
}
