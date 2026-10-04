using System.Text;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Display
{
    /// <summary>
    /// Builds the authoritative tell: a creature's mutations as words before its own name, in specification order.
    /// "Mad Greydwarf"; "Bloated Warding Miasmic Troll". A boss's aspects are its words the same way, headline first:
    /// "Twin Bonemass"; "Bountiful Enraged Mending Eikthyr". The name is always complete even when there are more
    /// mutations than stars, so it, not the colour, is what a player reads.
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
            StringBuilder builder = new StringBuilder();
            foreach (Aspect aspect in traits.Aspects())
            {
                builder.Append(AspectCatalog.Word(aspect)).Append(' '); // a boss: "Bountiful Enraged Mending Eikthyr"
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
