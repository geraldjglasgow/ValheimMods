using System.Text;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Display
{
    /// <summary>
    /// Builds the authoritative tell: a creature's mutations as words before its own name, in specification order.
    /// "Mad Greydwarf"; "Bloated Warding Miasmic Troll". A boss's aspect is its word the same way: "Twin Bonemass". The name is always complete even when there are more
    /// mutations than stars, so it, not the colour, is what a player reads.
    /// </summary>
    public static class Naming
    {
        public static string Decorate(CreatureTraits traits, string baseName)
        {
            if (traits == null || (!traits.Any && traits.Aspect == Aspect.None) || string.IsNullOrEmpty(baseName))
            {
                return baseName;
            }
            StringBuilder builder = new StringBuilder();
            if (traits.Aspect != Aspect.None)
            {
                builder.Append(AspectCatalog.Word(traits.Aspect)).Append(' '); // a boss: "Enraged Eikthyr"
            }
            foreach (Mutation mutation in traits.Active())
            {
                builder.Append(MutationCatalog.Word(mutation)).Append(' ');
            }
            builder.Append(baseName);
            return builder.ToString();
        }
    }
}
