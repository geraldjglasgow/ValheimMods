using PatchGuard;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OpenKeep.Batch
{
    /// <summary>
    /// The amount between - and +: a text field on the requirement slots' dark background, in the Craft button's font.
    /// A click selects the whole number and digits replace it (four at most, nothing else is accepted); Enter or a click
    /// elsewhere sets the amount, kept within 1 and the most that can be made; Escape keeps the old one. Then the field
    /// lets go of the selection, because OpenKeep's hotkeys stay quiet while an input field is selected. While the
    /// inventory is open the game reads no hotbar keys and opens no chat, so digits and Enter do nothing else.
    /// </summary>
    public static class BatchField
    {
        private const float FontSize = 26f;

        private static TMP_InputField field;

        /// <summary>Builds the field on an inactive, placed object and activates it once the input field is wired.</summary>
        public static void Create(InventoryGui gui, TMP_Text craftLabel, GameObject go)
        {
            Image background = go.AddComponent<Image>();
            Background(gui, background);
            RectTransform area = TextArea(go.transform);
            field = go.AddComponent<TMP_InputField>();
            field.textViewport = area;
            field.textComponent = Text(craftLabel, area);
            field.targetGraphic = background;
            Configure(field);
            go.SetActive(true);
        }

        /// <summary>Every frame the stepper shows: the amount, unless it is being typed; typing only with a recipe selected.</summary>
        public static void Refresh(bool selected)
        {
            if (field == null)
                return;
            field.interactable = selected;
            if (field.isFocused)
                return;
            string amount = BatchAmount.Value.ToString();
            if (field.text != amount)
                field.SetTextWithoutNotify(amount);
        }

        private static void Configure(TMP_InputField input)
        {
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterValidation = TMP_InputField.CharacterValidation.Digit;
            input.characterLimit = 4;
            input.onFocusSelectAll = true;
            input.customCaretColor = true;
            input.caretColor = Color.white;
            input.caretWidth = 2;
            input.selectionColor = new Color(1f, 0.75f, 0.35f, 0.45f);
            input.transition = Selectable.Transition.None;
            input.navigation = new Navigation { mode = Navigation.Mode.None };
            input.text = "1";
            input.onEndEdit.AddListener(text => Guard.Run("batch amount", () => Typed(input, text)));
        }

        /// <summary>The end of an edit: the typed amount (an empty field keeps the old one), shown as it was kept.</summary>
        private static void Typed(TMP_InputField input, string text)
        {
            if (int.TryParse(text, out int typed))
                BatchAmount.Set(typed);
            input.SetTextWithoutNotify(BatchAmount.Value.ToString());
            Release(input.gameObject);
        }

        /// <summary>
        /// Enter leaves the field selected; nothing is taken from a selection already moving to what was clicked. The
        /// field has stopped taking input before its end-edit event, so the deselect does not end the edit twice.
        /// </summary>
        private static void Release(GameObject go)
        {
            EventSystem system = EventSystem.current;
            if (system != null && !system.alreadySelecting && system.currentSelectedGameObject == go)
                system.SetSelectedGameObject(null);
        }

        /// <summary>The clipping area the text scrolls in, inset from the field's edge.</summary>
        private static RectTransform TextArea(Transform parent)
        {
            GameObject go = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(4f, 4f);
            rect.offsetMax = new Vector2(-4f, -4f);
            return rect;
        }

        /// <summary>A copy of the Craft button's label, white, centred, one fixed size (the caret sits wrong on auto-sized text).</summary>
        private static TMP_Text Text(TMP_Text craftLabel, RectTransform area)
        {
            TMP_Text text = Object.Instantiate(craftLabel.gameObject, area).GetComponent<TMP_Text>();
            text.gameObject.name = "Text";
            RectTransform rect = (RectTransform)text.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            text.enableAutoSizing = false;
            text.fontSize = FontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            return text;
        }

        /// <summary>The requirement slots' dark background, so the amount reads as a field.</summary>
        private static void Background(InventoryGui gui, Image image)
        {
            GameObject[] slots = gui.m_recipeRequirementList;
            Image slot = slots != null && slots.Length > 0 && slots[0] != null ? slots[0].GetComponent<Image>() : null;
            if (slot == null)
            {
                image.color = new Color(0f, 0f, 0f, 0.55f);
                return;
            }
            image.sprite = slot.sprite;
            image.type = slot.type;
            image.color = slot.color;
        }
    }
}
