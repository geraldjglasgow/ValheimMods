namespace EliteCrafting.Affixes
{
    /// <summary>
    /// The chain of format migrations (item-data.md section 8), each a pure step over the parsed state, run in memory
    /// on read. The dictionary is untouched until the item is next written for its own reasons, which then writes the
    /// current form. Format 1 is the first: the chain is empty. A new step ships with a test holding a frozen v(n)
    /// dictionary and its expected v(n+1) form.
    /// </summary>
    internal static class ItemMigrations
    {
        public static StateData Upgrade(StateData state)
        {
            if (state.Newer || state.Format >= ItemKeys.CurrentFormat)
            {
                return state;
            }
            // v1 -> v2 -> ... steps go here, in order.
            StateData upgraded = state.Copy();
            upgraded.Format = ItemKeys.CurrentFormat;
            return upgraded;
        }
    }
}
