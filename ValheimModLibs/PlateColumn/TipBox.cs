using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlateColumn
{
    /// <summary>
    /// The box a plate's tooltip shows: an instance of the game's tooltip prefab (the inventory item tooltip: a dark
    /// panel, its first child, holding a Topic and a Text) with a thin gold border, placed once - its left edge a few
    /// units right of the plate, centred on the plate's height - and left there. When that would leave the screen it
    /// goes left of the plate instead, and it is shifted up or down to stay on screen. It never takes the pointer.
    /// </summary>
    internal static class TipBox
    {
        private const float Gap = 6f;
        private static readonly Color BorderColour = new Color(0.85f, 0.66f, 0.36f, 0.9f);
        private static readonly Vector2 BorderWidth = new Vector2(2f, -2f);

        public static GameObject? Show(GameObject? prefab, RectTransform plate, string topic, string text)
        {
            Canvas? canvas = plate.GetComponentInParent<Canvas>();
            if (prefab == null || canvas == null)
            {
                return null;
            }
            GameObject box = Object.Instantiate(prefab, canvas.transform, false);
            box.name = "PlateColumn_tip";
            CanvasGroup group = box.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            Write(box.transform, "Topic", topic);
            Write(box.transform, "Text", text);
            box.SetActive(true);
            if (box.transform.childCount > 0 && box.transform.GetChild(0) is RectTransform panel)
            {
                AddBorder(panel);
                Place(panel, plate);
            }
            return box;
        }

        private static void Write(Transform root, string name, string words)
        {
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name == name)
                {
                    text.text = Localization.instance != null ? Localization.instance.Localize(words) : words;
                }
            }
        }

        private static void AddBorder(RectTransform panel)
        {
            if (panel.GetComponent<Graphic>() == null)
            {
                return;
            }
            Outline border = panel.gameObject.AddComponent<Outline>();
            border.effectColor = BorderColour;
            border.effectDistance = BorderWidth;
        }

        /// <summary>Sizes the panel to its text first (its layout would otherwise run after this frame), then pins it.</summary>
        private static void Place(RectTransform panel, RectTransform plate)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            Vector3[] corners = new Vector3[4];
            plate.GetWorldCorners(corners);
            float gap = Gap * panel.lossyScale.x;
            panel.pivot = new Vector2(0f, 0.5f);
            panel.position = (corners[2] + corners[3]) / 2f + Vector3.right * gap;
            Vector3[] own = new Vector3[4];
            panel.GetWorldCorners(own);
            if (own[2].x > Screen.width)
            {
                panel.pivot = new Vector2(1f, 0.5f);
                panel.position = (corners[0] + corners[1]) / 2f + Vector3.left * gap;
                panel.GetWorldCorners(own);
            }
            float up = Mathf.Max(0f, -own[0].y) - Mathf.Max(0f, own[1].y - Screen.height);
            panel.position += Vector3.up * up;
        }
    }
}
