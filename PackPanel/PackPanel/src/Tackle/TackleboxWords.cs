using Translations;

namespace PackPanel.Tackle
{
    /// <summary>
    /// Each tacklebox's description, which says how many cells it gives, so it is made from the words and the tackleboxes
    /// file's numbers. <see cref="Crafting.CraftedWords"/> writes it again when the file or the language changes. The
    /// boxes' names, the Tacklebox slot's captions and its message are in <c>translations/English.yml</c>.
    /// </summary>
    public static class TackleboxWords
    {
        public static void DescribeAll()
        {
            foreach (TackleboxKind kind in TackleboxCatalog.All)
                Lang.Add(kind.DescriptionToken, Describe(kind));
        }

        private static string Describe(TackleboxKind kind)
        {
            int cells = kind.Stats.Cells;
            string gives = cells == 1 ? Lang.Get("packpanel_tacklebox_one_cell") : Lang.Format("packpanel_tacklebox_cells", cells);
            return Lang.Format("packpanel_tacklebox_describe", Lang.Get(kind.LookKey), gives);
        }
    }
}
