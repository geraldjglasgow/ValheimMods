using System.Collections;
using System.Collections.Generic;
using DevBridge.Director;
using DevBridge.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevBridge.Routes
{
    /// <summary>/shot, /cue, /rec and /cast: the film director (DevBridge/REFERENCE.md, "Filming").</summary>
    internal static class DirectorRoute
    {
        internal static void Register(Router router)
        {
            Calls.Router = router;
            router.Add("/shot",
                "/shot (POST the shot JSON, or file=<path>)&wait=1&timeout=600&record=0&out=&fps=&size=WxH&quality=\n" +
                "                       plays a timed shot: cues (camera moves, spawns, AI, the player, slow motion, any endpoint) at\n" +
                "                       film seconds, recorded offline to out= (.mp4 with game sound) when it says so; status=1, stop=1",
                Shot);
            router.Add("/cue", "/cue (POST one cue, or {\"origin\":[x,y,z],\"yaw\":0,\"cues\":[...]})\n" +
                "                       runs cues at once, outside a shot: to set a scene, try a camera move, spawn and place the cast",
                Cue);
            router.Add("/rec", "/rec?out=<path without extension>&fps=60&size=WxH&quality=16&audio=1 | stop=1 | (status)\n" +
                "                       records the game window offline (every frame one film frame) to <out>.mp4 with its sound",
                Rec);
            router.Add("/cast", "/cast | clear=1 | reset=1 | spawns=0|1 | purge=tombstones,items,creatures | clear_at=x,y,z&radius=3\n" +
                "                       the director's named actors; clear=1 removes the spawned ones; reset=1 also gives back the\n" +
                "                       camera, controls, clock and hidden actors; spawns=0 stops natural spawning (world, spawners,\n" +
                "                       raids) until spawns=1; purge= removes those kinds from the whole world, loaded or not;\n" +
                "                       clear_at= removes loaded rocks, bushes, trees, logs, pickables and items near a point",
                CastList);
            ScoutRoute.Register(router);
            StudioScenesRoute.Register(router);
        }

        private static void Shot(BridgeRequest request)
        {
            if (request.Flag("stop")) { request.Json(ShotRunner.Finish("stopped")); return; }
            if (request.Flag("status") || !request.Has("body") && !request.Has("file")) { request.Json(ShotRunner.Status()); return; }
            ShotRunner.Start(ShotPlan.Parse(ShotPlan.Read(request), request));
            if (request.Flag("wait")) Async.Start(request, WaitForEnd(request));
            else request.Json(ShotRunner.Status());
        }

        private static IEnumerator WaitForEnd(BridgeRequest request)
        {
            while (ShotRunner.Running) yield return null;
            request.Json(ShotRunner.Status());
        }

        private static void Cue(BridgeRequest request)
        {
            JObject spec = Parse(ShotPlan.Read(request));
            ShotFrame frame = ShotFrame.From(spec);
            if (spec["origin"] != null) Cast.Frame = frame;
            DirectorDriver.Ensure();
            JArray cues = spec["cues"] as JArray ?? new JArray(spec);
            var errors = new List<string>();
            for (int i = 0; i < cues.Count; i++) RunOne(cues[i] as JObject, i, frame, errors);
            request.Json(new Dictionary<string, object> { ["ran"] = cues.Count - errors.Count, ["errors"] = errors, ["cast"] = Cast.Describe(), ["camera"] = CameraRig.Describe() });
        }

        private static void RunOne(JObject spec, int index, ShotFrame frame, List<string> errors)
        {
            try
            {
                Director.Cue cue = Director.Cue.Parse(spec ?? throw new BridgeException("not an object"), index);
                Cues.Run(cue, frame, cue.T);
            }
            catch (System.Exception error)
            {
                errors.Add($"cue {index}: {(error is BridgeException ? error.Message : error.ToString())}");
            }
        }

        private static JObject Parse(string json)
        {
            try { return JObject.Parse(json); }
            catch (JsonReaderException error) { throw new BridgeException("cue JSON: " + error.Message); }
        }

        private static void Rec(BridgeRequest request)
        {
            if (request.Flag("stop")) request.Json(Recorder.Stop());
            else if (request.Has("out")) { DirectorDriver.Ensure(); request.Json(Recorder.Start(RecordPlan.From(request))); }
            else request.Json(Recorder.Status());
        }

        private static void CastList(BridgeRequest request)
        {
            if (request.Flag("reset")) ShotRunner.Restore(false);
            else if (request.Flag("clear")) Cast.Clear();
            if (request.Has("spawns")) WorldTidy.Quiet = !request.Flag("spawns");
            int cleared = request.Has("clear_at") ? WorldTidy.ClearAround(Fmt.ParseV3(request.Get("clear_at"), "clear_at"), request.Float("radius", 3f)) : 0;
            Dictionary<string, int> purged = request.Has("purge") ? WorldTidy.Purge(request.Get("purge").Split(',')) : null;
            request.Json(new Dictionary<string, object>
            {
                ["cast"] = Cast.Describe(), ["camera"] = CameraRig.Describe(), ["shot"] = ShotRunner.Status(),
                ["spawns"] = !WorldTidy.Quiet, ["purged"] = purged, ["cleared"] = cleared,
            });
        }
    }
}
