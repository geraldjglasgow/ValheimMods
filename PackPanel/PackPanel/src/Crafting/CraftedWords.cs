using PackPanel.Backpacks;
using PackPanel.Tackle;
using Translations;

namespace PackPanel.Crafting
{
    /// <summary>
    /// The crafted items' descriptions (<see cref="BackpackWords"/>, <see cref="TackleboxWords"/>): each says what its item
    /// file gives, so it is made from the words and the numbers and written into the words under the item's own
    /// description key (<see cref="Lang.Add"/>; the item's shared data holds the token). Written at start, again when an
    /// item file is applied and on every change of language. Writing a word raises <see cref="Lang.Changed"/> itself,
    /// which a write in progress ignores.
    /// </summary>
    public static class CraftedWords
    {
        private static bool writing;

        /// <summary>Once, at start: the descriptions now and again on every change of the words.</summary>
        public static void Install()
        {
            Lang.Changed += () =>
            {
                if (!writing)
                    WriteAll();
            };
            WriteAll();
        }

        public static void WriteAll()
        {
            writing = true;
            try
            {
                BackpackWords.DescribeAll();
                TackleboxWords.DescribeAll();
            }
            finally
            {
                writing = false;
            }
        }
    }
}
