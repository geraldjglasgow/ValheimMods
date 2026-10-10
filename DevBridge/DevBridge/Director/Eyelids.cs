using DevBridge.Server;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DevBridge.Director
{
    /// <summary>
    /// A blink drawn over the game: two dark lids with soft edges closing from the top and bottom of the screen and
    /// opening again, eased over film seconds. {"blink": {"to": 1, "over": 0.15}} closes them (0 opens). Lids still shut
    /// when a shot ends stay shut, so the next shot opens out of the dark with no glimpse of the set change between.
    /// </summary>
    internal static class Eyelids
    {
        private static GameObject root;
        private static RectTransform top, bottom;
        private static float from, to, over, at, shut, keepUntil;

        internal static void Cue(JToken spec)
        {
            Ensure();
            JObject s = spec as JObject ?? throw new BridgeException("blink takes {\"to\": 0..1, \"over\": seconds}");
            (from, to, over, at, keepUntil) = (shut, Mathf.Clamp01(s.Value<float?>("to") ?? 1f), Mathf.Max(0f, s.Value<float?>("over") ?? 0.15f), 0f, 0f);
            Apply();
        }

        internal static void Tick(float delta)
        {
            if (!root) return;
            if (keepUntil > 0f && Time.realtimeSinceStartup > keepUntil) { Clear(); return; }
            at += delta;
            Apply();
        }

        internal static void EndShot()
        {
            if (root && shut > 0.99f) keepUntil = Time.realtimeSinceStartup + 20f;
            else Clear();
        }

        internal static void Clear()
        {
            if (root) Object.Destroy(root);
            (root, shut, keepUntil) = (null, 0f, 0f);
        }

        private static void Apply()
        {
            shut = over <= 0f ? to : Mathf.Lerp(from, to, CameraMove.Eased(Mathf.Clamp01(at / over), "inout"));
            float height = shut * 0.62f;                  // a little past half: the soft edges meet fully dark
            top.anchorMin = new Vector2(0f, 1f - height);
            bottom.anchorMax = new Vector2(1f, height);
        }

        private static void Ensure()
        {
            if (root) return;
            root = new GameObject("DirectorEyelids");
            Object.DontDestroyOnLoad(root);
            Canvas canvas = root.AddComponent<Canvas>();
            (canvas.renderMode, canvas.sortingOrder) = (RenderMode.ScreenSpaceOverlay, 31999);
            Sprite lid = LidSprite();
            top = Lid("top", lid, new Vector2(0f, 1f), new Vector2(1f, 1f), false);
            bottom = Lid("bottom", lid, new Vector2(0f, 0f), new Vector2(1f, 0f), true);
        }

        private static RectTransform Lid(string name, Sprite sprite, Vector2 min, Vector2 max, bool flip)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(root.transform, false);
            var rect = (RectTransform)go.transform;
            (rect.anchorMin, rect.anchorMax, rect.offsetMin, rect.offsetMax) = (min, max, Vector2.zero, Vector2.zero);
            if (flip) rect.localScale = new Vector3(1f, -1f, 1f);
            Image image = go.AddComponent<Image>();
            (image.sprite, image.color, image.raycastTarget) = (sprite, new Color(0.01f, 0.008f, 0.008f, 1f), false);
            return rect;
        }

        /// <summary>Opaque at the screen's edge, its last fifth fading out: the soft rim of a lid.</summary>
        private static Sprite LidSprite()
        {
            var texture = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++)
                texture.SetPixel(0, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(y / 13f))));
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f));
        }
    }
}
