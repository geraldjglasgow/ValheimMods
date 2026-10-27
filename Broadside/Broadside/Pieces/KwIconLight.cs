using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// The light a hammer icon is rendered under, the same at noon, at night, in the rain and while the world loads: a
    /// warm key from the picture's upper left, a cool fill from its right and a rim from behind, all three turned with
    /// the view, on the icon layer only and on for one render. For that render every other directional light that
    /// reaches the layer (the game's sun or moon) is off, fog is off, the ambient is a flat grey, and the game's own
    /// lighting values that its shaders read (the sun's direction and colour, the ambient, the wetness of rain) are the
    /// icon's; all of it goes back straight after, before the game draws its next frame.
    /// </summary>
    public sealed class KwIconLight
    {
        private static readonly int SunDir = Shader.PropertyToID("_SunDir");
        private static readonly int SunColor = Shader.PropertyToID("_SunColor");
        private static readonly int AmbientColor = Shader.PropertyToID("_AmbientColor");
        private static readonly int Wet = Shader.PropertyToID("_Wet");
        private static readonly Color Ambient = new Color(0.44f, 0.43f, 0.42f);

        // Each light's direction of travel in the view's frame (x right, y up, z into the picture).
        private static readonly Vector3 KeyWay = new Vector3(0.55f, -0.7f, 0.45f), FillWay = new Vector3(-0.8f, -0.1f, 0.6f), RimWay = new Vector3(-0.25f, -0.55f, -0.8f);

        private readonly int layer;
        private readonly Light key, fill, rim;

        public KwIconLight(Transform parent, int layer)
        {
            this.layer = layer;
            key = Lamp(parent, "key", new Color(1f, 0.95f, 0.86f), 1.15f);
            fill = Lamp(parent, "fill", new Color(0.8f, 0.86f, 1f), 0.35f);
            rim = Lamp(parent, "rim", new Color(1f, 0.97f, 0.92f), 0.5f);
        }

        private Light Lamp(Transform parent, string name, Color color, float intensity)
        {
            Light lamp = new GameObject("KwIconLight_" + name).AddComponent<Light>();
            lamp.transform.SetParent(parent, false);
            lamp.type = LightType.Directional;
            lamp.renderMode = LightRenderMode.ForcePixel;
            lamp.color = color;
            lamp.intensity = intensity;
            lamp.shadows = LightShadows.None;
            lamp.cullingMask = 1 << layer;
            lamp.enabled = false;
            return lamp;
        }

        /// <summary>Turns the three lights with the view (the camera's rotation).</summary>
        public void Face(Quaternion view)
        {
            key.transform.rotation = Quaternion.LookRotation(view * KeyWay.normalized);
            fill.transform.rotation = Quaternion.LookRotation(view * FillWay.normalized);
            rim.transform.rotation = Quaternion.LookRotation(view * RimWay.normalized);
        }

        /// <summary>Runs one render under the icon's light, the game's lighting set aside and put back after.</summary>
        public void During(Action render)
        {
            GameLighting saved = GameLighting.Take(layer);
            try
            {
                saved.SetAside();
                Set();
                render();
            }
            finally
            {
                key.enabled = fill.enabled = rim.enabled = false;
                saved.Restore();
            }
        }

        private void Set()
        {
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Ambient;
            Shader.SetGlobalVector(SunDir, -key.transform.forward);
            Shader.SetGlobalColor(SunColor, key.color * key.intensity);
            Shader.SetGlobalColor(AmbientColor, Ambient);
            Shader.SetGlobalFloat(Wet, 0f);
            key.enabled = fill.enabled = rim.enabled = true;
        }

        /// <summary>The game's lighting as it was before a render: what the icon's light changes, and the lights it turns off.</summary>
        private sealed class GameLighting
        {
            private readonly List<Light> lights = new List<Light>();
            private bool fog;
            private AmbientMode mode;
            private Color ambient, sunColor, ambientColor;
            private Vector4 sunDir;
            private float wet;

            public static GameLighting Take(int layer)
            {
                GameLighting saved = new GameLighting
                {
                    fog = RenderSettings.fog, mode = RenderSettings.ambientMode, ambient = RenderSettings.ambientLight,
                    sunDir = Shader.GetGlobalVector(SunDir), sunColor = Shader.GetGlobalColor(SunColor),
                    ambientColor = Shader.GetGlobalColor(AmbientColor), wet = Shader.GetGlobalFloat(Wet),
                };
                foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (light.type == LightType.Directional && light.isActiveAndEnabled && (light.cullingMask & (1 << layer)) != 0)
                        saved.lights.Add(light);
                }
                return saved;
            }

            // The sun and anything else that would light the icon from the world.
            public void SetAside()
            {
                foreach (Light light in lights)
                    light.enabled = false;
            }

            public void Restore()
            {
                foreach (Light light in lights)
                {
                    if (light != null)
                        light.enabled = true;
                }
                RenderSettings.fog = fog;
                RenderSettings.ambientMode = mode;
                RenderSettings.ambientLight = ambient;
                Shader.SetGlobalVector(SunDir, sunDir);
                Shader.SetGlobalColor(SunColor, sunColor);
                Shader.SetGlobalColor(AmbientColor, ambientColor);
                Shader.SetGlobalFloat(Wet, wet);
            }
        }
    }
}
