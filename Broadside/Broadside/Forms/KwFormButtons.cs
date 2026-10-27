using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Broadside
{
    /// <summary>
    /// A button row of the <see cref="KwForm"/> window (<see cref="KwField.Buttons"/>): its buttons side by side, as many
    /// to a line as the field asks, filling the row from its start to the window's right margin, more lines under one
    /// another; each a copy of the game's button (<see cref="KwFormCopies.Button"/>), the lit one with its label in the
    /// game's highlight orange and its wood warmed. A press tells the caller which button it was, and the caller may build
    /// the window again (<see cref="KwForm.Refill"/>). The label's colour is set before the window wakes, so the game's
    /// button colours keep it as the button's own.
    /// </summary>
    internal static class KwFormButtons
    {
        private const float Gap = 6f, FontSize = 15f;
        private static readonly Color LitText = new Color(1f, 0.72f, 0.24f), LitWood = new Color(1f, 0.86f, 0.62f);

        public static void Make(KwFormKit kit, KwField field, float y, float left)
        {
            string[] names = field.Options ?? Array.Empty<string>();
            int per = field.PerLine;
            float width = (KwFormRow.Width - KwFormRow.Margin - left - (per - 1) * Gap) / per;
            for (int i = 0; i < names.Length; i++)
            {
                int pressed = i;
                Button button = KwFormCopies.Button(kit.Button, kit.Window, field.Key + i, names[i], FontSize, () => field.Press?.Invoke(pressed));
                float x = left + (i % per) * (width + Gap), top = y - (i / per) * KwFormView.Spacing;
                KwFormCopies.Place((RectTransform)button.transform, x, top, width, KwFormRow.Height);
                if (i == field.Index)
                    Light(button);
            }
        }

        // The lit button: its label orange and bold, its wood warmed.
        private static void Light(Button button)
        {
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.color = LitText;
                text.fontStyle |= FontStyles.Bold;
            }
            if (button.targetGraphic != null)
                button.targetGraphic.color = LitWood;
        }
    }
}
