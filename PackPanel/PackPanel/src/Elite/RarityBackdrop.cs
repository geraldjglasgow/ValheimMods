using System.Collections.Generic;
using EliteCraftingLink;
using HarmonyLib;
using PackPanel.Core;
using PackPanel.Look;
using UnityEngine;
using UnityEngine.UI;

namespace PackPanel.Elite
{
    /// <summary>
    /// EliteCrafting's rarity colour behind its magic items in the inventory grid and the container grid, the slot, key
    /// ring and tacklebox cells included (they are the player grid's own elements): a rounded fill
    /// (<see cref="RoundedFill"/>) in the rarity's colour (<c>GetRarityColor</c>) at <see cref="Alpha"/>, under the icon,
    /// as PackPanel's own child <c>PackPanel_rarity</c> of the cell. Epic Loot draws a background of its own behind its
    /// magic items in every grid; an item both mods call magic keeps Epic Loot's and gets none of ours (Epic Loot first).
    /// Only with EliteCrafting present and PackPanel's <c>Enabled</c> on (off, the grid is the game's, like the rest of
    /// the look). After the game's grid update, every frame a grid is shown: the cells holding a magic item are worked
    /// out from the items (most carry no custom data and are skipped at once), and a backdrop is shown, recoloured or
    /// hidden only when that changes. Display only, on the viewing client.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    public static class RarityBackdrop
    {
        private const string Name = "PackPanel_rarity";
        private const float Inset = 3f;
        private const float Alpha = 0.45f;

        private static readonly Dictionary<InventoryElement, Image> playerShown = new Dictionary<InventoryElement, Image>();
        private static readonly Dictionary<InventoryElement, Image> containerShown = new Dictionary<InventoryElement, Image>();
        private static readonly Dictionary<InventoryElement, Color> wanted = new Dictionary<InventoryElement, Color>();
        private static readonly List<InventoryElement> stale = new List<InventoryElement>();
        private static readonly Dictionary<string, Color> colours = new Dictionary<string, Color>();

        [HarmonyPostfix]
        public static void Postfix(InventoryGrid __instance)
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null || (__instance != gui.m_playerGrid && __instance != gui.m_containerGrid))
                return;
            wanted.Clear();
            if (InventorySettings.Enabled.Value && CraftingLink.Present)
                Collect(__instance);
            Apply(__instance == gui.m_playerGrid ? playerShown : containerShown);
        }

        private static void Collect(InventoryGrid grid)
        {
            Inventory inventory = grid.GetInventory();
            if (inventory == null)
                return;
            int width = inventory.GetWidth();
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                Vector2i pos = item.m_gridPos;
                InventoryElement element = pos.x >= 0 && pos.y >= 0 && pos.x < width ? grid.GetElement(pos.x, pos.y, width) : null;
                if (element != null && Magic(item, out Color colour))
                    wanted[element] = colour;
            }
        }

        /// <summary>An EliteCrafting magic item that Epic Loot does not also call magic, and its rarity's colour.</summary>
        private static bool Magic(ItemDrop.ItemData item, out Color colour)
        {
            colour = default;
            if (item.m_customData == null || item.m_customData.Count == 0 || !CraftingItems.IsMagic(item) || EpicLootLink.IsMagic(item))
                return false;
            colour = Colour(CraftingItems.GetRarityColor(item) ?? "#FFFFFF");
            return true;
        }

        private static Color Colour(string hex)
        {
            if (!colours.TryGetValue(hex, out Color colour))
            {
                colour = ColorUtility.TryParseHtmlString(hex, out Color parsed) ? parsed : Color.white;
                colour.a = Alpha;
                colours[hex] = colour;
            }
            return colour;
        }

        /// <summary>Hides what is no longer wanted (and forgets cells the game destroyed), then shows what is.</summary>
        private static void Apply(Dictionary<InventoryElement, Image> shown)
        {
            stale.Clear();
            foreach (KeyValuePair<InventoryElement, Image> pair in shown)
            {
                if (pair.Key == null || pair.Value == null || !wanted.ContainsKey(pair.Key))
                    stale.Add(pair.Key);
            }
            foreach (InventoryElement element in stale)
            {
                if (shown[element] != null)
                    shown[element].enabled = false;
                shown.Remove(element);
            }
            foreach (KeyValuePair<InventoryElement, Color> pair in wanted)
                Show(pair.Key, pair.Value, shown);
        }

        private static void Show(InventoryElement element, Color colour, Dictionary<InventoryElement, Image> shown)
        {
            if (!shown.TryGetValue(element, out Image image) || image == null)
            {
                image = Backdrop(element);
                shown[element] = image;
            }
            if (image.color != colour)
                image.color = colour;
            if (!image.enabled)
                image.enabled = true;
        }

        /// <summary>The cell's backdrop, made once: first among its children, so the cell's own art lies under it and the icon, counts and marks over it.</summary>
        private static Image Backdrop(InventoryElement element)
        {
            Transform kept = element.transform.Find(Name);
            if (kept != null)
                return kept.GetComponent<Image>();
            GameObject go = new GameObject(Name, typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(element.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(Inset, Inset);
            rect.offsetMax = new Vector2(-Inset, -Inset);
            rect.SetAsFirstSibling();
            Image image = go.GetComponent<Image>();
            image.sprite = RoundedFill.Sprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            return image;
        }
    }
}
