namespace EliteCreaturesPack.Mimic
{
    /// <summary>
    /// The one question everything that could give a mimic away asks: is this creature a mimic still pretending to be a
    /// chest? While it is, nothing may show on or over it - no name plate, health bar, star row, size change, star look
    /// or mutation effect - and its hover text is the chest's. The answer comes from the game's own sleep state, which
    /// the ZDO carries to every client, so every machine answers the same.
    /// </summary>
    public static class Dormancy
    {
        public static bool Holds(Character character) =>
            character != null && character.TryGetComponent(out MimicDisguise disguise) && disguise.IsDormant;
    }
}
