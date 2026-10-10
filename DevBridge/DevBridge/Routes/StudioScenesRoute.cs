using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DevBridge.Capture;
using DevBridge.Director;
using DevBridge.Server;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DevBridge.Routes
{
    /// <summary>/studio/scenes and /studio/scene/*: the Studio's Scenes tab (and any local tool) plans and shoots scenes.</summary>
    internal static class StudioScenesRoute
    {
        internal static void Register(Router router)
        {
            SceneLibrary.Bind(DevBridgePlugin.Instance.Config);
            router.Add("/studio/scenes", "/studio/scenes            the scene files (scenes folder) in order, with their takes, and what is playing", List);
            router.Add("/studio/scene", "/studio/scene?name=        one scene file, with its last takes' records (what fired when)", Get);
            router.Add("/studio/scene/save", "/studio/scene/save?name= (POST the scene JSON)   checks it parses as a shot, then saves it", Save);
            router.Add("/studio/scene/play", "/studio/scene/play?name=&mode=rehearse|preview|final   setup steps, then the shot", Play);
            router.Add("/studio/scene/stop", "/studio/scene/stop        stops the scene playing", request => { SceneRunner.Stop(); request.Json(SceneRunner.Status()); });
            router.Add("/studio/scene/status", "/studio/scene/status      what is playing: step, film time, cues fired", request => request.Json(SceneRunner.Status()));
            router.Add("/studio/scene/sheet", "/studio/scene/sheet?name=&kind=preview|final   a take's contact sheet (PNG, base64)", Sheet);
            router.Add("/studio/scene/open", "/studio/scene/open?name=&kind=   opens the take's video in this machine's player", Open);
        }

        private static void List(BridgeRequest request) => request.Json(new Dictionary<string, object>
        {
            ["folder"] = SceneLibrary.Folder, ["footage"] = SceneLibrary.Footage, ["scenes"] = SceneLibrary.List(), ["runner"] = SceneRunner.Status(),
        });

        private static void Get(BridgeRequest request)
        {
            string name = request.Require("name");
            var takes = new Dictionary<string, object>();
            foreach (string kind in new[] { "preview", "final" })
            {
                string record = SceneLibrary.TakeBase(name, kind) + ".json";
                if (File.Exists(record)) takes[kind] = JObject.Parse(File.ReadAllText(record));
            }
            request.Json(new Dictionary<string, object> { ["name"] = name, ["scene"] = SceneLibrary.Read(name), ["takes"] = takes });
        }

        private static void Save(BridgeRequest request) =>
            request.Json(SceneLibrary.Save(request.Require("name"), request.Get("body") ?? throw new BridgeException("POST the scene JSON")));

        private static void Play(BridgeRequest request)
        {
            SceneRunner.Play(request.Require("name"), request.Get("mode", "rehearse"));
            request.Json(SceneRunner.Status());
        }

        /// <summary>Made by ffmpeg on a worker thread when missing or older than the take, then sent as base64.</summary>
        private static void Sheet(BridgeRequest request)
        {
            string take = SceneLibrary.TakeBase(request.Require("name"), request.Get("kind", "preview"));
            if (!File.Exists(take + ".mp4")) throw new BridgeException("no take of that kind yet");
            string sheet = take + "_sheet.png";
            Task work = File.Exists(sheet) && File.GetLastWriteTime(sheet) >= File.GetLastWriteTime(take + ".mp4")
                ? Task.CompletedTask : Task.Run(() => MakeSheet(take + ".mp4", sheet));
            Async.Start(request, Answer(request, work, sheet));
        }

        private static IEnumerator Answer(BridgeRequest request, Task work, string sheet)
        {
            while (!work.IsCompleted) yield return null;
            if (work.IsFaulted) throw new BridgeException("contact sheet: " + work.Exception?.InnerException?.Message);
            request.Json(new Dictionary<string, object> { ["png"] = Convert.ToBase64String(File.ReadAllBytes(sheet)), ["file"] = sheet });
        }

        // Four frames a second, eight across, each stamped with its time.
        private static void MakeSheet(string video, string sheet)
        {
            string ffmpeg = VideoJob.Find(null) ?? throw new IOException("ffmpeg is not on the PATH");
            string label = "drawtext=fontfile='C\\:/Windows/Fonts/arial.ttf':text='%{pts\\:hms}':x=4:y=4:fontsize=16:fontcolor=white:box=1:boxcolor=black@0.5";
            string args = $"-y -loglevel error -i \"{video}\" -vf \"fps=4,scale=240:-1,{label},tile=8x8\" -frames:v 1 \"{sheet}\"";
            using (Process process = Process.Start(new ProcessStartInfo(ffmpeg, args) { UseShellExecute = false, CreateNoWindow = true }))
                process.WaitForExit();
            if (!File.Exists(sheet)) throw new IOException("ffmpeg made no sheet");
        }

        private static void Open(BridgeRequest request)
        {
            string video = SceneLibrary.TakeBase(request.Require("name"), request.Get("kind", "preview")) + ".mp4";
            if (!File.Exists(video)) throw new BridgeException("no take of that kind yet");
            string url = "file:///" + video.Replace(Path.DirectorySeparatorChar, '/');
            try { Process.Start(new ProcessStartInfo("msedge", $"\"{url}\"") { UseShellExecute = true }); }
            catch (System.ComponentModel.Win32Exception) { Process.Start(new ProcessStartInfo(video) { UseShellExecute = true }); }
            request.Json(new Dictionary<string, object> { ["opened"] = video });
        }
    }
}
