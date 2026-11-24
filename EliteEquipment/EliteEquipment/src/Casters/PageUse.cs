using HarmonyLib;
using Translations;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Putting pages in a spellbook and taking them out (features/casters.md, "The spellbook"). Using a page item (from the
    /// inventory or the hotbar: the game consumes a Consumable through <c>Player.ConsumeItem</c>) while holding a book puts
    /// its spell in the book and uses the page up, instead of eating it; refused with a message when no book is held, or
    /// the book is full (at or over its room: "full (4 / 2 pages)") or already holds it, the page kept. With the switch off a page does nothing. Answering false keeps
    /// the game's eating animation and sound away. The Spellbook window's button takes a page out (<see cref="TakeOut"/>).
    /// Both on the player's own machine: the book and the pages are in their inventory.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.ConsumeItem))]
    public static class PageUse
    {
        [HarmonyPrefix]
        private static bool Prefix(Player __instance, Inventory inventory, ItemDrop.ItemData item, ref bool __result)
        {
            Spell spell = item?.m_dropPrefab != null ? Spells.ByItem(item.m_dropPrefab.name) : null;
            if (spell == null)
                return true;
            __result = false;
            if (CastersSettings.On)
                PutIn(__instance, inventory, item, spell);
            return false;
        }

        private static void PutIn(Player player, Inventory inventory, ItemDrop.ItemData page, Spell spell)
        {
            ItemDrop.ItemData book = CasterGearWorn.Book(player);
            string name = Lang.Get(spell.Token);
            if (book == null)
            {
                Say(player, Lang.Get("ee_page_no_book"));
                return;
            }
            PageAdd added = BookPages.Add(book, spell);
            if (added == PageAdd.Added)
            {
                inventory.RemoveOneItem(page);
                Say(player, Lang.Format("ee_page_added", name));
            }
            else if (added == PageAdd.AlreadyIn)
                Say(player, Lang.Format("ee_page_already", name));
            else
                Say(player, Lang.Format("ee_page_full", BookPages.Of(book).Count, BookPages.Room(book)));
        }

        /// <summary>The Spellbook's button: the held book's page goes into the inventory, refused when that has no room.</summary>
        public static void TakeOut(Player player, Spell spell)
        {
            ItemDrop.ItemData book = CasterGearWorn.Book(player);
            GameObject page = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(spell.Item) : null;
            if (book == null || page == null || !BookPages.Holds(book, spell))
                return;
            if (!player.GetInventory().AddItem(page, 1))
            {
                Say(player, Lang.Get("ee_page_inventory_full"));
                return;
            }
            BookPages.Remove(book, spell);
        }

        private static void Say(Player player, string text) => player.Message(MessageHud.MessageType.Center, text);
    }
}
