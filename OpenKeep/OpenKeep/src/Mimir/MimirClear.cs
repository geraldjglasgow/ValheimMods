using GUIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// The × inside the right end of Mímir's search field: shown while the field holds text, a click empties it (the
    /// field's own change event then clears the search, as typing would). Drawn in the field's own font and colour.
    /// </summary>
    public static class MimirClear
    {
        public const float Size = 22f;

        public static void Add(GuiInputField field)
        {
            // Made inactive so the text wakes with its font set: TextMeshPro looks for its default font when it wakes without
            // one and warns that LiberationSans is missing.
            GameObject go = new GameObject("OpenKeep_MimirClear", typeof(RectTransform));
            go.SetActive(false);
            go.AddComponent<TextMeshProUGUI>();
            go.AddComponent<Button>();
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(field.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-4f, 0f);
            rect.sizeDelta = new Vector2(Size, Size);
            TextMeshProUGUI cross = go.GetComponent<TextMeshProUGUI>();
            Style(cross, field.textComponent);
            Button button = go.GetComponent<Button>();
            button.targetGraphic = cross;
            button.onClick.AddListener(() => field.text = "");
            field.onValueChanged.AddListener(text => go.SetActive(!string.IsNullOrEmpty(text)));
        }

        private static void Style(TextMeshProUGUI cross, TMP_Text like)
        {
            if (like != null)
            {
                cross.font = like.font;
                cross.color = like.color;
            }
            cross.text = "×";
            cross.fontSize = 22f;
            cross.alignment = TextAlignmentOptions.Center;
        }
    }
}
