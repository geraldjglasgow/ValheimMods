namespace OpenKeep.Blueprints.Copy
{
    /// <summary>What a click with the Copy tool takes: the piece under the crosshair (a click), its building (Shift) or the joined pieces of one type (G).</summary>
    public enum CopyMode
    {
        Building,
        Piece,
        SameType,
    }
}
