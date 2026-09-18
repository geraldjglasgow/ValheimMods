namespace EliteCreaturesReborn.Traits
{
    /// <summary>
    /// The static facts about each mutation the specification fixes: the display word that goes in a creature's
    /// name, and the default star colour. Tunable per-mutation numbers live in the config classes, not here.
    /// </summary>
    public static class MutationCatalog
    {
        /// <summary>Every mutation, in specification order. Iterate this, never <c>Enum.GetValues</c>, so order is guaranteed.</summary>
        public static readonly Mutation[] InOrder =
        {
            Mutation.Mad, Mutation.Bloated, Mutation.Cloaked, Mutation.Splintering, Mutation.Leeching,
            Mutation.Warding, Mutation.Plated, Mutation.Miasmic, Mutation.Devouring, Mutation.Thieving,
        };

        /// <summary>The mutation a rule-file name (its display word) refers to, or null for an unknown word.</summary>
        public static Mutation? FromName(string name)
        {
            foreach (Mutation mutation in InOrder)
            {
                if (string.Equals(Word(mutation), name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return mutation;
                }
            }
            return null;
        }

        /// <summary>The word placed before the creature's own name, e.g. "Mad" in "Mad Greydwarf".</summary>
        public static string Word(Mutation mutation) => mutation switch
        {
            Mutation.Mad => "Mad",
            Mutation.Bloated => "Bloated",
            Mutation.Cloaked => "Cloaked",
            Mutation.Splintering => "Splintering",
            Mutation.Leeching => "Leeching",
            Mutation.Warding => "Warding",
            Mutation.Plated => "Plated",
            Mutation.Miasmic => "Miasmic",
            Mutation.Devouring => "Devouring",
            Mutation.Thieving => "Thieving",
            _ => "",
        };

        /// <summary>The default star colour as an "#RRGGBB" string; the palette config seeds itself from these.</summary>
        public static string DefaultColorHex(Mutation mutation) => mutation switch
        {
            Mutation.Mad => "#E23030",         // Red
            Mutation.Bloated => "#8B5A2B",     // Brown
            Mutation.Cloaked => "#4AA6FF",     // Blue - kept bright/light so it reads apart from Warding's navy on a small star
            Mutation.Splintering => "#FFFFFF", // Bright white
            Mutation.Leeching => "#33CC33",    // Green
            Mutation.Warding => "#101C50",     // Dark blue, near navy - genuinely dark against snow and night
            Mutation.Plated => "#F2C40C",      // Yellow
            Mutation.Miasmic => "#1E6B2E",     // Dark green
            Mutation.Devouring => "#7A0F0F",   // Dark red
            Mutation.Thieving => "#A64BE0",    // Violet
            _ => "#FFFFFF",
        };
    }
}
