using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DevBridge.Ui
{
    /// <summary>One line per GameObject: name, notable components, text, sprite and screen rectangle.</summary>
    internal static class UiDescribe
    {
        private static readonly HashSet<string> Quiet = new HashSet<string>
        {
            "Image", "RawImage", "Text", "TextMeshProUGUI", "TextMeshPro", "LayoutElement", "ContentSizeFitter",
            "HorizontalLayoutGroup", "VerticalLayoutGroup", "GridLayoutGroup", "CanvasScaler", "GraphicRaycaster",
            "Shadow", "Outline", "Mask", "RectMask2D", "AspectRatioFitter", "CanvasGroup",
        };

        internal static string Line(Transform t)
        {
            GameObject go = t.gameObject;
            string line = go.activeSelf ? go.name : go.name + " (off)";
            string tags = Tags(go);
            if (tags.Length > 0) line += " " + tags;
            string text = TextOf(go);
            if (!string.IsNullOrEmpty(text)) line += " \"" + Fmt.Clip(text, 80) + "\"";
            string sprite = SpriteOf(go);
            if (sprite != null) line += " img:" + sprite;
            return line + Rect(t);
        }

        internal static string TextOf(GameObject go)
        {
            TMP_Text tmp = go.GetComponent<TMP_Text>();
            if (tmp) return tmp.text;
            Text legacy = go.GetComponent<Text>();
            return legacy ? legacy.text : null;
        }

        private static string SpriteOf(GameObject go)
        {
            Image image = go.GetComponent<Image>();
            return image && image.enabled && image.sprite ? image.sprite.name : null;
        }

        /// <summary>Component names worth seeing, with (off), (disabled) and (alpha 0) marks.</summary>
        internal static string Tags(GameObject go)
        {
            var names = new List<string>();
            foreach (Behaviour behaviour in go.GetComponents<Behaviour>())
            {
                if (!behaviour) continue;
                string name = behaviour.GetType().Name;
                if (behaviour is CanvasGroup group && group.alpha < 0.01f) names.Add("CanvasGroup(alpha 0)");
                if (Quiet.Contains(name)) continue;
                if (!behaviour.enabled) name += "(off)";
                else if (behaviour is Selectable selectable && !selectable.interactable) name += "(disabled)";
                names.Add(name);
            }
            return names.Count == 0 ? "" : "[" + string.Join(",", names) + "]";
        }

        /// <summary>" @x,y wxh" in screen pixels from the top-left, the space screenshots and /click use.</summary>
        internal static string Rect(Transform t)
        {
            if (!(t is RectTransform rect) || !t.gameObject.activeInHierarchy) return "";
            Rect screen = ScreenRect(rect);
            return $" @{screen.x:0},{screen.y:0} {screen.width:0}x{screen.height:0}";
        }

        internal static Rect ScreenRect(RectTransform rect)
        {
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera camera = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return new Rect(min.x, Screen.height - max.y, max.x - min.x, max.y - min.y);
        }

        /// <summary>The centre of a UI element as a Unity screen point (bottom-left origin).</summary>
        internal static Vector2 Centre(GameObject go)
        {
            if (!(go.transform is RectTransform rect)) return new Vector2(Screen.width / 2f, Screen.height / 2f);
            Rect screen = ScreenRect(rect);
            return Fmt.ScreenPoint(screen.center.x, screen.center.y);
        }
    }
}
