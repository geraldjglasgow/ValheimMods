using System;
using System.Collections.Generic;
using EliteCreaturesReborn.Recap.Window;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EliteCreaturesReborn.Altars
{
    /// <summary>What the altar window's small choice buttons are copied from and how a chosen one looks: the compendium's
    /// Close button, pushed in (the game's pressed button) inside the inventory's slot frame in the list's orange, its
    /// label white; without a frame, its label in the highlight orange.</summary>
    internal sealed class ButtonLook
    {
        public Button Template = null!;
        public Color Normal = Color.white;
        public Sprite? Plain;
        public Sprite? Pressed;
        public Image? Frame;
        public Color Tint;

        public static ButtonLook Of(Button template, AltarPane pane)
        {
            TMP_Text? label = template.GetComponentInChildren<TMP_Text>(true);
            bool swaps = template.transition == Selectable.Transition.SpriteSwap;
            return new ButtonLook
            {
                Template = template,
                Normal = label != null ? label.color : Color.white,
                Plain = template.image != null ? template.image.sprite : null,
                Pressed = swaps ? template.spriteState.pressedSprite : null,
                Frame = pane.StarFrame,
                Tint = pane.StarTint,
            };
        }
    }

    /// <summary>
    /// A block of small choice buttons in the altar window, a set number to a row and the rows under one another, as wide
    /// as the inset's contents: each button stands for one value and reports it when clicked, and any of them can be
    /// marked chosen. The star counts are one row of them, a chosen Bountiful's extras a block.
    /// </summary>
    internal sealed class ChoiceButtons
    {
        private static readonly Color Chosen = new Color(1f, 0.72f, 0.25f, 1f);

        private readonly ButtonLook _look;
        private readonly RectTransform _root;
        private readonly string _name;
        private readonly float _height;
        private readonly float _gap;
        private readonly bool _fit;
        private readonly List<int> _values = new List<int>();
        private readonly List<TMP_Text?> _labels = new List<TMP_Text?>();
        private readonly List<GameObject?> _marks = new List<GameObject?>();
        private readonly List<Image?> _faces = new List<Image?>();
        private int _rows;

        /// <summary><paramref name="fit"/> lets a label too long for its button shrink to fit; otherwise it keeps its size.</summary>
        public ChoiceButtons(ButtonLook look, RectTransform root, string name, float height, float gap, bool fit)
        {
            _look = look;
            _root = root;
            _name = name;
            _height = height;
            _gap = gap;
            _fit = fit;
        }

        /// <summary>The block's height: its rows and the gaps between them; 0 with no buttons.</summary>
        public float Height => _rows == 0 ? 0f : _rows * _height + (_rows - 1) * _gap;

        /// <summary>One button per value, in its place for <paramref name="columns"/> to a row, each at most
        /// <paramref name="widest"/> wide; the old buttons go.</summary>
        public void Build(List<int> values, Func<int, string> label, Action<int> clicked, int columns, float widest)
        {
            Clear();
            if (values.Count == 0)
            {
                return;
            }
            columns = Mathf.Clamp(columns, 1, values.Count);
            float width = Mathf.Min(widest, (AltarWindowBuilder.Wide - (columns - 1) * _gap) / columns);
            for (int i = 0; i < values.Count; i++)
            {
                float x = i % columns * (width + _gap);
                Add(values[i], label(values[i]), clicked, x, i / columns * (_height + _gap), width);
            }
            _rows = (values.Count + columns - 1) / columns;
        }

        /// <summary>Each button marked as chosen or not by <paramref name="chosen"/>, asked of its value.</summary>
        public void Mark(Predicate<int> chosen)
        {
            for (int i = 0; i < _values.Count; i++)
            {
                MarkOne(i, chosen(_values[i]));
            }
        }

        private void Clear()
        {
            foreach (Transform old in _root)
            {
                UnityEngine.Object.Destroy(old.gameObject);
            }
            _values.Clear();
            _labels.Clear();
            _marks.Clear();
            _faces.Clear();
            _rows = 0;
        }

        private void Add(int value, string text, Action<int> clicked, float x, float y, float width)
        {
            Button button = UiParts.Button(_look.Template, _root, _name, text, () => clicked(value));
            UiParts.Place((RectTransform)button.transform, x, y, width, _height);
            TMP_Text? label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                Small(label);
            }
            _values.Add(value);
            _labels.Add(label);
            _marks.Add(Frame(button));
            _faces.Add(button.image);
        }

        // The label sized for the small button: one line filling it, shrunk only to fit where the block allows it.
        private void Small(TMP_Text text)
        {
            UiParts.Fill(text.rectTransform, 2f);
            text.enableAutoSizing = _fit;
            text.fontSizeMin = 11f;
            text.fontSizeMax = 16f;
            text.fontSize = 16f;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.alignment = TextAlignmentOptions.Center;
        }

        // The inventory's slot frame, rim only, in the list's orange inside the button's rim and under its label, shown
        // while it is a choice.
        private GameObject? Frame(Button button)
        {
            if (_look.Frame == null)
            {
                return null;
            }
            Image mark = UiParts.Image(_look.Frame, button.transform, "ecr_altar_choice_mark");
            mark.fillCenter = false;
            mark.color = _look.Tint;
            UiParts.Fill(mark.rectTransform, 3f);
            mark.transform.SetAsFirstSibling();
            mark.gameObject.SetActive(false);
            return mark.gameObject;
        }

        // A chosen button pushed in, in its frame with a white label; without a frame, its label in the highlight orange.
        // Hovering still lights it: the game's sprite swap lays its hover sprite over the face.
        private void MarkOne(int index, bool chosen)
        {
            Image? face = _faces[index];
            if (face != null && _look.Plain != null && _look.Pressed != null)
            {
                face.sprite = chosen ? _look.Pressed : _look.Plain;
            }
            GameObject? mark = _marks[index];
            mark?.SetActive(chosen);
            TMP_Text? label = _labels[index];
            if (label != null)
            {
                label.color = !chosen ? _look.Normal : mark != null ? Color.white : Chosen;
            }
        }
    }
}
