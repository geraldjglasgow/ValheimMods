using System.Collections;
using System.Collections.Generic;
using DevBridge.Director;
using DevBridge.Server;
using DevBridge.Studio;
using UnityEngine;

namespace DevBridge.Routes
{
    /// <summary>
    /// The switches and buttons above every studio tab (also usable from curl): time and weather (<see cref="StudioSky"/>),
    /// free camera, hitboxes and colliders (<see cref="StudioViews"/>), and the world's own spawning (the Director's
    /// <see cref="WorldTidy.Quiet"/>, so a scene's spawns step and this switch are one). Each answers with all of their states.
    /// </summary>
    internal static class StudioSwitchesRoute
    {
        private const string Pad = "\n                       ";

        internal static void Register(Router router)
        {
            router.Add("/studio/sky", "/studio/sky?time=off|night|morning|day|dusk&weather=off|<environment>" + Pad +
                "the studio's switches: hold the time of day near a point (an hour on and back, the world's clock runs on)" + Pad +
                "and the weather; this machine only; without arguments what they are, the clock and the weathers", Sky);
            router.Add("/studio/camera", "/studio/camera?free=1|0  the game's free-fly camera, let go where the camera is; 0 back to the player",
                request => { StudioViews.SetCamera(request.Flag("free")); request.Json(Switches()); });
            router.Add("/studio/hitboxes", "/studio/hitboxes?on=1|0  /hitbox's view on or off: swings flash their reach, hits on you draw a line",
                request => { StudioViews.SetHitboxes(request.Flag("on")); request.Json(Switches()); });
            router.Add("/studio/colliders", "/studio/colliders?on=1|0  /overlay's collider shapes round you (or the free camera) on or off",
                request => { StudioViews.SetColliders(request.Flag("on")); request.Json(Switches()); });
            router.Add("/studio/spawns", "/studio/spawns?on=1|0  the world's own spawning (world spawns, spawner points, nests, raids)" + Pad +
                "on or off, as /cast?spawns= sets it; creatures already there stay",
                request => { WorldTidy.Quiet = !request.Flag("on"); request.Json(Switches()); });
        }

        // A time or weather change shows on the game's next fixed updates; the answer waits for them.
        private static void Sky(BridgeRequest request)
        {
            if (request.Has("time")) StudioSky.SetTime(request.Get("time", "off"));
            if (request.Has("weather")) StudioSky.SetWeather(request.Get("weather", "off"));
            if (request.Has("time") || request.Has("weather")) Async.Start(request, AnswerLater(request));
            else request.Json(Switches());
        }

        private static IEnumerator AnswerLater(BridgeRequest request)
        {
            yield return new WaitForSecondsRealtime(0.2f);
            request.Json(Switches());
        }

        private static Dictionary<string, object> Switches()
        {
            Dictionary<string, object> state = StudioSky.State();
            foreach (KeyValuePair<string, object> view in StudioViews.State()) state[view.Key] = view.Value;
            state["spawns"] = !WorldTidy.Quiet;
            return state;
        }
    }
}
