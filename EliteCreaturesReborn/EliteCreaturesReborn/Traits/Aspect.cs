namespace EliteCreaturesReborn.Traits
{
    /// <summary>
    /// The boss aspects: the one modifier a boss carries in place of mutations. The numeric value is what a boss's and
    /// an altar's ZDO store, so it is fixed - append new aspects, never renumber. <see cref="None"/> is the plain
    /// vanilla fight and a real outcome of every roll, not the absence of one.
    /// </summary>
    public enum Aspect
    {
        None = 0,
        Reflective = 1,
        Shielded = 2,
        Mending = 3,
        Summoner = 4,
        Elementalist = 5,
        Enraged = 6,
        Twin = 7,
        Phantom = 8,
    }
}
