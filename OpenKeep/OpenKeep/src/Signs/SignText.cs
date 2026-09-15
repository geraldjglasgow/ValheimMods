using System;
using System.Collections.Generic;
using System.Text;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Signs
{
    /// <summary>
    /// The words on a sign: the container's item kinds by localized display name, most numerous first, with the
    /// count after the name when <c>Show Counts</c> is on. At most <c>Max Items</c> kinds and never more than
    /// <c>Max Characters</c> characters: kinds are added until the next would not fit, and an ellipsis (one
    /// character, counted) ends the text when any kind was left out. An empty container shows <c>Empty Text</c>.
    /// </summary>
    public static class SignText
    {
        public const string Ellipsis = "…";

        public static string Build(Inventory inventory)
        {
            List<KeyValuePair<string, int>> kinds = Kinds(inventory);
            if (kinds.Count == 0)
                return SignsSettings.EmptyText.Value ?? "";
            int maxItems = Mathf.Clamp(SignsSettings.MaxItems.Value, 1, 20);
            int maxChars = Mathf.Clamp(SignsSettings.MaxCharacters.Value, 10, 200);
            bool counts = SignsSettings.ShowCounts.Value;
            StringBuilder text = new StringBuilder();
            int listed = 0;
            for (; listed < kinds.Count && listed < maxItems; listed++)
            {
                string entry = Entry(kinds[listed], counts);
                int reserve = listed + 1 < kinds.Count ? Ellipsis.Length : 0;
                if (text.Length + (listed > 0 ? 2 : 0) + entry.Length + reserve > maxChars)
                    break;
                text.Append(listed > 0 ? ", " : "").Append(entry);
            }
            if (listed < kinds.Count)
                text.Append(Ellipsis);
            return text.ToString();
        }

        private static string Entry(KeyValuePair<string, int> kind, bool counts)
        {
            return counts ? kind.Key + " " + kind.Value : kind.Key;
        }

        /// <summary>Display name to total count over every stack, most numerous first, ties by name.</summary>
        private static List<KeyValuePair<string, int>> Kinds(Inventory inventory)
        {
            Dictionary<string, int> totals = new Dictionary<string, int>(StringComparer.Ordinal);
            if (inventory != null)
            {
                foreach (ItemDrop.ItemData item in inventory.GetAllItems())
                {
                    if (item == null || item.m_shared == null || item.m_stack <= 0)
                        continue;
                    string name = ItemNames.DisplayName(item);
                    if (string.IsNullOrEmpty(name))
                        name = item.m_shared.m_name;
                    totals.TryGetValue(name, out int count);
                    totals[name] = count + item.m_stack;
                }
            }
            List<KeyValuePair<string, int>> kinds = new List<KeyValuePair<string, int>>(totals);
            kinds.Sort((a, b) => b.Value != a.Value ? b.Value.CompareTo(a.Value) : string.Compare(a.Key, b.Key, StringComparison.Ordinal));
            return kinds;
        }
    }
}
