using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlateColumn
{
    /// <summary>
    /// How a plate is put together and the column's look: the wood is the plate's largest image, the icon is the other
    /// image that is not the text, and every icon is drawn large and centred behind its number (the game's icons are
    /// small and poke out above the plate).
    /// </summary>
    internal static class PlateStyle
    {
        /// <summary>Icon edge in panel units; the plates are 80x64 and the game's icons were 32.</summary>
        public const float IconSize = 48f;

        /// <summary>The direct child of <paramref name="parent"/> that is or contains <paramref name="part"/>.</summary>
        public static Transform? ChildHolding(Transform parent, Transform? part)
        {
            Transform? t = part;
            while (t != null && t.parent != parent)
            {
                t = t.parent;
            }
            return t;
        }

        /// <summary>The plate's wood: its largest child that draws an Image.</summary>
        public static Image? BackgroundOf(Transform plate)
        {
            Image? largest = null;
            float largestArea = 0f;
            foreach (Transform child in plate)
            {
                Image image = child.GetComponent<Image>();
                Rect rect = child is RectTransform r ? r.rect : default;
                if (image != null && rect.width * rect.height > largestArea)
                {
                    largest = image;
                    largestArea = rect.width * rect.height;
                }
            }
            return largest;
        }

        /// <summary>The plate's icon: the first child that draws an Image and is neither the wood nor the text.</summary>
        public static Image? IconOf(Transform plate, Transform? textChild)
        {
            Image? background = BackgroundOf(plate);
            foreach (Transform child in plate)
            {
                Image image = child.GetComponent<Image>();
                if (image != null && image != background && child != textChild)
                {
                    return image;
                }
            }
            return null;
        }

        /// <summary>Enlarges the icon and centres it on the number, drawn before the text so the number is on top.</summary>
        public static void CentreIcon(RectTransform plate, TMP_Text text)
        {
            Transform? textChild = ChildHolding(plate, text.transform);
            Image? icon = IconOf(plate, textChild);
            if (icon == null)
            {
                return;
            }
            RectTransform rect = icon.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(IconSize, IconSize);
            rect.anchoredPosition = CentreIn(plate, text.rectTransform) - plate.rect.center;
            icon.preserveAspect = true;
            if (textChild != null && rect.GetSiblingIndex() > textChild.GetSiblingIndex())
            {
                rect.SetSiblingIndex(textChild.GetSiblingIndex());
            }
        }

        /// <summary>The centre of <paramref name="rect"/> in the plate's local space.</summary>
        private static Vector2 CentreIn(RectTransform plate, RectTransform rect)
        {
            Vector3 world = rect.TransformPoint(rect.rect.center);
            return plate.InverseTransformPoint(world);
        }
    }
}
