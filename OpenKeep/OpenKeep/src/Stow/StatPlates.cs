using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Stow
{
    /// <summary>
    /// The game's two readouts on the player panel's right, armour above weight: each is a direct child of
    /// <c>m_player</c> holding a wood background, an icon and the text the game rewrites every frame (<c>m_armor</c>,
    /// <c>m_weight</c>). Both are pinned to the panel's top-right corner where the prefab puts them, so the panel
    /// growing downward with bought inventory rows (<c>InventoryGui.SetInventorySize</c>) no longer pulls them apart
    /// (the prefab anchors armour to the right edge's middle and weight to the bottom-right corner). Each icon is
    /// enlarged and centred behind its number. The trash can's plate (<see cref="TrashPlate"/>) goes at
    /// <see cref="Middle"/>.
    /// </summary>
    public sealed class StatPlates
    {
        /// <summary>Icon edge in panel units; the plates are 80x64 and the game's icons were 32.</summary>
        public const float IconSize = 48f;

        private StatPlates(RectTransform armor, RectTransform weight)
        {
            Armor = armor;
            Weight = weight;
        }

        public RectTransform Armor { get; }

        public RectTransform Weight { get; }

        /// <summary>Halfway between the two plates; both are anchored to the same corner after <see cref="Arrange"/>.</summary>
        public Vector2 Middle => (Armor.anchoredPosition + Weight.anchoredPosition) / 2f;

        /// <summary>Finds both plates, pins them and restyles their icons; null, with nothing changed, when either
        /// plate is missing.</summary>
        public static StatPlates Arrange(InventoryGui gui)
        {
            RectTransform armor = ChildHolding(gui.m_player, gui.m_armor) as RectTransform;
            RectTransform weight = ChildHolding(gui.m_player, gui.m_weight) as RectTransform;
            if (armor == null || weight == null || armor == weight)
                return null;
            PinTopRight(armor, gui.m_player);
            PinTopRight(weight, gui.m_player);
            CentreIcon(armor, gui.m_armor);
            CentreIcon(weight, gui.m_weight);
            Plugin.Log.LogInfo($"stat plates pinned to the player panel's top-right: armour {armor.anchoredPosition}, weight {weight.anchoredPosition}, icons {IconSize:0} px");
            return new StatPlates(armor, weight);
        }

        /// <summary>The direct child of <paramref name="parent"/> that is or contains <paramref name="part"/>; null
        /// when the part is not below the parent.</summary>
        public static Transform ChildHolding(Transform parent, Component part)
        {
            if (parent == null || part == null)
                return null;
            Transform t = part.transform;
            while (t != null && t.parent != parent)
                t = t.parent;
            return t;
        }

        /// <summary>The plate's wood: its largest child that draws an Image.</summary>
        public static Image BackgroundOf(Transform plate)
        {
            Image largest = null;
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
        public static Image IconOf(Transform plate, Transform textChild)
        {
            Image background = BackgroundOf(plate);
            foreach (Transform child in plate)
            {
                Image image = child.GetComponent<Image>();
                if (image != null && image != background && child != textChild)
                    return image;
            }
            return null;
        }

        /// <summary>Re-anchors the plate to the panel's top-right corner, leaving it where it is.</summary>
        private static void PinTopRight(RectTransform plate, RectTransform panel)
        {
            Vector2 size = plate.rect.size;
            Vector2 pivot = plate.localPosition;
            plate.anchorMin = Vector2.one;
            plate.anchorMax = Vector2.one;
            plate.sizeDelta = size;
            plate.anchoredPosition = pivot - panel.rect.max;
        }

        /// <summary>Enlarges the icon and centres it on the number, drawn before the text so the number is on top.</summary>
        private static void CentreIcon(RectTransform plate, TMP_Text text)
        {
            Transform textChild = ChildHolding(plate, text);
            Image icon = IconOf(plate, textChild);
            if (icon == null)
                return;
            RectTransform rect = icon.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(IconSize, IconSize);
            rect.anchoredPosition = CentreIn(plate, text.rectTransform) - plate.rect.center;
            icon.preserveAspect = true;
            if (textChild != null && rect.GetSiblingIndex() > textChild.GetSiblingIndex())
                rect.SetSiblingIndex(textChild.GetSiblingIndex());
        }

        /// <summary>The centre of <paramref name="rect"/> in the plate's local space.</summary>
        private static Vector2 CentreIn(RectTransform plate, RectTransform rect)
        {
            Vector3 world = rect.TransformPoint(rect.rect.center);
            return plate.InverseTransformPoint(world);
        }
    }
}
