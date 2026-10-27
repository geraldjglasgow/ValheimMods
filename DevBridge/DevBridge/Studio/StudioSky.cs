using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Studio
{
    /// <summary>
    /// The studio's time and weather switches. A time switch keeps the time of day near one point (night at midnight,
    /// morning just after sunrise, day at noon, dusk before sunset): the sky runs on at the game's pace for an in-game
    /// hour, then back for an hour, and again, so it stays that time and never jumps. It is the game's own time of day
    /// override (the console's tod), so the world's clock runs on underneath and plants, fermenters and the day count go
    /// on as normal; the sky and this machine's day and night checks follow the switch (in single player or on a host,
    /// night spawns too). A weather switch holds one environment (the console's env); a dungeon's or a boss's own still
    /// wins. Both carry over into the next world loaded. A tod or env from the console, /light or a film cue takes over
    /// and turns that switch off.
    /// </summary>
    internal static class StudioSky
    {
        internal static readonly Dictionary<string, float> Points = new Dictionary<string, float>
        {
            ["night"] = 0f, ["morning"] = 0.27f, ["day"] = 0.5f, ["dusk"] = 0.7f,
        };

        private const string Off = "off";
        private const float Hour = 1f / 24f;

        private static string time = Off, weather = Off;
        private static EnvMan owner;
        private static double started;
        private static float written = float.NaN;

        /// <summary>From the plugin's Update: holds the switches that are on.</summary>
        internal static void Tick()
        {
            EnvMan env = EnvMan.instance;
            if (!env || time == Off && weather == Off) return;
            if (env != owner) Adopt(env);
            else Notice(env);
            if (time != Off) Hold(env);
        }

        internal static void SetTime(string mode)
        {
            EnvMan env = Env();
            mode = (mode ?? "").ToLowerInvariant();
            if (mode != Off && !Points.ContainsKey(mode)) throw new BridgeException("time= takes off, night, morning, day or dusk");
            if (mode == Off && time != Off) env.m_debugTimeOfDay = false;
            (time, started) = (mode, Now());
            if (mode != Off) Own(env);
        }

        internal static void SetWeather(string name)
        {
            EnvMan env = Env();
            if (string.Equals(name, Off, StringComparison.OrdinalIgnoreCase))
            {
                if (weather != Off) Show(env, "");
                weather = Off;
                return;
            }
            weather = env.m_environments.Select(e => e.m_name).FirstOrDefault(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
                ?? throw new BridgeException($"no weather {name}; the game has {string.Join(", ", Names(env))}");
            Show(env, weather);
            Own(env);
        }

        internal static Dictionary<string, object> State()
        {
            var state = new Dictionary<string, object> { ["time"] = time, ["weather"] = weather };
            EnvMan env = EnvMan.instance;
            if (!env || !ZNet.instance) return state;
            double now = Now();
            state["clock"] = Clock(env.GetDayFraction());
            state["worldClock"] = Clock(env.RescaleDayFraction((float)(now % env.m_dayLengthSec / env.m_dayLengthSec)));
            state["day"] = env.GetDay(now);
            state["environment"] = env.GetCurrentEnvironment()?.m_name;
            state["environments"] = Names(env).ToList();
            return state;
        }

        // A new world: the switches carry over into it.
        private static void Adopt(EnvMan env)
        {
            started = Now();
            Own(env);
            if (weather != Off) Show(env, weather);
        }

        // A tod or env set by another hand turns that switch off.
        private static void Notice(EnvMan env)
        {
            if (time != Off && (!env.m_debugTimeOfDay || env.m_debugTime != written)) time = Off;
            if (weather != Off && env.m_debugEnv != weather) weather = Off;
        }

        // An hour on from the point and an hour back, at the pace the game's clock moves there (its night runs faster).
        private static void Hold(EnvMan env)
        {
            float point = Points[time];
            float pace = point >= 0.25f && point < 0.75f ? 0.5f / 0.7f : 0.25f / 0.15f;
            float hours = (float)((Now() - started) / env.m_dayLengthSec) * pace / Hour;
            written = Mathf.Repeat(point + Mathf.PingPong(hours, 1f) * Hour, 1f);
            env.m_debugTimeOfDay = true;
            env.m_debugTime = written;
        }

        private static void Own(EnvMan env)
        {
            owner = env;
            if (time != Off) Hold(env);
        }

        private static void Show(EnvMan env, string name)
        {
            env.m_debugEnv = name;
            env.ForceInstantEnvironmentSwitch();
        }

        private static EnvMan Env() => EnvMan.instance ? EnvMan.instance : throw new BridgeException("no world loaded: time and weather are the world's");

        private static double Now() => ZNet.instance ? ZNet.instance.GetTimeSeconds() : 0d;

        private static IEnumerable<string> Names(EnvMan env) => env.m_environments.Select(e => e.m_name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase);

        private static string Clock(float fraction)
        {
            int minutes = Mathf.FloorToInt(Mathf.Repeat(fraction, 1f) * 1440f);
            return $"{minutes / 60:00}:{minutes % 60:00}";
        }
    }
}
