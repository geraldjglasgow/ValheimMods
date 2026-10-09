namespace OpenKeep.Blueprints.Copy
{
    /// <summary>
    /// What a click with the Copy tool takes: the piece under the crosshair (a click), its building (Shift), the joined
    /// pieces of one type (G) or every piece of that type in its building (Shift + G).
    /// </summary>
    public enum CopyMode
    {
        Building,
        Piece,
        SameType,
        BuildingType,
    }
}
