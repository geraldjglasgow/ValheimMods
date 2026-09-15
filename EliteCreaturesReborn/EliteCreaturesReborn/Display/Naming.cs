using System.Text;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Display
{
    /// <summary>
    /// Builds the authoritative tell: a creature's mutations as words before its own name, in specification order.
    /// "Mad Greydwarf"; "Bloated Warding Miasmic Troll". The name is always complete even when there are more
    /// mutations than stars, so it, not the colour, is what a player reads.
    /// </summary>
    public static class Naming
    {
        public static string Decorate(CreatureTraits traits, string baseName)
        {
            if (traits == null || !traits.Any || string.IsNullOrEmpty(baseName))
            {
                return baseName;
            }
            StringBuilder builder = new StringBuilder();
            foreach (Mutation mutation in traits.Active())
            {
                builder.Append(MutationCatalog.Word(mutation)).Append(' ');
            }
            builder.Append(baseName);
            return builder.ToString();
        }
    }
}
