using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EliteCrafting.Display.Backdrops
{
    /// <summary>
    /// The rarity backdrop behind one item icon (display.md section 2, "Icon backdrop"): an Image of
    /// <see cref="BackdropArt"/> in <see cref="BackdropTone"/>, made once per icon as a sibling of it, just above its
    /// cell's own background (<c>bkg</c>) or first, so every marker and the icon draw over it. It covers the icon's rect
    /// less 3 of every 64 units on each side (58 of the game's 64-unit icon), follows the icon's rect, and shows only
    /// while the icon does. Display only, on the viewing client.
    /// </summary>
    internal static class IconBackdrop
    {
        private const string Name = "ecf_backdrop";
        private const float Inset = (64f - BackdropArt.Span) / 2f / 64f;

        private static readonly Dictionary<Image, Image> Made = new Dictionary<Image, Image>();
        private static readonly List<Image> Gone = new List<Image>();
        private static int _sweepAt = 64;

        /// <summary>Shows the backdrop for the item while the icon shows, else hides it. Called per frame by most surfaces.</summary>
        public static void Set(Image? icon, ItemDrop.ItemData? item)
        {
            if (icon == null)
            {
                return;
            }
            if (Visible(icon) && BackdropTone.TryGet(item, out Color tone))
            {
                Show(icon, tone);
            }
            else
            {
                Hide(icon);
            }
        }

        public static void Hide(Image? icon)
        {
            if (icon != null && Made.TryGetValue(icon, out Image backdrop) && backdrop != null && backdrop.enabled)
            {
                backdrop.enabled = false;
            }
        }

        /// <summary>The icon's backdrop, made on first use, fitted to the icon and shown in the tone.</summary>
        public static Image Show(Image icon, Color tone)
        {
            Image backdrop = Get(icon);
            Fit((RectTransform)backdrop.transform, icon.rectTransform);
            if (backdrop.color != tone)
            {
                backdrop.color = tone;
            }
            if (!backdrop.enabled)
            {
                backdrop.enabled = true;
            }
            return backdrop;
        }

        private static bool Visible(Image icon) =>
            icon.enabled && icon.gameObject.activeSelf && icon.sprite != null && icon.color.a > 0.01f;

        private static Image Get(Image icon)
        {
            if (Made.TryGetValue(icon, out Image backdrop) && backdrop != null)
            {
                return backdrop;
            }
            Sweep();
            backdrop = Make(icon);
            Made[icon] = backdrop;
            return backdrop;
        }

        /// <summary>Forgets icons the game destroyed (recipe lists are rebuilt, drag ghosts come and go), now and then.</summary>
        private static void Sweep()
        {
            if (Made.Count < _sweepAt)
            {
                return;
            }
            Gone.Clear();
            foreach (KeyValuePair<Image, Image> pair in Made)
            {
                if (pair.Key == null || pair.Value == null)
                {
                    Gone.Add(pair.Key!);
                }
            }
            foreach (Image icon in Gone)
            {
                Made.Remove(icon);
            }
            _sweepAt = Math.Max(64, Made.Count * 2);
        }

        private static Image Make(Image icon)
        {
            Transform parent = icon.transform.parent;
            GameObject go = new GameObject(Name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.layer = icon.gameObject.layer;
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            Transform bkg = parent.Find("bkg");
            rect.SetSiblingIndex(bkg != null ? bkg.GetSiblingIndex() + 1 : 0);
            Image image = go.GetComponent<Image>();
            image.sprite = BackdropArt.Sprite;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        /// <summary>The icon's anchors, pivot and scale, its corners moved in by the inset; written only when they differ.</summary>
        private static void Fit(RectTransform backdrop, RectTransform icon)
        {
            Vector2 inset = icon.rect.size * Inset;
            Vector2 min = icon.offsetMin + inset;
            Vector2 max = icon.offsetMax - inset;
            if (backdrop.anchorMin != icon.anchorMin || backdrop.anchorMax != icon.anchorMax || backdrop.pivot != icon.pivot)
            {
                backdrop.anchorMin = icon.anchorMin;
                backdrop.anchorMax = icon.anchorMax;
                backdrop.pivot = icon.pivot;
            }
            if (backdrop.offsetMin != min || backdrop.offsetMax != max)
            {
                backdrop.offsetMin = min;
                backdrop.offsetMax = max;
            }
            if (backdrop.localScale != icon.localScale)
            {
                backdrop.localScale = icon.localScale;
            }
        }
    }
}
