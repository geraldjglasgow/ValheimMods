using System.Collections.Generic;
using EliteCrafting.Affixes;
using UnityEngine;
using UnityEngine.UI;

namespace EliteCrafting.Display.Backdrops
{
    /// <summary>
    /// The seal mark on a sealed item's icon (<see cref="SealArt"/>): an Image made once per icon as the sibling right
    /// after it, so it draws over the icon and under the cell's numbers and markers, in the icon's lower left corner
    /// (gear shows no amount there; the durability bar runs below it). It follows the icon's rect and shows only while the
    /// icon does and the item is sealed. Driven by <see cref="IconBackdrop"/>, so it appears on every surface the rarity
    /// backdrop does. Display only, on the viewing client; <see cref="Hide"/> is one int test while no seal shows.
    /// </summary>
    internal static class IconSeal
    {
        private const string Name = "ecf_seal";

        // The mark's place in the icon's rect, as fractions of its width and height from the lower left corner.
        private static readonly Vector2 From = new Vector2(0.03f, 0.08f);
        private static readonly Vector2 To = new Vector2(0.55f, 0.34f);

        private static readonly Dictionary<Image, Image> Seals = new Dictionary<Image, Image>();
        private static readonly List<Image> Gone = new List<Image>();
        private static int _sweepAt = 64;
        private static int _shown;

        /// <summary>Whether the item carries the seal (a Serpent Rune's, or any other reason the item data names).</summary>
        public static bool IsSealed(ItemDrop.ItemData? item) =>
            item != null && item.m_customData != null && item.m_customData.Count > 0 && ItemState.Read(item).IsSealed;

        public static void Show(Image icon)
        {
            Image seal = Get(icon);
            Fit((RectTransform)seal.transform, icon.rectTransform);
            if (!seal.enabled)
            {
                seal.enabled = true;
                _shown++;
            }
        }

        public static void Hide(Image? icon)
        {
            if (_shown == 0 || icon == null || !Seals.TryGetValue(icon, out Image seal) || seal == null || !seal.enabled)
            {
                return;
            }
            seal.enabled = false;
            _shown--;
        }

        private static Image Get(Image icon)
        {
            if (Seals.TryGetValue(icon, out Image seal) && seal != null)
            {
                return seal;
            }
            Forget();
            seal = Make(icon);
            Seals[icon] = seal;
            return seal;
        }

        // Icons the game destroyed (recipe lists rebuilt, drag ghosts) are dropped once the table has doubled.
        private static void Forget()
        {
            if (Seals.Count < _sweepAt)
            {
                return;
            }
            Gone.Clear();
            _shown = 0;
            foreach (KeyValuePair<Image, Image> pair in Seals)
            {
                if (pair.Key == null || pair.Value == null)
                {
                    Gone.Add(pair.Key!);
                }
                else if (pair.Value.enabled)
                {
                    _shown++;
                }
            }
            Gone.ForEach(icon => Seals.Remove(icon));
            _sweepAt = System.Math.Max(64, Seals.Count * 2);
        }

        private static Image Make(Image icon)
        {
            GameObject go = new GameObject(Name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.layer = icon.gameObject.layer;
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(icon.transform.parent, false);
            rect.SetSiblingIndex(icon.transform.GetSiblingIndex() + 1);
            Image image = go.GetComponent<Image>();
            image.sprite = SealArt.Sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        /// <summary>The icon's anchors, pivot and scale, its corners moved to the mark's part; written only when they differ.</summary>
        private static void Fit(RectTransform seal, RectTransform icon)
        {
            Vector2 size = icon.rect.size;
            Vector2 min = icon.offsetMin + Vector2.Scale(size, From);
            Vector2 max = icon.offsetMax - Vector2.Scale(size, Vector2.one - To);
            if (seal.anchorMin != icon.anchorMin || seal.anchorMax != icon.anchorMax || seal.pivot != icon.pivot)
            {
                seal.anchorMin = icon.anchorMin;
                seal.anchorMax = icon.anchorMax;
                seal.pivot = icon.pivot;
            }
            if (seal.offsetMin != min || seal.offsetMax != max)
            {
                seal.offsetMin = min;
                seal.offsetMax = max;
            }
            if (seal.localScale != icon.localScale)
            {
                seal.localScale = icon.localScale;
            }
        }
    }
}
