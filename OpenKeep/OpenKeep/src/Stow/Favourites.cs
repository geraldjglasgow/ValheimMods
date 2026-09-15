using System.Collections.Generic;
using OpenKeep.Core;

namespace OpenKeep.Stow
{
    /// <summary>
    /// Favourite item names, favourite slots of the player inventory and junk marks, stored per character in the
    /// player's custom data (<c>OpenKeep.favouriteItems</c>, <c>OpenKeep.favouriteSlots</c>, <c>OpenKeep.junk</c>).
    /// Items are keyed by prefab name; slots as <c>x:y</c> (the sets are comma separated, so the comma of the spec's
    /// <c>x,y</c> cannot be used). The sets are cached until they change or the local player changes.
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

        public static bool IsFavouriteItem(ItemDrop.ItemData item) => item != null && Set(ref items, ItemsKey).Contains(Id(item));

        public static bool IsJunk(ItemDrop.ItemData item) => item != null && Set(ref junk, JunkKey).Contains(Id(item));

        public static bool IsFavouriteSlot(Vector2i pos) => Set(ref slots, SlotsKey).Contains(SlotId(pos));

        /// <summary>Toggles the item name; true when it is a favourite afterwards.</summary>
        public static bool ToggleItem(ItemDrop.ItemData item) => Toggle(ref items, ItemsKey, Id(item));

        public static bool ToggleSlot(Vector2i pos) => Toggle(ref slots, SlotsKey, SlotId(pos));

        public static bool ToggleJunk(ItemDrop.ItemData item) => Toggle(ref junk, JunkKey, Id(item));

        public static void ToggleItemWithMessage(ItemDrop.ItemData item)
        {
            bool on = ToggleItem(item);
            Messages.Center(StowWords.Format(on ? StowWords.FavouriteOn : StowWords.FavouriteOff, ItemNames.DisplayName(item)));
        }

        public static void ToggleSlotWithMessage(Vector2i pos)
        {
            bool on = ToggleSlot(pos);
            Messages.Center(StowWords.Format(on ? StowWords.SlotOn : StowWords.SlotOff, (pos.x + 1) + "," + (pos.y + 1)));
        }

        public static void ToggleJunkWithMessage(ItemDrop.ItemData item)
        {
            bool on = ToggleJunk(item);
            Messages.Center(StowWords.Format(on ? StowWords.JunkOn : StowWords.JunkOff, ItemNames.DisplayName(item)));
        }

        private static string Id(ItemDrop.ItemData item) => ItemNames.PrefabName(item);

        private static string SlotId(Vector2i pos) => pos.x + ":" + pos.y;

        private static bool Toggle(ref HashSet<string> set, string key, string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;
            HashSet<string> current = Set(ref set, key);
            bool on = !current.Remove(id);
            if (on)
                current.Add(id);
            CharacterData.SetSet(key, current);
            return on;
        }

        private static HashSet<string> Set(ref HashSet<string> set, string key)
        {
            if (cachedFor != Player.m_localPlayer)
            {
                items = slots = junk = null;
                cachedFor = Player.m_localPlayer;
            }
            return set ?? (set = CharacterData.GetSet(key));
        }
    }
}
