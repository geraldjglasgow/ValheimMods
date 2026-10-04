using System.Collections.Generic;
using PlateColumn;
using PackPanel.Core;
using UnityEngine;

namespace PackPanel.Look
{
    /// <summary>
    /// Embedded inventory artwork. Brown keeps its original procedural assets; Timber uses the supplied screenshot
    /// as its art reference. Frames are nine-sliced and labels remain live text; slot icons use material-colored art.
    /// Each texture is decoded once with mipmaps; missing Timber images fall back to the original artwork.
    /// </summary>
    public static class SkinArt
    {
        private const float PanelPixelsPerUnit = 200f;
        private const float PanelBorder = 32f;
        private const float ButtonBorder = 12f;

        private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        public static bool Timber => InventorySettings.PanelTheme != null && GridSkin.On && InventorySettings.PanelTheme.Value == InventoryTheme.Timber;

        public static Sprite Panel => Timber ? Wallpaper : BrownPanel;

        public static Sprite BrownPanel => Read("panel", PanelPixelsPerUnit, PanelBorder);

        public static Sprite Wallpaper => Read("timber_background", 100f, 0f) ?? BrownPanel;

        public static Sprite Cell => Read("cell", 100f, 0f);

        public static Sprite Button => Timber ? Read("timber_button", 100f, -5f) ?? BrownButton : BrownButton;

        private static Sprite BrownButton => Read("button", 100f, ButtonBorder);

        public static Sprite ButtonHover => Read("button_hover", 100f, ButtonBorder);

        public static Sprite ButtonPressed => Read("button_pressed", 100f, ButtonBorder);

        /// <summary>The key ring pop-up's round panel and the bronze wire its cells hang on (stretched, no slicing).</summary>
        public static Sprite RingPanel => Read("ring_panel", 100f, 0f);

        public static Sprite RingLine => Read("ring_line", 100f, 0f);

        /// <summary>A slot's item illustration (<c>icon_head.png</c> ...), or null when there is none for that name.</summary>
        public static Sprite Icon(string slot) => Read("icon_" + slot, 100f, 0f);

        public static bool IsPanel(Sprite sprite) => sprite != null &&
            (sprite.name == "PackPanel_panel" || sprite.name == "PackPanel_timber_panel" || TimberFrame.Owns(sprite));

        /// <summary>
        /// Every image the panels use, decoded now (at the main menu) rather than the first time the inventory opens:
        /// the Timber wallpaper (1254 px square) and button (1983 x 793) decode with their mipmaps on the main thread,
        /// which stalled the panels' first slide-in (reported 2026-10-04). Decoded images are kept, so later reads are free.
        /// </summary>
        public static void Prewarm()
        {
            Sprite[] all = { Wallpaper, BrownPanel, Cell, Button, BrownButton, ButtonHover, ButtonPressed, RingPanel, RingLine };
            int count = all.Length;
            foreach (Slots.SlotKind kind in System.Enum.GetValues(typeof(Slots.SlotKind)))
            {
                if (kind != Slots.SlotKind.Retired && Icon(kind.ToString().ToLowerInvariant()) != null)
                    count++;
            }
            Plugin.Log.LogInfo($"PackPanel: {count} panel images ready");
        }

        private static Sprite Read(string name, float pixelsPerUnit, float border)
        {
            if (sprites.TryGetValue(name, out Sprite sprite))
                return sprite;
            sprites[name] = null;
            string resource = $"PackPanel.assets.{name}.png";
            if (typeof(SkinArt).Assembly.GetManifestResourceInfo(resource) == null)
                return null;
            Sprite decoded = EmbeddedSprite.Load(typeof(SkinArt).Assembly, resource, "PackPanel_" + name);
            if (decoded == null)
            {
                Plugin.Log.LogWarning($"could not read the embedded image {resource}; the plain brown is used instead");
                return null;
            }
            sprite = Resliced(decoded.texture, name, pixelsPerUnit, border);
            Object.Destroy(decoded);
            sprites[name] = sprite;
            return sprite;
        }

        /// <summary>The decoded texture as a sprite with the given scale and 9-slice border.</summary>
        private static Sprite Resliced(Texture2D texture, string name, float pixelsPerUnit, float border)
        {
            texture.wrapMode = TextureWrapMode.Clamp;
            Rect whole = new Rect(0f, 0f, texture.width, texture.height);
            // Generated sources may have different resolutions. Keep 13% corners intact and
            // express their final thickness in UI units, independent of the source pixel count.
            if (border < 0f)
            {
                float units = -border;
                border = Mathf.Ceil(Mathf.Min(texture.width, texture.height) * 0.13f);
                pixelsPerUnit = border * 100f / units;
            }
            if (name == "timber_button")
            {
                // Exclude the transparent source margins without changing the master artwork.
                whole = new Rect(texture.width * (57f / 1983f), texture.height * (137f / 793f),
                    texture.width * (1870f / 1983f), texture.height * (551f / 793f));
                border = whole.height * 0.25f;
                pixelsPerUnit = border * 100f / 5f;
            }
            Sprite sprite = Sprite.Create(texture, whole, new Vector2(0.5f, 0.5f), pixelsPerUnit, 0u, SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            sprite.name = "PackPanel_" + name;
            return sprite;
        }
    }
}
