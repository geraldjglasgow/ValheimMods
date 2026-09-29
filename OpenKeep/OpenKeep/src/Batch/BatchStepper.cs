using OpenKeep.Core;
using PatchGuard;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OpenKeep.Batch
{
    /// <summary>
    /// The stepper: - amount + in the Craft button's row, left of the Craft button, which gives up that width while the
    /// stepper shows. The buttons are clones of the game's own quality - and + buttons (built into the panel, never
    /// shown by the game); the amount between them is a field to type in (<see cref="BatchField"/>). No tooltips. On a
    /// gamepad the D-pad's left and right press - and + while the crafting panel is selected; the game repeats them
    /// while held. Shown on the Craft tab only, hidden while the craft bar fills.
    /// </summary>
    public static class BatchStepper
    {
        private const float ButtonWidth = 44f;
        private const float AmountWidth = 62f;
        private const float Gap = 4f;
        private const float Width = ButtonWidth * 2f + AmountWidth + Gap * 2f;
        private const float CraftGap = 6f;

        private static GameObject root;
        private static Button less;
        private static Button more;
        private static TMP_Text craftLabel;
        private static RectTransform craft;
        private static Vector2 craftOffsetMin;
        private static bool shown;

        /// <summary>Called from InventoryGui.Awake. Builds nothing when the game's buttons are missing, and the panel stays the game's.</summary>
        public static void Create(InventoryGui gui)
        {
            root = null;
            shown = false;
            if (gui.m_craftButton == null || gui.m_qualityLevelDown == null || gui.m_qualityLevelUp == null)
                return;
            craftLabel = gui.m_craftButton.GetComponentInChildren<TMP_Text>(true);
            if (craftLabel == null)
                return;
            craft = (RectTransform)gui.m_craftButton.transform;
            craftOffsetMin = craft.offsetMin;
            root = MakeRoot();
            less = StepButton(gui.m_qualityLevelDown, -1, 0f);
            BatchField.Create(gui, craftLabel, Slot("OpenKeep_BatchAmount", ButtonWidth + Gap, AmountWidth));
            more = StepButton(gui.m_qualityLevelUp, 1, Width - ButtonWidth);
            root.SetActive(false);
        }

        /// <summary>After every UpdateRecipe: shown on the Craft tab while no craft runs; the amount, the buttons, the Craft label.</summary>
        public static void Refresh(InventoryGui gui, Player player)
        {
            if (root == null)
                return;
            bool show = BatchAmount.Applies(gui, player) && gui.m_craftTimer < 0f;
            Show(show);
            if (!show)
                return;
            bool selected = gui.m_selectedRecipe.Recipe != null;
            BatchField.Refresh(selected);
            less.interactable = selected && BatchAmount.Value > 1;
            more.interactable = selected && BatchAmount.CanStepUp(player);
            // The game adds " x 5" to Craft while Shift is held; the stepper shows the amount, so Craft stays Craft.
            if (selected)
                craftLabel.text = Language.Localize("$inventory_craftbutton");
        }

        private static void Show(bool show)
        {
            if (show == shown)
                return;
            shown = show;
            root.SetActive(show);
            craft.offsetMin = new Vector2(craftOffsetMin.x + (show ? Width + CraftGap : 0f), craftOffsetMin.y);
        }

        /// <summary>The row's left end, as high as the Craft button.</summary>
        private static GameObject MakeRoot()
        {
            GameObject go = new GameObject("OpenKeep_BatchStepper", typeof(RectTransform));
            go.layer = craft.gameObject.layer;
            go.transform.SetParent(craft.parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, craft.anchorMin.y);
            rect.anchorMax = new Vector2(0f, craft.anchorMax.y);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = craftOffsetMin;
            rect.offsetMax = new Vector2(craftOffsetMin.x + Width, craft.offsetMax.y);
            return go;
        }

        /// <summary>An empty, inactive child of the row, placed; the caller adds its components and activates it.</summary>
        private static GameObject Slot(string name, float x, float width)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.SetActive(false);
            go.layer = root.layer;
            go.transform.SetParent(root.transform, false);
            Place((RectTransform)go.transform, x, width);
            return go;
        }

        /// <summary>Full height, from x, that wide, inside the row.</summary>
        private static void Place(RectTransform rect, float x, float width)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = new Vector2(x, 0f);
            rect.offsetMax = new Vector2(x + width, 0f);
            rect.localScale = Vector3.one;
        }

        private static Button StepButton(Button template, int direction, float x)
        {
            bool up = direction > 0;
            GameObject go = Object.Instantiate(template.gameObject, root.transform);
            go.name = up ? "OpenKeep_BatchMore" : "OpenKeep_BatchLess";
            go.SetActive(true);
            Place((RectTransform)go.transform, x, ButtonWidth);
            Button button = go.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => Guard.Run("batch step", () => BatchAmount.Step(direction)));
            Gamepad(go, up ? "JoyDPadRight" : "JoyDPadLeft");
            Label(go.transform, up ? "+" : "-");
            return button;
        }

        /// <summary>The clone's gamepad key becomes the D-pad; its hint (the game's stick glyph) goes.</summary>
        private static void Gamepad(GameObject go, string key)
        {
            UIGamePad pad = go.GetComponent<UIGamePad>();
            if (pad == null)
                return;
            pad.m_zinputKey = key;
            pad.m_keyCode = KeyCode.None;
            if (pad.m_hint != null && pad.m_hint.transform.IsChildOf(go.transform))
                Object.DestroyImmediate(pad.m_hint);
            pad.m_hint = null;
        }

        /// <summary>The button's own text (a direct child), in the Craft button's font.</summary>
        private static void Label(Transform button, string text)
        {
            foreach (Transform child in button)
            {
                TMP_Text label = child.GetComponent<TMP_Text>();
                if (label == null)
                    continue;
                label.font = craftLabel.font;
                label.enableAutoSizing = false;
                label.fontSize = 32f;
                label.text = text;
            }
        }
    }
}
