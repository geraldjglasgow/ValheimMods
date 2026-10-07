using System.Collections.Generic;
using OpenKeep.Core;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// The chest search as a filter, worded like the recipe search: every word must appear, without regard to case, in
    /// the item's name in the game's language or in its prefab name; a leading - makes a word that must not. Blank text
    /// matches everything.
    /// </summary>
    public sealed class MimirQuery
    {
        public static readonly MimirQuery All = new MimirQuery("");

        private readonly List<string> words = new List<string>();
        private readonly List<string> notWords = new List<string>();

        public MimirQuery(string text)
        {
            Text = text ?? "";
            foreach (string raw in Text.ToLowerInvariant().Split(' ', '\t'))
            {
                string word = raw.Trim();
                bool not = word.StartsWith("-");
                if (not)
                    word = word.Substring(1);
                if (word.Length > 0)
                    (not ? notWords : words).Add(word);
            }
        }

        public string Text { get; }

        public bool Empty => words.Count + notWords.Count == 0;

        public bool Matches(ItemDrop.ItemData item)
        {
            if (Empty)
                return true;
            string text = (ItemNames.DisplayName(item) + " " + ItemNames.PrefabName(item)).ToLowerInvariant();
            foreach (string word in words)
            {
                if (!text.Contains(word))
                    return false;
            }
            foreach (string word in notWords)
            {
                if (text.Contains(word))
                    return false;
            }
            return true;
        }
    }
}
