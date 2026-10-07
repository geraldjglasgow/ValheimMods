using System.Collections.Generic;
using OpenKeep.Core;

namespace OpenKeep.Store
{
    /// <summary>
    /// Favourite item names, favourite slots of the player inventory and junk marks, stored per character in the
    /// player's custom data (<c>OpenKeep.favouriteItems</c>, <c>OpenKeep.favouriteSlots</c>, <c>OpenKeep.junk</c>).
    /// Items are keyed by prefab name; slots as <c>x:y</c> (the sets are comma separated, so the comma of the spec's
    /// <c>x,y</c> cannot be used). The sets are cached until they change or the local player changes; the slots are also
    /// kept as packed numbers, so the marks drawn on the grid look them up without building a string per cell.
    /// <see cref="Revision"/> changes with every toggle and with the local player.
    /// </summary>
    public static class Favourites
    {
        private const string ItemsKey = "favouriteItems";
        private const string SlotsKey = "favouriteSlots";
        private const string JunkKey = "junk";

        private static Player cachedFor;
        private static HashSet<string> items;
        private static HashSet<string> slots;
        private static HashSet<string> junk;
        private static HashSet<int> slotKeys;
        private static int revision;

        /// <summary>Changes whenever a set changes or another local player's sets are read.</summary>
        public static int Revision
        {
            get
            {
                CheckPlayer();
                return revision;
            }
        }

        public static bool IsFavouriteItem(ItemDrop.ItemData item) => item != null && Set(ref items, ItemsKey).Contains(Id(item));

        public static bool IsJunk(ItemDrop.ItemData item) => item != null && Set(ref junk, JunkKey).Contains(Id(item));

        public static bool IsFavouriteSlot(Vector2i pos) => SlotKeys().Contains(Pack(pos.x, pos.y));

        /// <summary>Toggles the item name; true when it is a favourite afterwards.</summary>
        public static bool ToggleItem(ItemDrop.ItemData item) => Toggle(ref items, ItemsKey, Id(item));

        public static bool ToggleSlot(Vector2i pos)
        {
            slotKeys = null;
            return Toggle(ref slots, SlotsKey, SlotId(pos));
        }

        public static bool ToggleJunk(ItemDrop.ItemData item) => Toggle(ref junk, JunkKey, Id(item));

        public static void ToggleItemWithMessage(ItemDrop.ItemData item)
        {
            bool on = ToggleItem(item);
            Messages.Center(StoreWords.Format(on ? StoreWords.FavouriteOn : StoreWords.FavouriteOff, ItemNames.DisplayName(item)));
        }

        public static void ToggleSlotWithMessage(Vector2i pos)
        {
            bool on = ToggleSlot(pos);
            Messages.Center(StoreWords.Format(on ? StoreWords.SlotOn : StoreWords.SlotOff, (pos.x + 1) + "," + (pos.y + 1)));
        }

        public static void ToggleJunkWithMessage(ItemDrop.ItemData item)
        {
            bool on = ToggleJunk(item);
            Messages.Center(StoreWords.Format(on ? StoreWords.JunkOn : StoreWords.JunkOff, ItemNames.DisplayName(item)));
        }

        private static string Id(ItemDrop.ItemData item) => ItemNames.PrefabName(item);

        private static string SlotId(Vector2i pos) => pos.x + ":" + pos.y;

        private static int Pack(int x, int y) => (x << 16) | (y & 0xFFFF);

        /// <summary>The favourite slots as packed numbers, parsed from their <c>x:y</c> ids when first asked for.</summary>
        private static HashSet<int> SlotKeys()
        {
            HashSet<string> ids = Set(ref slots, SlotsKey);
            if (slotKeys != null)
                return slotKeys;
            slotKeys = new HashSet<int>();
            foreach (string id in ids)
            {
                string[] parts = id.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                    slotKeys.Add(Pack(x, y));
            }
            return slotKeys;
        }

        private static bool Toggle(ref HashSet<string> set, string key, string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;
            HashSet<string> current = Set(ref set, key);
            bool on = !current.Remove(id);
            if (on)
                current.Add(id);
            CharacterData.SetSet(key, current);
            revision++;
            return on;
        }

        private static HashSet<string> Set(ref HashSet<string> set, string key)
        {
            CheckPlayer();
            return set ?? (set = CharacterData.GetSet(key));
        }

        private static void CheckPlayer()
        {
            if (cachedFor == Player.m_localPlayer)
                return;
            items = slots = junk = null;
            slotKeys = null;
            cachedFor = Player.m_localPlayer;
            revision++;
        }
    }
}
