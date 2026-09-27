namespace GrindstoneSkills
{
    /// <summary>
    /// One breeding check on the creature's owner (<see cref="BreedingCheck"/>), carried from the prefix to the postfix
    /// and the finalizer: the game's own values to put back, the keeper's level, and what was decided for a birth.
    /// </summary>
    public sealed class BreedingCall
    {
        /// <summary>The best Husbandry level within Keeper Range of the parent, read once for every roll of the check.</summary>
        public float Level { get; set; }

        /// <summary>The three game values below were changed for this check and must be put back.</summary>
        public bool Paced { get; set; }

        /// <summary>The game's own Procreation.m_pregnancyDuration (seconds from pregnancy to birth).</summary>
        public float PregnancyDuration { get; set; }

        /// <summary>The game's own Procreation.m_pregnancyChance (really the chance a love check is skipped).</summary>
        public float PregnancyChance { get; set; }

        /// <summary>The game's own Procreation.m_maxCreatures (the crowding limit).</summary>
        public int MaxCreatures { get; set; }

        /// <summary>The parent is pregnant and due: the game gives birth in this check.</summary>
        public bool Birth { get; set; }

        /// <summary>This birth is a twin's (<see cref="Keys.Twin"/>): it rolls no twins, and the mark is cleared after it.</summary>
        public bool WasTwin { get; set; }

        /// <summary>The twin roll was won: the parent is made pregnant again, already due, after the birth.</summary>
        public bool Twins { get; set; }

        /// <summary>How Better offspring's star reaches the newborn, when its roll was won.</summary>
        public OffspringStar.Route Star { get; set; }
    }
}
