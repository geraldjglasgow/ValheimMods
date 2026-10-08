using System;
using OpenKeep.Core;
using PatchGuard;
using PlateColumn;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Store
{
    /// <summary>
    /// The trash can on its own plate in the game's own style (the user's call, 2026-09-29: OpenKeep leaves the
    /// inventory's look alone; the brown boxes are PackPanel's): a copy of the game's armour plate with its wood, the bin
    /// in place of the shield and a small "Trash" under it, both inside the wood with a little padding (the game's icon
    /// pokes above its plate; the user wanted the can on the wood), <see cref="Below"/> units under the armour plate. That
    /// is where trash can mods put theirs, and how other mods recognise one: Jewelcrafting puts its synergy box on the same
    /// spot and shows the can only while a stack is dragged (it looks for the <c>armor_icon</c> child, which the copy
    /// keeps), CurrencyPocket and OttoPay move an object named <c>Trash</c> along with the armour plate, and TrashItems
    /// makes no can of its own when one is there. The game's armour and weight plates are found by name and only read,
    /// never moved or restyled. Clicking the can with a dragged stack trashes it. False, with nothing made, when either
    /// game plate is missing, a <c>Trash</c> already exists, or the weight plate sits too close under the armour for a
    /// plate between them; the button row then keeps the can.
    /// </summary>
    public static class TrashPlate
    {
        private const string Name = "Trash";
        private const string Icon = "armor_icon";
        private const string Wood = "bkg";

        /// <summary>Space between the wood's edges and the bin and the word (the user asked for small padding all round).</summary>
        private const float Pad = 4f;
        private const float WordHeight = 14f;
        private const float WordGap = 1f;
        private const float WordFont = 13f;

        /// <summary>Under the armour plate, where trash can mods have always put theirs.</summary>
        private const float Below = 78f;

        public static bool TryCreate(InventoryGui gui, Action onClick)
        {
            RectTransform armor = gui.m_player.Find("Armor") as RectTransform;
            RectTransform weight = gui.m_player.Find("Weight") as RectTransform;
            if (armor == null || weight == null || gui.m_player.Find(Name) != null || !Fits(gui.m_player, armor, weight))
            {
                Plugin.Log.LogInfo("trash can stays in the button row: a game plate is missing, a Trash exists, or there is no room under the armour");
                return false;
            }
            GameObject go = UnityEngine.Object.Instantiate(armor.gameObject, armor.parent);
            go.name = Name;
            go.transform.SetSiblingIndex(armor.GetSiblingIndex() + 1);
            ((RectTransform)go.transform).anchoredPosition = armor.anchoredPosition - new Vector2(0f, Below);
            Image bin = Dress(go);
            if (bin == null)
            {
                UnityEngine.Object.Destroy(go);
                return false;
            }
            MakeButton(go, bin, onClick);
            Tip(gui, go);
            return true;
        }

        /// <summary>Turns <paramref name="go"/> into the trash button: the bin lights up on hover and press.</summary>
        public static void MakeButton(GameObject go, Image bin, Action onClick)
        {
            Button button = go.GetComponent<Button>();
            if (button == null)
                button = go.AddComponent<Button>();
            button.targetGraphic = bin;
            ColorBlock colours = button.colors;
            colours.highlightedColor = new Color(1f, 0.55f, 0.45f, 1f);
            colours.selectedColor = colours.normalColor;   // a click selects the button; it must not stay red after
            colours.pressedColor = new Color(1f, 0.3f, 0.2f, 1f);
            button.colors = colours;
            button.onClick.AddListener(() => Guard.Run("trash can", onClick));
        }

        /// <summary>Room for a plate between the two: the weight plate's centre a plate's height below the new plate's.</summary>
        private static bool Fits(RectTransform panel, RectTransform armor, RectTransform weight)
        {
            float gap = CentreY(panel, armor) - CentreY(panel, weight);
            return gap >= Below + armor.rect.height;
        }

        private static float CentreY(RectTransform panel, RectTransform plate) =>
            panel.InverseTransformPoint(plate.TransformPoint(plate.rect.center)).y;

        /// <summary>
        /// The copy's shield becomes the bin and its number the word; every behaviour it copied from the armour plate goes,
        /// so nothing updates it or answers for the armour. Null without an icon.
        /// </summary>
        private static Image Dress(GameObject go)
        {
            foreach (Behaviour behaviour in go.GetComponents<Behaviour>())
                UnityEngine.Object.DestroyImmediate(behaviour);
            Image bin = go.transform.Find(Icon) is Transform icon ? icon.GetComponent<Image>() : null;
            if (bin == null)
                return null;
            bin.sprite = StoreSprites.Bin;
            bin.preserveAspect = true;
            RectTransform wood = go.transform.Find(Wood) as RectTransform ?? (RectTransform)go.transform;
            Inside(bin.rectTransform, wood, Pad + WordHeight + WordGap, Pad);
            TMP_Text word = go.GetComponentInChildren<TMP_Text>(true);
            if (word != null)
                Word(word, wood);
            return bin;
        }

        /// <summary>"Trash" on a small line along the bottom of the wood, shrinking to fit a longer word.</summary>
        private static void Word(TMP_Text word, RectTransform wood)
        {
            Inside(word.rectTransform, wood, Pad, wood.rect.height - Pad - WordHeight);
            word.text = Language.Localize(StoreWords.Trash);
            word.alignment = TextAlignmentOptions.Center;
            word.textWrappingMode = TextWrappingModes.NoWrap;
            word.enableAutoSizing = true;
            word.fontSizeMin = 8f;
            word.fontSizeMax = WordFont;
            word.margin = Vector4.zero;
        }

        /// <summary>
        /// Lays <paramref name="part"/> over the wood (a sibling, so the same anchors and offsets give the same rect), then
        /// insets it by <see cref="Pad"/> on the sides and by the given amounts at the bottom and the top.
        /// </summary>
        private static void Inside(RectTransform part, RectTransform wood, float bottom, float top)
        {
            if (part.parent != wood.parent)
                return;
            part.anchorMin = wood.anchorMin;
            part.anchorMax = wood.anchorMax;
            // Centred: an Image that keeps its sprite's aspect aligns it by the pivot, and the wood's pivot sits on its
            // left edge, which drew the bin against the plate's left side (2026-10-07). The offsets below set the same rect.
            part.pivot = new Vector2(0.5f, 0.5f);
            part.localScale = Vector3.one;
            part.offsetMin = wood.offsetMin + new Vector2(Pad, bottom);
            part.offsetMax = wood.offsetMax - new Vector2(Pad, top);
        }

        /// <summary>The game's own tooltip, following the pointer, in the look of the inventory's item tooltip.</summary>
        private static void Tip(InventoryGui gui, GameObject go)
        {
            UITooltip tip = go.AddComponent<UITooltip>();
            tip.m_topic = StoreWords.Trash;
            tip.m_text = StoreWords.DragHint;
            if (!Column.DressTooltip(gui, tip))
                UnityEngine.Object.Destroy(tip);
        }
    }
}
