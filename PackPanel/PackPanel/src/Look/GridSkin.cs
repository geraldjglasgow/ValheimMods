using System.Collections.Generic;
using PlateColumn;
using PackPanel.Core;
using PackPanel.Panels;
using UnityEngine.UI;
using UnityEngine;

namespace PackPanel.Look
{
    /// <summary>
    /// Local inventory styling. Brown uses the original custom panel; Timber combines screen-aligned wallpaper with
    /// PackPanel's own sliced bevel and metal fittings. Both use the default UI material.
    /// Each grid keeps its recessed cells; PlateColumn's plain Skin stands in for missing custom artwork.
    /// The game's panel backgrounds reach 10
    /// units past their panel; the brown ones reach 12, a little room between the frame and the cells, and the container
    /// panel sits 4 units lower so the two frames keep the game's gap. Off, the game's sprites, materials and sizes come
    /// back: each panel's own are remembered the first time it is changed, the cell sprite once from the game's element.
    /// </summary>
    public static class GridSkin
    {
        private const float GameOverhang = 10f;
        private const float WoodOverhang = 12f;

        private static readonly Dictionary<Image, Sprite> panelSprites = new Dictionary<Image, Sprite>();
        private static readonly Dictionary<Image, Material> panelMaterials = new Dictionary<Image, Material>();
        private static Sprite gameCell;

        /// <summary>The brown look is part of the new inventory: the master switch off brings the game's back too.</summary>
        public static bool On => InventorySettings.Enabled.Value && InventorySettings.BrownStyle.Value;

        /// <summary>Extra room above the first row with the frame on, so the cells clear it.</summary>
        public static float TopPad => On ? 4f : 0f;

        /// <summary>How far the background reaches past a panel's rect: the game's 10, or 12 with the brown frame.</summary>
        public static float PanelOverhang => On && SkinArt.Panel != null ? WoodOverhang : GameOverhang;

        public static void Panels(InventoryGui gui)
        {
            Background(Background(gui.m_player));
            Background(Background(gui.m_container));
            CompanionPanel(Background(gui.m_info));
            CompanionPanel(Background(gui.m_crafting));
            ContainerDrop.Apply(gui.m_container, PanelOverhang - GameOverhang);
        }

        /// <summary>The character header and crafting panel share the theme, retaining their size and controls.</summary>
        private static void CompanionPanel(Image image)
        {
            if (image != null)
                TimberBackground.Apply(image, SkinArt.Timber && SkinArt.Panel != null);
        }

        /// <summary>A stretched panel background (the game's Bkg): the skin, and how far it reaches past the panel.</summary>
        public static void Background(Image image)
        {
            Panel(image);
            if (image != null)
                image.rectTransform.sizeDelta = Vector2.one * (PanelOverhang * 2f);
        }

        public static void Panel(Image image)
        {
            if (image == null)
                return;
            TimberBackground.Apply(image, false);
            if (!panelSprites.ContainsKey(image) && image.sprite != Skin.Panel && !SkinArt.IsPanel(image.sprite))
            {
                panelSprites[image] = image.sprite;
                panelMaterials[image] = image.material;
            }
            Sprite wood = SkinArt.Panel;
            bool timber = SkinArt.Timber;
            image.sprite = !On || timber ? GameSprite(image) : wood != null ? wood : Skin.Panel;
            image.material = !On || timber ? GameMaterial(image) : null;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            TimberBackground.Apply(image, SkinArt.Timber && wood != null);
            NightShade.Track(image);
        }

        public static void Cell(InventoryElement element)
        {
            Image image = element != null ? element.GetComponent<Image>() : null;
            if (image == null)
                return;
            if (gameCell == null && image.sprite != Skin.Cell && image.sprite != SkinArt.Cell)
                gameCell = image.sprite;
            Sprite recessed = SkinArt.Cell;
            image.sprite = !On ? gameCell : recessed != null ? recessed : Skin.Cell;
            image.type = On && recessed != null ? Image.Type.Simple : Image.Type.Sliced;
        }

        /// <summary>The game's own sprite and material of a panel background, whatever it shows now.</summary>
        public static Sprite GameSprite(Image image) => image != null && panelSprites.TryGetValue(image, out Sprite kept) ? kept : image?.sprite;

        public static Material GameMaterial(Image image) => image != null && panelMaterials.TryGetValue(image, out Material kept) ? kept : image?.material;

        /// <summary>A panel's wood: its direct child named Bkg, as the game's scene names it.</summary>
        public static Image Background(RectTransform panel)
        {
            Transform child = panel != null ? panel.Find("Bkg") : null;
            return child != null ? child.GetComponent<Image>() : null;
        }
    }
}
