namespace EliteCreaturesReborn.Traits
{
    /// <summary>
    /// The fifteen mutations, declared in the order the specification lists them. That order is authoritative:
    /// it is the order names are assembled in and the order stars take their colours. The numeric value doubles
    /// as the bit index used when a creature's mutation set is packed into a single int for the ZDO.
    /// </summary>
    public enum Mutation
    {
        Mad = 0,
        Bloated = 1,
        Cloaked = 2,
        Splintering = 3,
        Leeching = 4,
        Warding = 5,
        Plated = 6,
        Miasmic = 7,
        Devouring = 8,
        Thieving = 9,
        Gilded = 10,
        Blinking = 11,
        Relentless = 12,
        Juggernaut = 13,
        Screecher = 14,
    }
}
