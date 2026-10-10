using System.Collections.Generic;
using DevBridge.Server;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>
    /// A set's lighting for a shot: every light within a radius (fires, sconces, braziers) scaled in intensity and
    /// range, on this machine only, and put back when the shot ends. {"lights": {"near": spot, "radius": 40,
    /// "scale": 0.35, "range": 0.8}}. A light's flicker keeps its own base, so the base is scaled too.
    /// </summary>
    internal static class SetLights
    {
        private sealed class Saved
        {
            internal Light Lamp;
            internal LightFlicker Flicker;
            internal float Intensity, Range, Base;
        }

        private static readonly List<Saved> Changed = new List<Saved>();

        internal static void Apply(JObject spec, ShotFrame frame)
        {
            Vector3 near = Spot.Parse(spec["near"], "lights near")?.Resolve(frame) ?? frame.Origin;
            float radius = spec.Value<float?>("radius") ?? 40f, scale = spec.Value<float?>("scale") ?? 1f, range = spec.Value<float?>("range") ?? 1f;
            foreach (Light lamp in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (lamp.type == LightType.Directional || Vector3.Distance(lamp.transform.position, near) > radius) continue;
                Saved saved = Remember(lamp);
                lamp.intensity = saved.Intensity * scale;
                lamp.range = saved.Range * range;
                if (saved.Flicker) saved.Flicker.m_baseIntensity = saved.Base * scale;
            }
        }

        private static Saved Remember(Light lamp)
        {
            Saved saved = Changed.Find(s => s.Lamp == lamp);
            if (saved != null) return saved;
            LightFlicker flicker = lamp.GetComponent<LightFlicker>();
            saved = new Saved { Lamp = lamp, Flicker = flicker, Intensity = lamp.intensity, Range = lamp.range, Base = flicker ? flicker.m_baseIntensity : 0f };
            Changed.Add(saved);
            return saved;
        }

        internal static void Restore()
        {
            foreach (Saved saved in Changed)
            {
                if (!saved.Lamp) continue;
                (saved.Lamp.intensity, saved.Lamp.range) = (saved.Intensity, saved.Range);
                if (saved.Flicker) saved.Flicker.m_baseIntensity = saved.Base;
            }
            Changed.Clear();
        }
    }
}
