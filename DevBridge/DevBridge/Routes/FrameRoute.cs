using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using DevBridge.Stage;
using UnityEngine;

namespace DevBridge.Routes
{
    /// <summary>/frame and /light: a repeatable view of the stage and fixed light for comparing it with the game's own.</summary>
    internal static class FrameRoute
    {
        internal static void Register(Router router)
        {
            router.Add("/frame",
                "/frame?id=N&yaw=0&pitch=12&distance=<fit>&fov=&camera=free|player&park=1&zoom=&hud=0|1&at=x,y,z&radius=2\n" +
                "                       frame everything placed (or one id, or a point) for /screenshot: the game's free-fly camera at a\n" +
                "                       fitted distance from the side the row faces, turned yaw and tilted pitch degrees (park=1 moves the\n" +
                "                       player behind it); camera=player moves and turns the player instead; hud=0 hides the HUD\n" +
                "/frame?off=1           back to the normal camera (and the HUD, if /frame hid it)",
                Frame);
            router.Add("/light",
                "/light?tod=0.5|off&env=<environment>|off&wind=<angle>,<0-1>|off&reset=1&list=1\n" +
                "                       fixed light on this machine only: time of day (0 midnight, 0.25 dawn, 0.5 noon), weather by\n" +
                "                       environment name (list=1 lists them), wind; what the tod, env, resetenv and wind console\n" +
                "                       commands set, without devcommands",
                Light);
        }

        private static void Frame(BridgeRequest request)
        {
            if (request.Flag("off"))
            {
                Framing.Release();
                Async.Start(request, Settle(request, null));
                return;
            }
            if (request.Has("hud")) Framing.ShowHud(request.Flag("hud"));
            Bounds box = Target(request);
            Vector3 look = Framing.Look(request.Float("yaw", 0f), request.Float("pitch", 12f));
            float fov = request.Float("fov", Framing.Camera.m_camera.fieldOfView);
            float distance = request.Has("distance") ? request.Float("distance", 5f) : Framing.Fit(box, look, fov, Framing.Camera.m_camera.aspect);
            if (request.Get("camera", "free") == "player") Framing.PlayerAt(box.center, look, distance, request.Float("zoom", 0f));
            else FreeAt(request, box.center - look * distance, look, fov);
            var framed = new Dictionary<string, object> { ["target"] = Fmt.V3(box.center), ["size"] = Fmt.V3(box.size), ["distance"] = Fmt.R(distance) };
            Async.Start(request, Settle(request, framed));
        }

        private static void FreeAt(BridgeRequest request, Vector3 eye, Vector3 look, float fov)
        {
            Framing.Free(eye, look, fov);
            if (!request.Has("park") || request.Flag("park")) Framing.Park(eye, look);
        }

        /// <summary>One placement (id=), a point (at=x,y,z with radius=), or everything placed.</summary>
        private static Bounds Target(BridgeRequest request)
        {
            if (request.Has("id")) return AssetInfo.Bounds(Placements.Get(request.Int("id", 0)).Root);
            if (request.Has("at")) return new Bounds(Fmt.ParseV3(request.Get("at"), "at"), Vector3.one * 2f * request.Float("radius", 1f));
            return Placements.Bounds(Placements.All, out Bounds box) ? box : throw new BridgeException("nothing is placed: give at=x,y,z (and radius=) or place something first");
        }

        // A few frames for the camera (and the normal camera's own smoothing) to settle before reporting where it is.
        private static IEnumerator Settle(BridgeRequest request, Dictionary<string, object> framed)
        {
            yield return null;
            yield return new WaitForSecondsRealtime(request.Get("camera") == "player" ? 0.5f : 0.05f);
            Dictionary<string, object> info = Framing.Describe();
            if (framed != null) foreach (KeyValuePair<string, object> pair in framed) info[pair.Key] = pair.Value;
            request.Json(info);
        }

        private static void Light(BridgeRequest request)
        {
            EnvMan env = EnvMan.instance ? EnvMan.instance : throw new BridgeException("no world loaded");
            if (request.Flag("reset")) Reset(env);
            if (request.Has("tod")) TimeOfDay(env, request.Get("tod"));
            if (request.Has("env")) Weather(env, request.Get("env"));
            if (request.Has("wind")) Wind(env, request.Get("wind"));
            Async.Start(request, Report(request, env));
        }

        private static void Reset(EnvMan env)
        {
            env.m_debugTimeOfDay = false;
            env.m_debugEnv = "";
            env.ResetDebugWind();
            env.ForceInstantEnvironmentSwitch();
        }

        private static void TimeOfDay(EnvMan env, string value)
        {
            env.m_debugTimeOfDay = value != "off";
            if (env.m_debugTimeOfDay) env.m_debugTime = Mathf.Repeat(Fmt.Numbers(value, 1, "tod")[0], 1f);
        }

        private static void Weather(EnvMan env, string name)
        {
            if (name == "off") env.m_debugEnv = "";
            else env.m_debugEnv = env.m_environments.Select(e => e.m_name).FirstOrDefault(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
                ?? throw new BridgeException($"no environment {name}; the game has {string.Join(", ", Names(env))}");
            env.ForceInstantEnvironmentSwitch();
        }

        private static void Wind(EnvMan env, string value)
        {
            if (value == "off") env.ResetDebugWind();
            else
            {
                float[] wind = Fmt.Numbers(value, 2, "wind");
                env.SetDebugWind(wind[0], wind[1]);
            }
        }

        private static IEnumerable<string> Names(EnvMan env) => env.m_environments.Select(e => e.m_name).OrderBy(n => n);

        // The environment switches on the game's next fixed updates; report once it has.
        private static IEnumerator Report(BridgeRequest request, EnvMan env)
        {
            yield return new WaitForSecondsRealtime(request.Has("env") || request.Flag("reset") ? 0.3f : 0.05f);
            var info = new Dictionary<string, object>
            {
                ["dayFraction"] = Fmt.R(env.GetDayFraction()),
                ["tod"] = env.m_debugTimeOfDay ? (object)Fmt.R(env.m_debugTime) : "off",
                ["environment"] = env.GetCurrentEnvironment()?.m_name,
                ["env"] = string.IsNullOrEmpty(env.m_debugEnv) ? "off" : env.m_debugEnv,
                ["wind"] = env.m_debugWind ? $"{env.m_debugWindAngle:0} deg, {env.m_debugWindIntensity:0.##}" : "off",
            };
            if (request.Flag("list")) info["environments"] = Names(env).ToList();
            request.Json(info);
        }
    }
}
