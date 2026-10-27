using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// What a <see cref="KwForm"/>'s fields hold, by key: each number as it is kept (within its range, on its step) and
    /// each choice's option (a button row holds nothing: its presses go straight to the caller). The form hands its callers
    /// a copy, so one they keep never changes under them. A key the form has no field of throws, so a misspelt key shows
    /// up in the log at once.
    /// </summary>
    public sealed class KwFormValues
    {
        private readonly Dictionary<string, float> numbers;
        private readonly Dictionary<string, int> indexes;
        private readonly Dictionary<string, string[]> options;

        internal KwFormValues(IEnumerable<KwField> fields)
        {
            numbers = new Dictionary<string, float>();
            indexes = new Dictionary<string, int>();
            options = new Dictionary<string, string[]>();
            foreach (KwField field in fields)
            {
                if (field.IsButtons)
                    continue;
                if (field.Options != null)
                {
                    indexes[field.Key] = field.Index;
                    options[field.Key] = field.Options;
                }
                else
                    numbers[field.Key] = field.Value;
            }
        }

        private KwFormValues(KwFormValues from)
        {
            numbers = new Dictionary<string, float>(from.numbers);
            indexes = new Dictionary<string, int>(from.indexes);
            options = from.options;
        }

        /// <summary>Whether the form has a number or a choice of that key.</summary>
        public bool Has(string key) => numbers.ContainsKey(key) || indexes.ContainsKey(key);

        public float Number(string key) =>
            numbers.TryGetValue(key, out float value) ? value : throw new KeyNotFoundException($"The form has no number '{key}'");

        /// <summary>The text of the option picked; empty for a choice without options.</summary>
        public string Choice(string key)
        {
            int index = ChoiceIndex(key);
            string[] names = options[key];
            return index >= 0 && index < names.Length ? names[index] : "";
        }

        public int ChoiceIndex(string key) =>
            indexes.TryGetValue(key, out int index) ? index : throw new KeyNotFoundException($"The form has no choice '{key}'");

        internal void SetNumber(string key, float value) => numbers[key] = value;

        internal void SetIndex(string key, int index) => indexes[key] = index;

        internal KwFormValues Copy() => new KwFormValues(this);
    }
}
