using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>
    /// A film's own weather: a copy of one of the game's environments under a new name, its night (or day) light,
    /// fog and sun scaled, added to the game's list so /light env= picks it like any other.
    /// </summary>
    internal static class FilmEnv
    {
        internal static string Make(JObject spec)
        {
            EnvMan env = EnvMan.instance ? EnvMan.instance : throw new BridgeException("no world loaded");
            string name = spec.Value<string>("name") ?? throw new BridgeException("env needs name=");
            string from = spec.Value<string>("base") ?? "Clear";
            EnvSetup source = env.m_environments.FirstOrDefault(e => e.m_name == from) ?? throw new BridgeException($"no environment {from}");
            EnvSetup copy = source.Clone();
            copy.m_name = name;
            Night(copy, spec.Value<float?>("night") ?? 1f, spec.Value<float?>("fog") ?? 1f);
            Day(copy, spec.Value<float?>("day") ?? 1f, spec.Value<float?>("dayFog") ?? spec.Value<float?>("day") ?? 1f);
            if (spec["density"] != null) copy.m_fogDensityNight = copy.m_fogDensityDay = copy.m_fogDensityMorning = copy.m_fogDensityEvening = spec.Value<float>("density");
            if (spec["dark"] != null) copy.m_alwaysDark = spec.Value<bool>("dark");
            Colors(copy, spec);
            env.m_environments.RemoveAll(e => e.m_name == name);
            env.AppendEnvironment(copy);
            return name;
        }

        /// <summary>Night colours set outright (ambient=, fogColor=, sun= as [r,g,b]), for a night with no tint of its own.</summary>
        private static void Colors(EnvSetup env, JObject spec)
        {
            if (spec["ambient"] is JArray a) env.m_ambColorNight = Color(a);
            if (spec["fogColor"] is JArray f) env.m_fogColorNight = env.m_fogColorSunNight = Color(f);
            if (spec["sun"] is JArray s) env.m_sunColorNight = Color(s);
            if (spec["ao"] is JArray ao) env.m_ambientOcclusionColor = Color(ao);
        }

        private static Color Color(JArray c) => new Color(c[0].Value<float>(), c[1].Value<float>(), c[2].Value<float>(), 1f);

        /// <summary>
        /// Daylight (morning, day and evening) dimmed for a darker, moodier day: ambient, sun and light scaled by day, the
        /// day fog colours by dayFog (the haze a blizzard turns white).
        /// </summary>
        private static void Day(EnvSetup env, float day, float fog)
        {
            env.m_ambColorDay *= day;
            (env.m_sunColorMorning, env.m_sunColorDay, env.m_sunColorEvening) = (env.m_sunColorMorning * day, env.m_sunColorDay * day, env.m_sunColorEvening * day);
            env.m_lightIntensityDay *= day;
            (env.m_fogColorMorning, env.m_fogColorDay, env.m_fogColorEvening) = (env.m_fogColorMorning * fog, env.m_fogColorDay * fog, env.m_fogColorEvening * fog);
            (env.m_fogColorSunMorning, env.m_fogColorSunDay, env.m_fogColorSunEvening) = (env.m_fogColorSunMorning * fog, env.m_fogColorSunDay * fog, env.m_fogColorSunEvening * fog);
        }

        /// <summary>Night ambient, sun and light scaled by night; night fog colours by fog; no aurora.</summary>
        private static void Night(EnvSetup env, float night, float fog)
        {
            env.m_ambColorNight *= night;
            env.m_sunColorNight *= night;
            env.m_lightIntensityNight *= night;
            env.m_fogColorNight *= fog;
            env.m_fogColorSunNight *= fog;
            env.m_auroraIntensityNight = 0f;
        }
    }

    /// <summary>
    /// A clean frame while a shot runs: no HUD, chat window, floating texts or messages, and DevBridge's own debug
    /// drawing (hitbox and overlay lines) kept off.
    /// </summary>
    internal static class Clean
    {
        private static readonly List<LineRenderer> Switched = new List<LineRenderer>();
        private static bool active, hudWas;
        private static int nextSweep;

        internal static void Set(bool on)
        {
            if (on == active || !Hud.instance) return;
            if (on) hudWas = Hud.instance.m_userHidden;
            else Hud.instance.m_userHidden = hudWas;
            active = on;
            nextSweep = 0;
            if (on) return;
            foreach (LineRenderer line in Switched) if (line) line.enabled = true;
            Switched.Clear();
        }

        /// <summary>Each LateUpdate, after the game's own Update has shown what it shows.</summary>
        internal static void Apply()
        {
            if (!active) return;
            if (Hud.instance) Hud.instance.m_userHidden = true;
            if (Chat.instance && Chat.instance.m_chatWindow) Chat.instance.m_chatWindow.gameObject.SetActive(false);
            if (InventoryGui.IsVisible()) InventoryGui.instance.Hide();
            if (Time.frameCount < nextSweep) return;
            nextSweep = Time.frameCount + 3;
            HideDebugLines();
        }

        /// <summary>Every line DevBridge draws (hitbox shapes, overlay), under any of its roots, any copy of it.</summary>
        private static void HideDebugLines()
        {
            foreach (LineRenderer line in Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None))
            {
                if (!line.enabled || !line.transform.root.name.StartsWith("DevBridge_")) continue;
                line.enabled = false;
                Switched.Add(line);
            }
        }
    }
}
