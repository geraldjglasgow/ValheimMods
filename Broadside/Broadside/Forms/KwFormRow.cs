using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Broadside
{
    /// <summary>
    /// One row of the <see cref="KwForm"/> window: the field's label, then for a number the - button, its box, the + button
    /// and the unit, for a choice the &lt; button, the option and the &gt; button, for a button row its buttons
    /// (<see cref="KwFormButtons"/>, across the whole row when it has no label). A number's box takes digits, one decimal
    /// separator (a point or a comma) and, when the minimum is below zero, a leading minus. As soon as the typed text reads
    /// as a number, the value it would be kept as (within the range, on the step) is the field's value and the form hears
    /// of it; when the typing ends (Enter, Tab, a click elsewhere, OK) the box shows that value, or the last good one when
    /// the text does not read. A step (the - / + buttons, the mouse wheel over the box) changes the value at once. A choice
    /// cycles round its options. The form hears of a change only when a value really changed.
    /// </summary>
    internal sealed class KwFormRow
    {
        public const float Height = 34f;

        private const float LabelLeft = 28f;
        private const float LabelWidth = 160f;
        private const float Small = 34f;
        private const float Box = 130f;
        private const float UnitWidth = 46f;
        private const float LeftButton = LabelLeft + LabelWidth + 6f;
        private const float BoxLeft = LeftButton + Small + 4f;
        private const float RightButton = BoxLeft + Box + 4f;
        private const float UnitLeft = RightButton + Small + 6f;

        /// <summary>The window's width the columns fill, with the label's margin on the right as well.</summary>
        public const float Width = UnitLeft + UnitWidth + LabelLeft;

        /// <summary>The margin left and right of the rows, and where a row's controls start after its label.</summary>
        public const float Margin = LabelLeft, Controls = LeftButton;

        private static readonly char[] Separators = { '.', ',' };
        private static readonly TMP_InputField.OnValidateInput Unsigned = (text, at, added) => Accept(text, at, added, false);
        private static readonly TMP_InputField.OnValidateInput Signed = (text, at, added) => Accept(text, at, added, true);

        private readonly KwField field;
        private readonly KwFormValues values;
        private TMP_InputField? input;
        private TMP_Text? option;
        private float number;
        private int index;
        private bool wasFocused;

        private KwFormRow(KwField field, KwFormValues values)
        {
            this.field = field;
            this.values = values;
        }

        /// <summary>Off once the form has closed: the row's events (an edit ending as its box hides) change nothing then.</summary>
        public bool Live { get; set; } = true;

        public bool IsNumber => input != null;
        public bool Focused => input != null && input.isFocused;

        public static KwFormRow Make(KwFormKit kit, KwField field, KwFormValues values, float y)
        {
            KwFormRow row = new KwFormRow(field, values);
            if (field.Label.Length > 0)
                Words(kit, "Label", field.Label, LabelLeft, y, LabelWidth);
            if (field.IsButtons)
                KwFormButtons.Make(kit, field, y, field.Label.Length > 0 ? Controls : Margin);
            else if (field.IsChoice)
                row.BuildChoice(kit, y);
            else
                row.BuildNumber(kit, y);
            return row;
        }

        /// <summary>The number's box takes the keys, its text selected; nothing for a choice.</summary>
        public void Focus()
        {
            if (input != null)
                input.ActivateInputField();
        }

        /// <summary>Every frame the form shows: the box keeps the row's character rule, and a box that lost the keys keeps what was typed.</summary>
        public void Tick()
        {
            if (input == null)
                return;
            // The game's input field adds a validator of its own when it starts, which would let letters past this one.
            TMP_InputField.OnValidateInput rule = field.Min < 0f ? Signed : Unsigned;
            if (input.onValidateInput != rule)
                input.onValidateInput = rule;
            bool focused = input.isFocused;
            if (wasFocused && !focused)
                Commit();
            wasFocused = focused;
        }

        /// <summary>The typing ends: the text is read (the last good value when it does not read) and shown as kept.</summary>
        public void Commit()
        {
            if (input == null)
                return;
            if (KwField.TryRead(input.text, out float typed))
                Set(field.Snap(typed));
            string shown = field.Show(number);
            if (input.text != shown)
                input.SetTextWithoutNotify(shown);
        }

        public void Wheel(int direction) => Run(() => Step(direction));

        private void BuildNumber(KwFormKit kit, float y)
        {
            number = values.Number(field.Key);
            SmallButton(kit, "Less", "-", LeftButton, y, () => Step(-1));
            input = KwFormBoxes.Number(kit.Field, kit.Window, "Number");
            KwFormCopies.Place((RectTransform)input.transform, BoxLeft, y, Box, Height);
            input.SetTextWithoutNotify(field.Show(number));
            input.onValidateInput = field.Min < 0f ? Signed : Unsigned;
            input.onValueChanged.AddListener(text => Run(() => Typed(text)));
            input.onEndEdit.AddListener(_ => Run(Commit));
            input.onDeselect.AddListener(_ => Run(Commit));
            input.onSelect.AddListener(_ => Run(() => KwFormView.Selected(this)));
            input.gameObject.AddComponent<KwFormWheel>().Row = this;
            SmallButton(kit, "More", "+", RightButton, y, () => Step(1));
            Words(kit, "Unit", field.Unit, UnitLeft, y, UnitWidth);
        }

        private void BuildChoice(KwFormKit kit, float y)
        {
            index = values.ChoiceIndex(field.Key);
            SmallButton(kit, "Previous", "<", LeftButton, y, () => Cycle(-1));
            RectTransform box = KwFormBoxes.Choice(kit.Field, kit.Title, kit.Window, "Option", () => Cycle(1));
            KwFormCopies.Place(box, BoxLeft, y, Box, Height);
            option = box.GetComponentInChildren<TMP_Text>(true);
            SmallButton(kit, "Next", ">", RightButton, y, () => Cycle(1));
            ShowOption();
        }

        private void Typed(string text)
        {
            if (KwField.TryRead(text, out float typed))
                Set(field.Snap(typed));
        }

        // One step up or down; with Shift held, a whole 1.
        private void Step(int direction)
        {
            if (input == null)
                return;
            bool shift = ZInput.GetKey(KeyCode.LeftShift, false) || ZInput.GetKey(KeyCode.RightShift, false);
            Set(field.Snap(number + direction * (shift ? 1f : field.Nudge)));
            input.SetTextWithoutNotify(field.Show(number));
        }

        private void Set(float value)
        {
            if (value == number)
                return;
            number = value;
            values.SetNumber(field.Key, value);
            KwForm.Changed();
        }

        private void Cycle(int direction)
        {
            int count = field.Options?.Length ?? 0;
            if (count < 2)
                return;
            index = ((index + direction) % count + count) % count;
            values.SetIndex(field.Key, index);
            ShowOption();
            KwForm.Changed();
        }

        private void ShowOption()
        {
            string[] names = field.Options ?? Array.Empty<string>();
            if (option != null)
                option.text = index >= 0 && index < names.Length ? names[index] : "";
        }

        private void Run(Action action)
        {
            if (Live)
                KwForm.Safe("Broadside form " + field.Key, action);
        }

        // Digits; one decimal separator, a point or a comma; a minus first when the number may go below zero.
        private static char Accept(string text, int at, char added, bool signed)
        {
            if (added >= '0' && added <= '9')
                return added;
            if ((added == '.' || added == ',') && text.IndexOfAny(Separators) < 0)
                return added;
            return signed && added == '-' && at == 0 && text.IndexOf('-') < 0 ? added : '\0';
        }

        private static void Words(KwFormKit kit, string name, string words, float x, float y, float width)
        {
            TMP_Text text = KwFormCopies.Text(kit.Title, kit.Window, name, 18f, TextAlignmentOptions.MidlineLeft);
            text.color = KwFormCopies.Pale;
            text.text = words;
            KwFormCopies.Place(text.rectTransform, x, y, width, Height);
        }

        private static void SmallButton(KwFormKit kit, string name, string label, float x, float y, Action click)
        {
            Button button = KwFormCopies.Button(kit.Button, kit.Window, name, label, 20f, click);
            KwFormCopies.Place((RectTransform)button.transform, x, y, Small, Height);
        }
    }

    /// <summary>The mouse wheel over a number's box steps it: a notch up adds a step, a notch down takes one off.</summary>
    internal sealed class KwFormWheel : MonoBehaviour, IScrollHandler
    {
        public KwFormRow? Row;

        public void OnScroll(PointerEventData eventData)
        {
            float notch = eventData != null ? eventData.scrollDelta.y : 0f;
            if (Row != null && notch != 0f)
                Row.Wheel(notch > 0f ? 1 : -1);
        }
    }
}
