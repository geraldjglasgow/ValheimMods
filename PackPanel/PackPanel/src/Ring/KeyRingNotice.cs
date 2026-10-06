using System.Collections.Generic;
using PackPanel.Core;
using PackPanel.Panels;
using PackPanel.Slots;
using PlateColumn;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackPanel.Ring
{
    /// <summary>
    /// The announcement of a key the character never had before (<see cref="KeyRingNews"/>). While one is waiting, the ring
    /// button's key icon breathes slowly between its own colours and gold (<see cref="Period"/> seconds a breath) and a small box
    /// under the button says so ("Swamp Key went onto your key ring", or "2 new keys went onto your key ring"). It waits
    /// until the ring is opened; it is never saved. The button shows only with the inventory open, so a key picked up on
    /// the road is announced the next time the inventory opens. The box takes no clicks. The box, its text and the hint's
    /// icon are found once per placed button (<see cref="Forget"/>), and the text is made only when a key was added.
    /// </summary>
    public static class KeyRingNotice
    {
        private const string Name = "PackPanel_keynotice";
        private const string HintIcon = "PackPanel_hint/icon";
        private const float Period = 1.6f;
        private const float Height = 26f;
        private static readonly Color Glow = new Color(1f, 0.86f, 0.4f, 1f);
        private static readonly List<string> waiting = new List<string>();
        private static bool unwritten = true;
        private static InventoryElement foundOn;
        private static Transform box;
        private static Image hint;

        public static void Add(ItemDrop.ItemData key)
        {
            waiting.Add(key.m_shared.m_name);
            unwritten = true;
        }

        public static void Clear() => waiting.Clear();

        /// <summary>The button was placed again (its hint may be new): look its parts up again on the next frame.</summary>
        public static void Forget() => foundOn = null;

        /// <summary>Every frame the button is refreshed (after it set its icon's colour): the box and the breathing icon, or neither.</summary>
        public static void Show(InventoryElement button)
        {
            if (KeyRingState.Open)
                waiting.Clear();
            Find(button);
            if (waiting.Count == 0)
            {
                if (box != null && box.gameObject.activeSelf)
                    Rest();
                return;
            }
            if (box == null)
            {
                box = Make(button);
                unwritten = true;
            }
            if (!box.gameObject.activeSelf)
                box.gameObject.SetActive(true);
            Write();
            Breathe(button, 0.5f - 0.5f * Mathf.Cos(Time.unscaledTime * 2f * Mathf.PI / Period));
        }

        private static void Find(InventoryElement button)
        {
            if (foundOn == button)
                return;
            foundOn = button;
            box = button.transform.Find(Name);
            Transform icon = button.transform.Find(HintIcon);
            hint = icon != null ? icon.GetComponent<Image>() : null;
        }

        /// <summary>Both icons the button may show (its own with Slot Labels off, the hint's with them on), from their hint colour towards gold.</summary>
        private static void Breathe(InventoryElement button, float glow)
        {
            button.m_icon.color = Color.Lerp(SlotIcons.Hint, Glow, glow);
            if (hint != null)
                hint.color = Color.Lerp(SlotIcons.TintFor(SlotKind.Key), Glow, glow);
        }

        private static void Rest()
        {
            box.gameObject.SetActive(false);
            if (hint != null)
                hint.color = SlotIcons.TintFor(SlotKind.Key);
        }

        /// <summary>The text for what is waiting, made, set and the box sized to it only after a key was added.</summary>
        private static void Write()
        {
            if (!unwritten)
                return;
            unwritten = false;
            string text = waiting.Count == 1
                ? string.Format(Language.Localize(Words.KeyNew), Language.Localize(waiting[0]))
                : string.Format(Language.Localize(Words.KeysNew), waiting.Count);
            TMP_Text label = box.GetComponentInChildren<TMP_Text>(true);
            label.text = text;
            ((RectTransform)box).sizeDelta = new Vector2(label.GetPreferredValues(text).x + 16f, Height);
        }

        /// <summary>A small box in the column's style hanging under the button, centred, with one line of text.</summary>
        private static Transform Make(InventoryElement button)
        {
            GameObject go = new GameObject(Name, typeof(RectTransform), typeof(Image));
            go.layer = button.gameObject.layer;
            go.transform.SetParent(button.transform, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -6f);
            Image image = go.GetComponent<Image>();
            image.sprite = Skin.Box;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            Label(button, rect);
            return go.transform;
        }

        private static void Label(InventoryElement button, RectTransform box)
        {
            GameObject go = new GameObject("text", typeof(RectTransform));
            go.SetActive(false);   // the text wakes with its font set, so it never looks for TextMeshPro's missing default
            go.layer = box.gameObject.layer;
            go.transform.SetParent(box, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            TMP_Text text = go.AddComponent<TextMeshProUGUI>();
            text.font = button.m_amount.font;
            go.SetActive(true);
            text.fontSize = 14f;
            text.color = Skin.LabelColour;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
        }
    }
}
