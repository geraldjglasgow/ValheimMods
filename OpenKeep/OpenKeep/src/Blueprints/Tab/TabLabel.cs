using TMPro;
using UnityEngine;

namespace OpenKeep.Blueprints.Tab
{
    /// <summary>
    /// The name on a piece button of the Blueprints tab: a dark band along the bottom of the icon holding the entry's
    /// name in the menu's font, outlined, at most two lines and cut with an ellipsis; the band is as tall as the lines
    /// it needs. It is a child of the game's button, so it is set again every time the menu sets the button up for a
    /// piece (buttons are reused) and hidden for any piece that has no name to show.
    /// </summary>
    public static class TabLabel
    {
        private const string Name = "OpenKeep Label";
        private const float FontSize = 11f;

        /// <summary>The width the text wraps in: the 64 px cell less the band's and the text's insets.</summary>
        private const float TextWidth = 54f;

        private static float twoLines;

        /// <summary>Shows the name on the button, or hides the band for null.</summary>
        public static void Show(BuildUiPieceButton button, string text)
        {
            Transform band = button.transform.Find(Name);
            if (string.IsNullOrEmpty(text) || TabLook.Font == null)
            {
                if (band != null)
                    band.gameObject.SetActive(false);
                return;
            }
            if (band == null)
                band = Make(button.transform);
            TMP_Text label = band.GetComponentInChildren<TMP_Text>(true);
            label.text = text;
            float height = Mathf.Min(label.GetPreferredValues(text, TextWidth, 0f).y, TwoLines(label)) + 3f;
            ((RectTransform)band).sizeDelta = new Vector2(-4f, height);
            band.gameObject.SetActive(true);
        }

        /// <summary>The band: anchored to the bottom of the button, inset 2 px, with the text inside.</summary>
        private static Transform Make(Transform button)
        {
            RectTransform band = TabLook.Image(button, Name, TabLook.Band).rectTransform;
            band.anchorMin = new Vector2(0f, 0f);
            band.anchorMax = new Vector2(1f, 0f);
            band.pivot = new Vector2(0.5f, 0f);
            band.anchoredPosition = new Vector2(0f, 2f);
            TMP_Text text = TabLook.Text(band, "Text", FontSize);
            text.rectTransform.offsetMin = new Vector2(2f, 1f);
            text.rectTransform.offsetMax = new Vector2(-2f, -1f);
            return band;
        }

        /// <summary>The height of two lines in the label's font, measured once.</summary>
        private static float TwoLines(TMP_Text label)
        {
            if (twoLines <= 0f)
                twoLines = label.GetPreferredValues("Ag\nAg", TextWidth, 0f).y;
            return twoLines;
        }
    }
}
