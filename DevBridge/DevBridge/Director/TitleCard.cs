using System;
using System.Linq;
using DevBridge.Server;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DevBridge.Director
{
    /// <summary>
    /// Film cards drawn over the game: a black veil (fade to and from black) and a line of text in one of the game's own
    /// fonts, each eased to its target opacity over film seconds.
    /// {"title": {"text": "ELITE CREATURES PACK", "font": "Norse", "size": 120, "color": [r,g,b], "y": 0, "fade": 1}},
    /// {"title": "off"}, {"fade": {"to": 1, "over": 0.5}} (1 is black).
    /// </summary>
    internal static class TitleCard
    {
        private static GameObject root;
        private static Image veil;
        private static TextMeshProUGUI text;
        private static Ramp veilRamp, textRamp;
        private static Color veilColor = Color.black;
        private static float keepUntil;

        internal static void Title(JToken spec)
        {
            Ensure();
            if (spec.Type == JTokenType.String && spec.Value<string>() == "off") { textRamp = Ramp.To(text.alpha, 0f, 0.4f); return; }
            JObject s = spec as JObject ?? throw new BridgeException("title takes an object or \"off\"");
            text.text = s.Value<string>("text") ?? "";
            text.font = Font(s.Value<string>("font") ?? "Norse");
            text.fontSize = s.Value<float?>("size") ?? 110f;
            text.characterSpacing = s.Value<float?>("spacing") ?? 6f;
            Color color = s["color"] is JArray c ? new Color(c[0].Value<float>(), c[1].Value<float>(), c[2].Value<float>()) : new Color(0.92f, 0.86f, 0.72f);
            text.color = new Color(color.r, color.g, color.b, text.alpha);
            text.rectTransform.anchoredPosition = new Vector2(0f, s.Value<float?>("y") ?? 0f);
            textRamp = Ramp.To(text.alpha, s.Value<float?>("alpha") ?? 1f, s.Value<float?>("fade") ?? 1f);
        }

        internal static void Fade(JToken spec)
        {
            Ensure();
            JObject s = spec as JObject;
            if (s?["color"] is JArray c) veilColor = new Color(c[0].Value<float>(), c[1].Value<float>(), c[2].Value<float>());
            veilRamp = Ramp.To(veil.color.a, s?.Value<float?>("to") ?? spec.Value<float>(), s?.Value<float?>("over") ?? 0f);
            keepUntil = 0f;
        }

        /// <summary>
        /// At a shot's end: a full veil (it ended on white or black) stays up, so the next shot opens out of it with no
        /// glimpse of the set change between (a teleport, a loading screen); gone after 20 s if no shot takes it over.
        /// </summary>
        internal static void EndShot()
        {
            if (!root || veil.color.a < 0.99f) { Clear(); return; }
            keepUntil = Time.realtimeSinceStartup + 20f;
            textRamp = Ramp.To(text.alpha, 0f, 0f);
        }

        internal static void Tick(float delta)
        {
            if (!root) return;
            if (keepUntil > 0f && Time.realtimeSinceStartup > keepUntil) { Clear(); return; }
            if (veilRamp != null) veil.color = new Color(veilColor.r, veilColor.g, veilColor.b, veilRamp.Step(delta));
            if (textRamp != null) text.alpha = textRamp.Step(delta);
        }

        internal static void Clear()
        {
            if (root) Object.Destroy(root);
            (root, veilRamp, textRamp, veilColor, keepUntil) = (null, null, null, Color.black, 0f);
        }

        private static TMP_FontAsset Font(string name)
        {
            TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            return fonts.FirstOrDefault(f => f.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                ?? fonts.FirstOrDefault(f => f.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                ?? throw new BridgeException($"no font {name}; the game has {string.Join(", ", fonts.Select(f => f.name).Distinct())}");
        }

        private static void Ensure()
        {
            if (root) return;
            root = new GameObject("DirectorTitle");
            Object.DontDestroyOnLoad(root);
            Canvas canvas = root.AddComponent<Canvas>();
            (canvas.renderMode, canvas.sortingOrder) = (RenderMode.ScreenSpaceOverlay, 32000);
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            (scaler.uiScaleMode, scaler.referenceResolution, scaler.matchWidthOrHeight) = (CanvasScaler.ScaleMode.ScaleWithScreenSize, new Vector2(1920f, 1080f), 1f);
            veil = Child<Image>("veil");
            veil.color = new Color(0f, 0f, 0f, 0f);
            text = Child<TextMeshProUGUI>("text");
            (text.alignment, text.alpha, text.enableWordWrapping) = (TextAlignmentOptions.Center, 0f, false);
        }

        private static T Child<T>(string name) where T : Graphic
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(root.transform, false);
            var rect = (RectTransform)go.transform;
            (rect.anchorMin, rect.anchorMax, rect.offsetMin, rect.offsetMax) = (Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            T graphic = go.AddComponent<T>();
            graphic.raycastTarget = false;
            return graphic;
        }

        /// <summary>An eased change of one value over film seconds.</summary>
        private sealed class Ramp
        {
            private float from, to, over, at;

            internal static Ramp To(float from, float to, float over) => new Ramp { from = from, to = to, over = Mathf.Max(0f, over) };

            internal float Step(float delta)
            {
                at += delta;
                return over <= 0f ? to : Mathf.Lerp(from, to, CameraMove.Eased(Mathf.Clamp01(at / over), "inout"));
            }
        }
    }
}
