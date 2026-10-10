using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DevBridge.Studio
{
    /// <summary>
    /// The booth's light: a warm key from the upper left and a cool rim from behind, on the booth's layer only and on for
    /// one render. For that render the game's sun is off and its ambient, fog and shader globals (the sun's direction and
    /// colour, wetness) are the booth's, so a picture looks the same at noon, at night and in the rain; all of it goes
    /// back straight after, before the game draws its own frame.
    /// </summary>
    internal static class BoothLight
    {
        private static readonly int SunDir = Shader.PropertyToID("_SunDir");
        private static readonly int SunColor = Shader.PropertyToID("_SunColor");
        private static readonly int AmbientColor = Shader.PropertyToID("_AmbientColor");
        private static readonly int Wet = Shader.PropertyToID("_Wet");
        private static readonly Color Ambient = new Color(0.42f, 0.44f, 0.48f);

        private static Light key;
        private static Light rim;

        internal static void Make(Transform parent, int layer)
        {
            key = Lamp(parent, "key", Quaternion.Euler(40f, 25f, 0f), new Color(1f, 0.96f, 0.9f), 1.15f, layer);
            rim = Lamp(parent, "rim", Quaternion.Euler(20f, 200f, 0f), new Color(0.75f, 0.85f, 1f), 0.6f, layer);
        }

        private static Light Lamp(Transform parent, string name, Quaternion turn, Color color, float intensity, int layer)
        {
            Light lamp = new GameObject(name).AddComponent<Light>();
            lamp.transform.SetParent(parent, false);
            lamp.transform.rotation = turn;
            lamp.type = LightType.Directional;
            lamp.renderMode = LightRenderMode.ForcePixel;
            (lamp.color, lamp.intensity, lamp.shadows) = (color, intensity, LightShadows.None);
            (lamp.cullingMask, lamp.enabled) = (1 << layer, false);
            return lamp;
        }

        /// <summary>Runs the render under the booth's light.</summary>
        internal static void During(Action render)
        {
            GameLighting saved = GameLighting.Take();
            try
            {
                Set();
                render();
            }
            finally
            {
                key.enabled = rim.enabled = false;
                saved.Restore();
            }
        }

        private static void Set()
        {
            if (GameLighting.Sun) GameLighting.Sun.enabled = false;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Ambient;
            Shader.SetGlobalVector(SunDir, -key.transform.forward);
            Shader.SetGlobalColor(SunColor, key.color * key.intensity);
            Shader.SetGlobalColor(AmbientColor, Ambient);
            Shader.SetGlobalFloat(Wet, 0f);
            key.enabled = rim.enabled = true;
        }

        /// <summary>The game's lighting as it was before a render.</summary>
        private struct GameLighting
        {
            private bool sunOn;
            private bool fog;
            private AmbientMode mode;
            private Color ambient;
            private Vector4 sunDir;
            private Color sunColor;
            private Color ambientColor;
            private float wet;

            internal static Light Sun => EnvMan.instance ? EnvMan.instance.m_dirLight : null;

            internal static GameLighting Take() => new GameLighting
            {
                sunOn = Sun && Sun.enabled, fog = RenderSettings.fog, mode = RenderSettings.ambientMode,
                ambient = RenderSettings.ambientLight, sunDir = Shader.GetGlobalVector(SunDir), sunColor = Shader.GetGlobalColor(SunColor),
                ambientColor = Shader.GetGlobalColor(AmbientColor), wet = Shader.GetGlobalFloat(Wet),
            };

            internal void Restore()
            {
                if (Sun) Sun.enabled = sunOn;
                (RenderSettings.fog, RenderSettings.ambientMode, RenderSettings.ambientLight) = (fog, mode, ambient);
                Shader.SetGlobalVector(SunDir, sunDir);
                Shader.SetGlobalColor(SunColor, sunColor);
                Shader.SetGlobalColor(AmbientColor, ambientColor);
                Shader.SetGlobalFloat(Wet, wet);
            }
        }
    }
}
