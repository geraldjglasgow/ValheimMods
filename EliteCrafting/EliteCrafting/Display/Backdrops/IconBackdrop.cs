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
    /// less 3 of every 64 units on each side (58 of the game's 64-unit icon), follows the icon's rect (fitted when made and
    /// whenever the icon's size changes), and shows only while the icon does. Display only, on the viewing client.
    /// Per-frame callers pay the custom-data test first; <see cref="Hide"/> is one int test while no backdrop shows.
    /// </summary>
    internal static class IconBackdrop
    {
        private const string Name = "ecf_backdrop";
        private const float Inset = (64f - BackdropArt.Span) / 2f / 64f;

        private static readonly Dictionary<Image, Made> Backdrops = new Dictionary<Image, Made>();
        private static readonly List<Image> Gone = new List<Image>();
        private static int _sweepAt = 64;
        private static int _shown;

        /// <summary>A made backdrop and the icon size it was last fitted to.</summary>
        private sealed class Made
        {
            public Image Backdrop = null!;
            public Vector2 FittedSize = new Vector2(-1f, -1f);
        }

        /// <summary>Shows the backdrop for the item while the icon shows, else hides it. Called per frame by most surfaces.</summary>
        public static void Set(Image? icon, ItemDrop.ItemData? item)
        {
            if (icon == null)
            {
                return;
            }
            if (BackdropTone.TryGet(item, out Color tone) && Visible(icon))
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
            if (_shown == 0 || icon == null || !Backdrops.TryGetValue(icon, out Made made))
            {
                return;
            }
            Image backdrop = made.Backdrop;
            if (backdrop != null && backdrop.enabled)
            {
                backdrop.enabled = false;
                _shown--;
            }
        }

        /// <summary>The icon's backdrop, made on first use, fitted to the icon and shown in the tone.</summary>
        public static Image Show(Image icon, Color tone)
        {
            Made made = Get(icon);
            Image backdrop = made.Backdrop;
            RectTransform iconRect = icon.rectTransform;
            Vector2 size = iconRect.rect.size;
            if (size != made.FittedSize)
            {
                made.FittedSize = size;
                Fit((RectTransform)backdrop.transform, iconRect, size);
            }
            if (backdrop.color != tone)
            {
                backdrop.color = tone;
            }
            if (!backdrop.enabled)
            {
                backdrop.enabled = true;
                _shown++;
            }
            return backdrop;
        }

        private static bool Visible(Image icon) =>
            icon.enabled && icon.gameObject.activeSelf && icon.sprite != null && icon.color.a > 0.01f;

        private static Made Get(Image icon)
        {
            if (Backdrops.TryGetValue(icon, out Made made) && made.Backdrop != null)
            {
                return made;
            }
            Sweep();
            made = new Made { Backdrop = Make(icon) };
            Backdrops[icon] = made;
            return made;
        }

        /// <summary>Forgets icons the game destroyed (recipe lists are rebuilt, drag ghosts come and go), now and then.</summary>
        private static void Sweep()
        {
            if (Backdrops.Count < _sweepAt)
            {
                return;
            }
            CollectGone();
            foreach (Image icon in Gone)
            {
                Backdrops.Remove(icon);
            }
            _sweepAt = Math.Max(64, Backdrops.Count * 2);
        }

        /// <summary>Lists the destroyed pairs in <see cref="Gone"/> and recounts the backdrops showing.</summary>
        private static void CollectGone()
        {
            Gone.Clear();
            _shown = 0;
            foreach (KeyValuePair<Image, Made> pair in Backdrops)
            {
                Image backdrop = pair.Value.Backdrop;
                if (pair.Key == null || backdrop == null)
                {
                    Gone.Add(pair.Key!);
                }
                else if (backdrop.enabled)
                {
                    _shown++;
                }
            }
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
        private static void Fit(RectTransform backdrop, RectTransform icon, Vector2 size)
        {
            Vector2 inset = size * Inset;
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
