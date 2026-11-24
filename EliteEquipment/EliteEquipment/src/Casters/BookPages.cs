using System;
using System.Collections.Generic;

namespace EliteEquipment.Casters
{
    /// <summary>What putting a page in a book came to.</summary>
    public enum PageAdd
    {
        Added,
        AlreadyIn,
        Full,
    }

    /// <summary>
    /// The spell pages in a spellbook (features/casters.md, "The spellbook"), saved on the book item itself (item custom
    /// data <see cref="Key"/>: spell keys separated by commas; the game saves it with the item in an inventory, a chest or
    /// on the ground), so a book dropped, traded or left in a chest carries its pages. A new book is empty: every spell,
    /// Mend included, is a page found in the world (user, 2026-11-02: "spellbook should not automatically come with the
    /// heal"). Each spell once per book, at most the book's <see cref="GearPiece.Pages"/> (2, 4, 8 by tier). A book
    /// holding more than its room (a Meadows book filled while it held 4) keeps every page, all of them castable, and
    /// takes no more until pages taken out bring it under its room: nothing here ever removes a page on its own. A wand
    /// casts only what the book in the caster's left hand holds (<see cref="Castable"/>).
    /// </summary>
    public static class BookPages
    {
        public const string Key = "EliteEquipment.pages";

        /// <summary>The page keys as saved (none for a new book); compared to see whether the pages changed.</summary>
        public static string Text(ItemDrop.ItemData book) =>
            book.m_customData.TryGetValue(Key, out string text) ? text : "";

        /// <summary>The spells in the book, in Spellbook order; none for no book.</summary>
        public static List<Spell> Of(ItemDrop.ItemData book)
        {
            var pages = new List<Spell>();
            if (book == null)
                return pages;
            string text = Text(book);
            foreach (Spell spell in Spells.All)
            {
                if (Listed(text, spell.Key))
                    pages.Add(spell);
            }
            return pages;
        }

        /// <summary>How many pages the book takes at most (one it already holds beyond that stays).</summary>
        public static int Room(ItemDrop.ItemData book) => CasterGearWorn.BookPiece(book)?.Pages ?? 0;

        public static bool Holds(ItemDrop.ItemData book, Spell spell) => book != null && spell != null && Listed(Text(book), spell.Key);

        /// <summary>The spell's page is in the book in the caster's left hand.</summary>
        public static bool Castable(Humanoid caster, Spell spell) => Holds(CasterGearWorn.Book(caster), spell);

        /// <summary>The book holds a page of this kind (the heal highlight asks often: nothing allocated).</summary>
        public static bool HasKind(ItemDrop.ItemData book, SpellKind kind)
        {
            if (book == null)
                return false;
            foreach (Spell spell in Spells.All)
            {
                if (spell.Kind == kind && Holds(book, spell))
                    return true;
            }
            return false;
        }

        public static PageAdd Add(ItemDrop.ItemData book, Spell spell)
        {
            List<Spell> pages = Of(book);
            if (pages.Contains(spell))
                return PageAdd.AlreadyIn;
            if (pages.Count >= Room(book))
                return PageAdd.Full;
            pages.Add(spell);
            Save(book, pages);
            return PageAdd.Added;
        }

        public static void Remove(ItemDrop.ItemData book, Spell spell)
        {
            List<Spell> pages = Of(book);
            if (pages.Remove(spell))
                Save(book, pages);
        }

        private static void Save(ItemDrop.ItemData book, List<Spell> pages)
        {
            var keys = new string[pages.Count];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = pages[i].Key;
            book.m_customData[Key] = string.Join(",", keys);
        }

        /// <summary>The key is one of the comma separated keys, found without splitting (the heal highlight asks often).</summary>
        private static bool Listed(string text, string key)
        {
            int at = text.IndexOf(key, StringComparison.Ordinal);
            while (at >= 0)
            {
                int end = at + key.Length;
                if ((at == 0 || text[at - 1] == ',') && (end == text.Length || text[end] == ','))
                    return true;
                at = text.IndexOf(key, end, StringComparison.Ordinal);
            }
            return false;
        }
    }
}
