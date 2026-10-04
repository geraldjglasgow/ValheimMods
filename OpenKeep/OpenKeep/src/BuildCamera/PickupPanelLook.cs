using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.BuildCamera
{
    /// <summary>
    /// The build menu's look for the pickup panel, taken from the game's own HUD at runtime: the largest sprite image of
    /// the piece selection window (its background) and the text style of the selected piece's name. When the game
    /// changes its HUD and no background is found, a plain dark panel stands in.
    /// </summary>
    public static class PickupPanelLook
    {
        private static readonly Color Fallback = new Color(0f, 0f, 0f, 0.7f);

        public static void Background(Image image, Hud hud)
        {
            Image like = Largest(hud.m_pieceSelectionWindow);
            image.raycastTarget = false;
            if (like == null)
            {
                image.color = Fallback;
                return;
            }
            image.sprite = like.sprite;
            image.type = like.type;
            image.color = like.color;
            image.material = like.material;
            image.pixelsPerUnitMultiplier = like.pixelsPerUnitMultiplier;
        }

        public static void Text(TMP_Text text, TMP_Text like, float share)
        {
            text.font = like.font;
            text.fontSharedMaterial = like.fontSharedMaterial;
            text.color = like.color;
            text.fontSize = like.fontSize * share;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
        }

        private static Image Largest(GameObject window)
        {
            if (window == null)
                return null;
            Image best = null;
            float bestArea = 0f;
            foreach (Image image in window.GetComponentsInChildren<Image>(true))
            {
                Rect rect = image.rectTransform.rect;
                float area = rect.width * rect.height;
                if (image.sprite != null && area > bestArea)
                {
                    best = image;
                    bestArea = area;
                }
            }
            return best;
        }
    }
}
