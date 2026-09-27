using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlateColumn
{
    /// <summary>A plate a mod added: its rect on the panel, its icon, and its text when it keeps one.</summary>
    public sealed class Plate
    {
        public Plate(RectTransform rect, Image icon, TMP_Text? text)
        {
            Rect = rect;
            Icon = icon;
            Text = text;
        }

        public RectTransform Rect { get; }

        public Image Icon { get; }

        public TMP_Text? Text { get; }
    }
}
