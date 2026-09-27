namespace OpenKeep.Homestead
{
    /// <summary>What area repair did with one neighbour, in the order of the checks.</summary>
    public enum RepairOutcome
    {
        /// <summary>No piece or no network view: nothing the hammer could repair.</summary>
        NotAPiece,

        /// <summary>The piece's crafting station is not within build range of the player.</summary>
        NoStation,

        /// <summary>The piece stands in a ward the player has no access to.</summary>
        Warded,

        /// <summary><c>WearNTear.Repair</c> refused: full health, or repaired less than a second ago.</summary>
        Undamaged,

        /// <summary>The game's repair was sent to the piece's owner.</summary>
        Repaired
    }
}
