using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Commands
{
    /// <summary>
    /// The words of <c>elite spawn</c> for a prefab another mod registered traits for (<see cref="Registrations"/>): such a
    /// creature may carry mutations and aspects together, boss or not, so both are read. A mutation word adds that
    /// mutation; the first aspect word is its aspect, and aspect words after Bountiful are its extras (rolled from its
    /// rotation when none follow), as for a boss. Like every <c>elite spawn</c> it bypasses every roll and limit: the
    /// creature is exactly what was typed (<c>elite spawn ECP_Warlord 2 Enraged Mad Juggernaut</c>).
    /// </summary>
    internal static class SpawnWords
    {
        public static CreatureTraits Read(Terminal.ConsoleEventArgs args, string prefab, int stars)
        {
            CreatureTraits traits = new CreatureTraits(stars, 0);
            for (int i = 4; i < args.Length; i++)
            {
                Take(args, traits, args[i]);
            }
            if (traits.Aspect == Aspect.Bountiful && traits.ExtraAspects == 0)
            {
                traits.ExtraAspects = AspectRoller.RollExtras(prefab);
            }
            return traits;
        }

        private static void Take(Terminal.ConsoleEventArgs args, CreatureTraits traits, string word)
        {
            Mutation? mutation = MutationCatalog.FromName(word);
            Aspect? aspect = mutation == null ? AspectCatalog.FromName(word) : null;
            string? refusal = mutation == null ? Refusal(traits, aspect) : null;
            if (refusal != null)
            {
                EliteCommands.Reply(args, $"elite spawn: '{word}' skipped - {refusal}.");
            }
            else if (mutation != null)
            {
                traits.Add(mutation.Value);
            }
            else if (traits.Aspect == Aspect.None)
            {
                traits.Aspect = aspect!.Value; // `none` leaves it plain
            }
            else
            {
                traits.ExtraAspects |= 1 << (int)aspect!.Value;
            }
        }

        // Why an aspect word cannot be taken here; null when it can.
        private static string? Refusal(CreatureTraits traits, Aspect? aspect) => aspect switch
        {
            null => "not a mutation or an aspect",
            _ when traits.Aspect == Aspect.None => null,
            _ when traits.Aspect != Aspect.Bountiful => "only Bountiful carries more than one aspect",
            _ => SpawnCommand.Refusal(aspect, traits.ExtraAspects),
        };
    }
}
