using UnityEngine;
using UnityEngine.UI;

namespace GrindstoneSkills
{
    /// <summary>
    /// The info pane's scrollbar, along the pane's right edge beside the page text. Made new rather than copied from the
    /// list's, so it carries none of that bar's wiring; it borrows only its track and handle sprites and colours. The
    /// pane's scroll view drives it and hides it while the page fits.
    /// </summary>
    internal static class PaneScrollbar
    {
        public static Scrollbar Build(RectTransform pane, Scrollbar like, float width, float padding, float top)
        {
            RectTransform track = Part("scrollbar", pane);
            track.anchorMin = new Vector2(1f, 0f);
            track.anchorMax = Vector2.one;
            track.pivot = new Vector2(1f, 0.5f);
            track.offsetMin = new Vector2(-padding / 2f - width, padding);
            track.offsetMax = new Vector2(-padding / 2f, -(top + 2f));
            Image trackImage = Look(track.gameObject.AddComponent<Image>(), like != null ? like.GetComponent<Image>() : null, new Color(0f, 0f, 0f, 0.4f));
            RectTransform handle = Part("handle", Part("sliding", track));
            Image handleImage = Look(handle.gameObject.AddComponent<Image>(), like != null && like.handleRect != null ? like.handleRect.GetComponent<Image>() : null,
                new Color(0.85f, 0.6f, 0.3f, 1f));
            trackImage.raycastTarget = true;
            Scrollbar bar = track.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handle;
            bar.targetGraphic = handleImage;
            bar.direction = Scrollbar.Direction.BottomToTop;
            return bar;
        }

        private static Image Look(Image image, Image source, Color fallback)
        {
            image.sprite = source != null ? source.sprite : null;
            image.type = source != null ? source.type : Image.Type.Simple;
            image.color = source != null ? source.color : fallback;
            return image;
        }

        /// <summary>A part filling its parent; the scrollbar sets the handle's own anchors as it moves.</summary>
        private static RectTransform Part(string name, Transform parent)
        {
            GameObject part = new GameObject(name, typeof(RectTransform));
            part.layer = parent.gameObject.layer;
            RectTransform rect = (RectTransform)part.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
