using System;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Broadside
{
    /// <summary>
    /// The boxes a <see cref="KwForm"/> row shows its value in. A number is typed into a copy of the build menu's search
    /// field (the game's own input field type, its dark box, font and Ctrl + Backspace), with fresh events, one fixed text
    /// size and no placeholder; a choice shows in a box of the same look that is a button (which, like the form's other
    /// buttons, lets go of the selection after a click, so Enter never presses it). The search field is reached by
    /// name, as the HUD has it, since its type lives in an assembly this mod does not compile against; without a HUD
    /// there is none, and the form does not open.
    /// </summary>
    internal static class KwFormBoxes
    {
        private const float FontSize = 18f;

        private static readonly FieldInfo? SearchField = AccessTools.Field(typeof(BuildUi), "m_searchField");

        /// <summary>The game's input field the number boxes are copied from; null outside a world.</summary>
        public static TMP_InputField? Template()
        {
            BuildUi? build = Hud.instance != null ? Hud.instance.m_buildUi : null;
            return build != null ? SearchField?.GetValue(build) as TMP_InputField : null;
        }

        public static TMP_InputField Number(TMP_InputField template, Transform parent, string name)
        {
            TMP_InputField field = KwFormCopies.Copy(template, parent, name).GetComponent<TMP_InputField>();
            field.onValueChanged = new TMP_InputField.OnChangeEvent();
            field.onEndEdit = new TMP_InputField.SubmitEvent();
            field.onSubmit = new TMP_InputField.SubmitEvent();
            field.onSelect = new TMP_InputField.SelectionEvent();
            field.onDeselect = new TMP_InputField.SelectionEvent();
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.characterValidation = TMP_InputField.CharacterValidation.None;
            field.characterLimit = 10;
            field.richText = false;
            field.onFocusSelectAll = true;
            field.restoreOriginalTextOnEscape = true;
            field.navigation = new Navigation { mode = Navigation.Mode.None };
            Look(field);
            return field;
        }

        private static void Look(TMP_InputField field)
        {
            field.customCaretColor = true;
            field.caretColor = Color.white;
            field.caretWidth = 2;
            field.selectionColor = new Color(1f, 0.75f, 0.35f, 0.45f);
            if (field.textViewport != null)
            {
                field.textViewport.offsetMin = new Vector2(6f, 2f);
                field.textViewport.offsetMax = new Vector2(-6f, -2f);
            }
            if (field.textComponent != null)
                Centred(field.textComponent);
            if (field.placeholder is TMP_Text placeholder)
                placeholder.text = "";
        }

        /// <summary>A box in the number boxes' look that is a button, a text centred in it; a click runs the action.</summary>
        public static RectTransform Choice(TMP_InputField template, TMP_Text font, Transform parent, string name, Action click)
        {
            RectTransform box = KwFormCopies.Rect(name, parent);
            Image background = box.gameObject.AddComponent<Image>();
            if (template.GetComponent<Image>() is Image look)
            {
                background.sprite = look.sprite;
                background.type = look.type;
                background.color = look.color;
            }
            else
                background.color = new Color(0f, 0f, 0f, 0.55f);
            Button button = box.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => KwFormCopies.Clicked(name, click));
            TMP_Text text = KwFormCopies.Text(font, box, "Text", FontSize, TextAlignmentOptions.Center);
            KwFormCopies.Fill(text.rectTransform, 4f);
            text.color = Color.white;
            return box;
        }

        // One fixed size (the caret sits wrong on auto-sized text), centred, white.
        private static void Centred(TMP_Text text)
        {
            text.enableAutoSizing = false;
            text.fontSize = FontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.richText = false;
        }
    }
}
