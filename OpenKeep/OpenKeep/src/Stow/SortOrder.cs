namespace OpenKeep.Stow
{
    /// <summary>The orders of the Sort actions: item type then name, name, heaviest first, most valuable first.</summary>
    public enum SortOrder
    {
        Category,
        Name,
        Weight,
        Value,
    }
}
