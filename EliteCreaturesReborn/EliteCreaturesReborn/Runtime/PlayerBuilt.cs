namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// A creature players build with the hammer (the game's training dummy, or any mod's creature that is also a piece):
    /// left exactly as the game makes it. No stars, no mutations, and above all no Splintering copies, each of which
    /// would dismantle for the piece's full cost (GitHub #36). One that rolled before this check is plain again too.
    /// </summary>
    public static class PlayerBuilt
    {
        public static bool Is(Character character) =>
            character.m_faction == Character.Faction.TrainingDummy || character.GetComponent<Piece>() != null;
    }
}
