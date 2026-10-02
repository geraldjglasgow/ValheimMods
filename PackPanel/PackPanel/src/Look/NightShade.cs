using System.Collections.Generic;
using PackPanel.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PackPanel.Look
{
    /// <summary>
    /// PackPanel's panels darken with the light around the player, as the game's own wooden panels do (their
    /// <c>litpanel</c> material reads the sun and ambient colours <c>EnvMan</c> writes every frame: <c>_SunColor</c>,
    /// <c>_AmbientColor</c>). Our skins draw with the default UI material, so they never darkened; here the same two
    /// colours give one brightness, grey only (the game's tint turns the panels purple near some bosses), mapped so a
    /// clear Meadows noon is full brightness and a clear midnight is <see cref="InventorySettings.NightShade"/>; dark
    /// weather lands in between or below, never under that setting's floor. It is applied as each graphic's
    /// <c>CanvasRenderer</c> colour, which multiplies its own colour without changing it, and only to graphics on the
    /// default material, so anything still drawn with the game's lit material is not shaded twice. The user's request,
    /// 2026-10-02. Inventory cells are never shaded (the user's request, same day): their Button's hover tint writes the
    /// same <c>CanvasRenderer</c> colour, so a shaded cell lost its shade on hover and could stay light.
    /// </summary>
    public static class NightShade
    {
        private static readonly int SunId = Shader.PropertyToID("_SunColor");
        private static readonly int AmbientId = Shader.PropertyToID("_AmbientColor");

        /// <summary>How much the sun counts beside the ambient light.</summary>
        private const float SunWeight = 0.25f;

        /// <summary>The light of a clear Meadows midnight (ambient 0.37, sun 0.20, read in game 2026-10-02).</summary>
        private const float NightLight = 0.42f;

        /// <summary>The light of a clear Meadows noon (ambient #7592B4, sun #FFC47B x 1.7, the game's weather data).</summary>
        private const float DayLight = 0.90f;

        /// <summary>The most the shade changes per second, so a weather change fades in.</summary>
        private const float Fade = 0.5f;

        private static readonly List<Graphic> shaded = new List<Graphic>();
        private static readonly HashSet<Graphic> known = new HashSet<Graphic>();
        private static float current = 1f;

        /// <summary>Shade this graphic from now on, at the current shade straight away (again after a re-skin).</summary>
        public static void Track(Graphic graphic)
        {
            if (graphic == null)
                return;
            if (known.Add(graphic))
                shaded.Add(graphic);
            Paint(graphic, current);
        }

        /// <summary>Plugin.Update: follows the light, and repaints only when the shade moved.</summary>
        public static void Update()
        {
            float target = Target();
            if (Mathf.Abs(target - current) < 0.004f)
                return;
            current = Mathf.MoveTowards(current, target, Fade * Time.unscaledDeltaTime);
            for (int i = shaded.Count - 1; i >= 0; i--)
            {
                Graphic graphic = shaded[i];
                if (graphic == null)
                {
                    known.Remove(graphic);
                    shaded.RemoveAt(i);
                    continue;
                }
                Paint(graphic, current);
            }
        }

        /// <summary>
        /// The shade the light asks for: 1 by day, the setting at a clear midnight; 1 with PackPanel's look off (the game's
        /// own panels shade themselves) and outside a world.
        /// </summary>
        private static float Target()
        {
            float night = InventorySettings.NightShade.Value;
            if (night >= 1f || !GridSkin.On || EnvMan.instance == null)
                return 1f;
            float light = Luminance(Shader.GetGlobalColor(AmbientId)) + SunWeight * Luminance(Shader.GetGlobalColor(SunId));
            return Mathf.Lerp(night, 1f, Mathf.Clamp01((light - NightLight) / (DayLight - NightLight)));
        }

        private static float Luminance(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

        private static void Paint(Graphic graphic, float shade)
        {
            float value = graphic.material == Graphic.defaultGraphicMaterial ? shade : 1f;
            Color tint = new Color(value, value, value, 1f);
            if (graphic.canvasRenderer.GetColor() != tint)
                graphic.canvasRenderer.SetColor(tint);
        }
    }
}
