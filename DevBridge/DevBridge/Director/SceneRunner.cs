using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DevBridge.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>
    /// Plays a scene file: its setup steps in real time (move to the set, quiet the world, lay paths, light), then its
    /// shot, recorded as a preview or final take or only rehearsed; when the shot ends the take's record (what fired
    /// when) is written beside the video.
    /// </summary>
    internal static class SceneRunner
    {
        private static Coroutine running;
        private static string scene, mode, step;
        private static readonly List<string> Problems = new List<string>();

        internal static void Play(string name, string how)
        {
            if (running != null || ShotRunner.Running) throw new BridgeException($"scene {scene} is still playing; stop it first");
            if (how != "rehearse" && how != "preview" && how != "final") throw new BridgeException("mode is rehearse, preview or final");
            JObject spec = SceneLibrary.Read(name);
            (scene, mode, step) = (name, how, "starting");
            Problems.Clear();
            DirectorDriver.Ensure();
            running = DevBridgePlugin.Instance.StartCoroutine(Run(name, spec, how));
        }

        internal static void Stop()
        {
            if (running != null) DevBridgePlugin.Instance.StopCoroutine(running);
            running = null;
            if (ShotRunner.Running) ShotRunner.Finish("stopped");
            step = "stopped";
        }

        private static IEnumerator Run(string name, JObject spec, string how)
        {
            var frame = ShotFrame.From(spec);
            foreach (JToken setup in spec["setup"] as JArray ?? new JArray())
            {
                step = "setup: " + (setup.Value<string>("do") ?? "?");
                IEnumerator work = SceneSteps.Run(setup as JObject, frame, Problems);
                while (Advance(work)) yield return work.Current;
            }
            if (!Start(name, spec, how)) { running = null; yield break; }
            step = how == "rehearse" ? "playing" : "recording";
            while (ShotRunner.Running) yield return null;
            Keep(name, how);
            (step, running) = ("done", null);
        }

        private static bool Advance(IEnumerator work)
        {
            try
            {
                return work.MoveNext();
            }
            catch (Exception error)
            {
                Problems.Add(error is BridgeException ? error.Message : error.ToString());
                return false;
            }
        }

        private static bool Start(string name, JObject spec, string how)
        {
            var args = new Dictionary<string, string> { ["record"] = how == "rehearse" ? "0" : "1" };
            if (how != "rehearse") args["out"] = SceneLibrary.TakeBase(name, how);
            if (how == "preview") (args["size"], args["quality"]) = ("1280x720", "24");
            if (how == "final") (args["size"], args["quality"]) = ("2560x1440", "16");
            try
            {
                ShotRunner.Start(ShotPlan.Parse(spec.ToString(), BridgeRequest.Make("/shot", args)));
                return true;
            }
            catch (Exception error)
            {
                Problems.Add(error is BridgeException ? error.Message : error.ToString());
                step = "failed";
                return false;
            }
        }

        /// <summary>The shot's own report (what fired when, the recording) beside the take, for the edit.</summary>
        private static void Keep(string name, string how)
        {
            if (how == "rehearse") return;
            string file = SceneLibrary.TakeBase(name, how) + ".json";
            File.WriteAllText(file, JsonConvert.SerializeObject(ShotRunner.Status(), Formatting.Indented));
        }

        internal static Dictionary<string, object> Status() => new Dictionary<string, object>
        {
            ["scene"] = scene, ["mode"] = mode, ["step"] = step ?? "idle", ["busy"] = running != null,
            ["problems"] = new List<string>(Problems), ["shot"] = ShotRunner.Status(),
        };
    }
}
