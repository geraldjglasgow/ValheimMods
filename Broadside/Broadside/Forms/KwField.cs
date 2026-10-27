using System;
using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One row of a <see cref="KwForm"/>. A number is typed, stepped with the mouse wheel over its box or with the small -
    /// and + buttons beside it; when the typing ends it is kept within its minimum and maximum and rounded to its step, and
    /// it is shown with as many decimals as the step has, its unit after it. Numbers are read and written in the invariant
    /// culture, and a comma typed as the decimal separator counts as a point. A choice is cycled with the &lt; and &gt;
    /// buttons beside it, or by clicking the option shown; options are shown by name. A button row is a row of buttons
    /// side by side (as many to a line as it is given, on more lines when there are more), one of them lit (the one picked,
    /// or none); a press tells the caller which button it was (<see cref="Press"/>), and holds no value of its own.
    /// </summary>
    public sealed class KwField
    {
        private KwField(string key, string label, string unit, string[]? options)
        {
            Key = key ?? "";
            Label = label ?? "";
            Unit = unit ?? "";
            Options = options;
        }

        public string Key { get; }
        public string Label { get; }
        public string Unit { get; }
        public float Value { get; private set; }
        public float Min { get; private set; }
        public float Max { get; private set; }
        public float Step { get; private set; }

        /// <summary>The options of a choice, or a button row's buttons; null for a number.</summary>
        public string[]? Options { get; }

        /// <summary>A choice's option, or a button row's lit button (-1 for none).</summary>
        public int Index { get; private set; }

        public bool IsChoice => Options != null && Press == null;

        /// <summary>What a button row's press runs, with the button's index; null for any other field.</summary>
        public Action<int>? Press { get; private set; }

        public bool IsButtons => Press != null;

        /// <summary>How many of a button row's buttons go on one line.</summary>
        public int PerLine { get; private set; } = 1;

        /// <summary>How many of the window's lines the row takes: a button row as many as its buttons fill, any other one.</summary>
        public int Lines => IsButtons ? Mathf.Max(1, (Options!.Length + PerLine - 1) / PerLine) : 1;

        /// <summary>The decimals a number is shown with: as many as its step has (three without a step, four at most).</summary>
        internal int Decimals { get; private set; }

        /// <summary>What a wheel notch or a - / + click adds: the step, or one in the last decimal shown without a step.</summary>
        internal float Nudge => Step > 0f ? Step : Mathf.Pow(10f, -Decimals);

        public static KwField Number(string key, string label, float value, float min, float max, float step, string unit = "m")
        {
            KwField field = new KwField(key, label, unit, null)
            {
                Min = Mathf.Min(min, max),
                Max = Mathf.Max(min, max),
                Step = step > 0f ? step : 0f,
            };
            field.Decimals = DecimalsOf(field.Step);
            field.Value = field.Snap(value);
            return field;
        }

        public static KwField Choice(string key, string label, string[] options, int index)
        {
            string[] names = options ?? Array.Empty<string>();
            return new KwField(key, label, "", names) { Index = names.Length == 0 ? 0 : Mathf.Clamp(index, 0, names.Length - 1) };
        }

        /// <summary>A row of buttons, perLine to a line, the one at lit shown lit (-1 for none); a press runs press with its index.</summary>
        public static KwField Buttons(string key, string label, string[] buttons, int lit, Action<int> press, int perLine = 4)
        {
            string[] names = buttons ?? Array.Empty<string>();
            return new KwField(key, label, "", names) { Index = lit, Press = press ?? (_ => { }), PerLine = Mathf.Max(1, perLine) };
        }

        /// <summary>A number as it is kept: rounded to the step, then within the minimum and maximum, cut to its decimals.</summary>
        internal float Snap(float value)
        {
            if (float.IsNaN(value))
                value = Min;
            if (Step > 0f)
                value = Mathf.Round(value / Step) * Step;
            value = Mathf.Clamp(value, Min, Max);
            return (float)Math.Round(value, Decimals);
        }

        internal string Show(float value) => value.ToString("F" + Decimals, CultureInfo.InvariantCulture);

        /// <summary>A typed number, a comma or a point as its decimal separator; false for anything else (an empty box, "-").</summary>
        internal static bool TryRead(string text, out float value)
        {
            string plain = (text ?? "").Trim().Replace(',', '.');
            NumberStyles styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
            bool read = float.TryParse(plain, styles, CultureInfo.InvariantCulture, out value);
            return read && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static int DecimalsOf(float step)
        {
            if (step <= 0f)
                return 3;
            for (int decimals = 0; decimals < 4; decimals++)
            {
                float scaled = step * Mathf.Pow(10f, decimals);
                if (Mathf.Abs(scaled - Mathf.Round(scaled)) < 0.001f)
                    return decimals;
            }
            return 4;
        }
    }
}
