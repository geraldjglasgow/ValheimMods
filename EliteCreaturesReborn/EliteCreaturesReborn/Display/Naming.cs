using EliteCreaturesReborn.Traits;
using Translations;

namespace EliteCreaturesReborn.Display
{
    /// <summary>
    /// Builds the authoritative tell: a creature's mutations as words before its own name, in specification order.
    /// "Mad Greydwarf"; "Bloated Warding Miasmic Troll". A boss's aspects are its words the same way, headline first:
    /// "Twin Bonemass"; "Bountiful Enraged Mending Eikthyr". The name is always complete even when there are more
    /// mutations than stars, so it, not the colour, is what a player reads. The words are in the player's language, put
    /// together by two patterns of the word file: <c>ecr_name_words</c> joins two words, <c>ecr_name_decorated</c> sets
    /// the words beside the name, so a language may put them after it.
    /// </summary>
    public static class Naming
    {
        public static string Decorate(CreatureTraits traits, string baseName)
        {
            if (traits == null || string.IsNullOrEmpty(baseName)
                || (!traits.Any && traits.Aspect == Aspect.None && traits.ExtraAspects == 0))
            {
                return baseName;
            }
            string? words = null;
            foreach (Aspect aspect in traits.Aspects())
            {
                words = Join(words, AspectCatalog.Name(aspect)); // a boss: "Bountiful Enraged Mending Eikthyr"
            }
            foreach (Mutation mutation in traits.Active())
            {
                words = Join(words, MutationCatalog.Name(mutation));
            }
            return words == null ? baseName : Lang.Format("ecr_name_decorated", words, baseName);
        }

        private static string Join(string? words, string word) =>
            words == null ? word : Lang.Format("ecr_name_words", words, word);
    }
}
