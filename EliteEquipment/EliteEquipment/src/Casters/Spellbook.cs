using System.Collections.Generic;
using Translations;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The Spellbook window (features/casters.md, "The spellbook"): anywhere, with the inventory open and a spellbook in
    /// the left hand (a wand is not needed), the Spellbook button opens it in the crafting panel's place, the crafting
    /// panel hidden until it closes (<see cref="SpellbookCover"/>). It lists the pages in the held book; a click picks
    /// one, shown on the right with a button that takes it out into the inventory (<see cref="PageUse.TakeOut"/>). The
    /// spells are chosen with a wand on the spell ring (<see cref="SpellRing"/>), not here (user, 2026-11-02). The window
    /// closes with its button, with the inventory, and when the book leaves the hand or the switch goes off. Refreshed
    /// only when the book, its pages, a pick or the language change; the rows are made again only when the pages change.
    /// </summary>
    public static class Spellbook
    {
        private static bool open;
        private static Spell picked;
        private static bool dirty = true;
        private static ItemDrop.ItemData shownBook;
        private static string shownPages;
        private static int shownWords = -1;

        public static void Tick()
        {
            Player player = Player.m_localPlayer;
            bool shown = CastersSettings.On && InventoryGui.IsVisible();
            ItemDrop.ItemData book = shown ? CasterGearWorn.Book(player) : null;
            if (book == null)
            {
                Hide();
                return;
            }
            if (!SpellbookUi.Ensure(Toggle, PickPage, TakeOut))
                return;
            if (!SpellbookUi.OpenButton.gameObject.activeSelf)
                SpellbookUi.OpenButton.gameObject.SetActive(true);
            SpellbookUi.Relabel();
            if (open && !SpellbookUi.Window.gameObject.activeSelf)
                Close();
            if (!open)
                return;
            SpellbookLayout.Fit(SpellbookUi.Window, SpellbookUi.Gui.m_crafting);
            if (Changed(book))
                Refresh(player, book);
        }

        private static void Hide()
        {
            if (open)
                Close();
            if (SpellbookUi.OpenButton != null && SpellbookUi.OpenButton.gameObject.activeSelf)
                SpellbookUi.OpenButton.gameObject.SetActive(false);
        }

        private static void Close()
        {
            open = false;
            SpellbookCover.Restore();
            if (SpellbookUi.Window != null)
                SpellbookUi.Window.gameObject.SetActive(false);
        }

        private static void Toggle()
        {
            if (open)
            {
                Close();
                return;
            }
            (open, dirty) = (true, true);
            SpellbookLayout.Fit(SpellbookUi.Window, SpellbookUi.Gui.m_crafting);
            SpellbookUi.Window.gameObject.SetActive(true);
            SpellbookCover.Cover(SpellbookUi.Gui.m_crafting);
        }

        /// <summary>A page clicked: shown on the right.</summary>
        private static void PickPage(Spell spell)
        {
            picked = spell;
            dirty = true;
        }

        private static void TakeOut()
        {
            if (picked != null)
                PageUse.TakeOut(Player.m_localPlayer, picked);
            dirty = true;
        }

        private static bool Changed(ItemDrop.ItemData book)
        {
            string pages = BookPages.Text(book);
            if (!dirty && book == shownBook && pages == shownPages && Lang.Version == shownWords)
                return false;
            if (pages != shownPages)
                SpellbookPages.Clear();
            (dirty, shownBook, shownPages, shownWords) = (false, book, pages, Lang.Version);
            return true;
        }

        private static void Refresh(Player player, ItemDrop.ItemData book)
        {
            List<Spell> pages = BookPages.Of(book);
            if (picked == null || !pages.Contains(picked))
                picked = pages.Count > 0 ? pages[0] : null;
            SpellbookUi.ShowHeader(book, pages.Count);
            SpellbookPages.Show(pages, picked, player, pages.Count == 0 ? Lang.Get("ee_spellbook_empty") : null);
            SpellbookDetail.Show(picked, player);
        }
    }
}
