using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A part's values as an edit sees them (<see cref="KwEdit"/>), by the keys of its tool's window: each a short text, a
    /// number written in the invariant culture or a choice by its saved name (a tier's id, a hull shape, a curve's letter,
    /// a colour's code), so a part's own values read back exactly as its saved text has them. An edit compares the
    /// window's values as it opened with its values at OK (<see cref="Changed"/>) and gives each part only the keys the
    /// player changed (<see cref="With"/>): a part keeps every other value of its own. A key one side has and the other has
    /// not counts as changed (a sail added to a mast or taken off it). Pure.
    /// </summary>
    public sealed class KwPartValues
    {
        private readonly SortedDictionary<string, string> values;

        public KwPartValues() => values = new SortedDictionary<string, string>(StringComparer.Ordinal);

        private KwPartValues(SortedDictionary<string, string> from) => values = new SortedDictionary<string, string>(from, StringComparer.Ordinal);

        /// <summary>A key's value; empty for a key it has not.</summary>
        public string this[string key]
        {
            get => values.TryGetValue(key, out string value) ? value : "";
            set => values[key] = value ?? "";
        }

        public bool Has(string key) => values.ContainsKey(key);

        public void Remove(string key) => values.Remove(key);

        public IEnumerable<string> Keys => values.Keys;

        /// <summary>A key's number; the fallback for a key it has not, or one that is not a number.</summary>
        public float Number(string key, float fallback = 0f) =>
            float.TryParse(this[key], NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : fallback;

        /// <summary>A key's whole number, rounded; the fallback as <see cref="Number"/>.</summary>
        public int Whole(string key, int fallback = 0) => Mathf.RoundToInt(Number(key, fallback));

        public void Set(string key, float value) => values[key] = F(value);

        /// <summary>A number as the values write it: invariant, at most four decimals.</summary>
        public static string F(float value) => value.ToString("0.####", CultureInfo.InvariantCulture);

        public KwPartValues Copy() => new KwPartValues(values);

        /// <summary>The keys whose values differ between two, in order; a key only one of them has counts.</summary>
        public static List<string> Changed(KwPartValues before, KwPartValues after)
        {
            List<string> keys = new List<string>();
            foreach (KeyValuePair<string, string> pair in after.values)
            {
                if (!before.values.TryGetValue(pair.Key, out string was) || was != pair.Value)
                    keys.Add(pair.Key);
            }
            foreach (string key in before.values.Keys)
            {
                if (!after.values.ContainsKey(key))
                    keys.Add(key);
            }
            keys.Sort(StringComparer.Ordinal);
            return keys;
        }

        /// <summary>These values with the keys given taken from the edited ones (a key the edited values lack is taken away).</summary>
        public KwPartValues With(KwPartValues edited, IEnumerable<string> keys)
        {
            KwPartValues merged = Copy();
            foreach (string key in keys)
            {
                if (edited.Has(key))
                    merged[key] = edited[key];
                else
                    merged.Remove(key);
            }
            return merged;
        }

        public override string ToString()
        {
            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, string> pair in values)
                parts.Add(pair.Key + "=" + pair.Value);
            return string.Join(";", parts);
        }
    }
}
