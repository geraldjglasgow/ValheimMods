using System.Globalization;
using Translations;

namespace PackPanel.Backpacks
{
    /// <summary>
    /// Each backpack's description, which says what it gives, so it is made from the words and the backpacks file's
    /// numbers: its look, where it is worn, its slots and carry weight, and the portal line for a pack that has one.
    /// <see cref="Crafting.CraftedWords"/> writes it again when the file or the language changes. The packs' names and
    /// the other backpack words are in <c>translations/English.yml</c>.
    /// </summary>
    public static class BackpackWords
    {
        public static void DescribeAll()
        {
            foreach (BackpackKind kind in BackpackCatalog.All)
                Lang.Add(kind.DescriptionToken, Describe(kind));
        }

        private static string Describe(BackpackKind kind)
        {
            BackpackStats stats = kind.Stats;
            string carry = stats.Carry.ToString("0", CultureInfo.InvariantCulture);
            string gives = stats.Carry > 0f
                ? Lang.Format("packpanel_backpack_gives_slots_carry", stats.Slots, carry)
                : Lang.Format("packpanel_backpack_gives_slots", stats.Slots);
            string text = Lang.Format("packpanel_backpack_describe", Lang.Get(kind.LookKey), gives);
            return stats.Portal ? Lang.Format("packpanel_backpack_describe_portal", text) : text;
        }
    }
}
