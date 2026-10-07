using OpenKeep.Core;

namespace OpenKeep.Store
{
    /// <summary>The "$ok_store..." words of the module. <see cref="Register"/> hands them to the game's localization;
    /// <see cref="Format"/> localizes a word with placeholders and fills them.</summary>
    public static class StoreWords
    {
        public const string QuickStack = "$ok_store_quickstack";
        public const string StoreAll = "$ok_store_storeall";
        public const string TopUp = "$ok_store_topup";
        public const string Sort = "$ok_store_sort";
        public const string Trash = "$ok_store_trash";
        public const string Edit = "$ok_store_edit";
        public const string Off = "$ok_store_off";
        public const string Moved = "$ok_store_moved";
        public const string MovedTo = "$ok_store_moved_to";
        public const string Nothing = "$ok_store_nothing";
        public const string NoContainer = "$ok_store_nocontainer";
        public const string NearbyOff = "$ok_store_nearby_off";
        public const string NoRoute = "$ok_store_noroute";
        public const string Routed = "$ok_store_routed";
        public const string RoutedMore = "$ok_store_routed_more";
        public const string StoredOne = "$ok_store_storedone";
        public const string ToppedUp = "$ok_store_toppedup";
        public const string ToppedUpFrom = "$ok_store_toppedup_from";
        public const string Sorted = "$ok_store_sorted";
        public const string FavouriteOn = "$ok_store_favourite_on";
        public const string FavouriteOff = "$ok_store_favourite_off";
        public const string SlotOn = "$ok_store_slot_on";
        public const string SlotOff = "$ok_store_slot_off";
        public const string JunkOn = "$ok_store_junk_on";
        public const string JunkOff = "$ok_store_junk_off";
        public const string FavouriteKept = "$ok_store_favourite_kept";
        public const string EquippedKept = "$ok_store_equipped_kept";
        public const string TrashAsk = "$ok_store_trash_ask";
        public const string DestroyJunkAsk = "$ok_store_destroy_junk_ask";
        public const string Trashed = "$ok_store_trashed";
        public const string NoJunk = "$ok_store_nojunk";
        public const string DragHint = "$ok_store_drag_hint";
        public const string Found = "$ok_store_found";
        public const string NotFound = "$ok_store_notfound";
        public const string NoCycle = "$ok_store_nocycle";
        public const string Salvaged = "$ok_store_salvaged";
        public const string TrashModeOn = "$ok_store_trash_mode";

        public static void Register()
        {
            Language.Add("ok_store_quickstack", "Quick stack");
            Language.Add("ok_store_storeall", "Store all");
            Language.Add("ok_store_topup", "Top up");
            Language.Add("ok_store_sort", "Sort");
            Language.Add("ok_store_trash", "Trash");
            Language.Add("ok_store_edit", "Edit store rules");
            Language.Add("ok_store_off", "OpenKeep store is disabled");
            Language.Add("ok_store_moved", "Moved {0} stacks");
            Language.Add("ok_store_moved_to", "Moved {0} stacks to {1}");
            Language.Add("ok_store_nothing", "Nothing to move");
            Language.Add("ok_store_nocontainer", "No container is open");
            Language.Add("ok_store_nearby_off", "Quick stacking to nearby containers is disabled");
            Language.Add("ok_store_noroute", "No nearby container takes {0}");
            Language.Add("ok_store_routed", "Sent {0} to {1}");
            Language.Add("ok_store_routed_more", "Sent {0} to {1} and {2} more");
            Language.Add("ok_store_storedone", "Stored one {0}");
            Language.Add("ok_store_toppedup", "Topped up {0} items");
            Language.Add("ok_store_toppedup_from", "Topped up {0} items from {1}");
            Language.Add("ok_store_sorted", "Sorted {0} stacks");
            RegisterMarks();
            RegisterTrash();
        }

        private static void RegisterMarks()
        {
            Language.Add("ok_store_favourite_on", "{0} is now a favourite");
            Language.Add("ok_store_favourite_off", "{0} is no longer a favourite");
            Language.Add("ok_store_slot_on", "Slot {0} is now a favourite");
            Language.Add("ok_store_slot_off", "Slot {0} is no longer a favourite");
            Language.Add("ok_store_junk_on", "{0} is marked as junk");
            Language.Add("ok_store_junk_off", "{0} is no longer marked as junk");
            Language.Add("ok_store_favourite_kept", "{0} is a favourite and stays");
            Language.Add("ok_store_equipped_kept", "{0} is equipped and stays");
            Language.Add("ok_store_found", "{0} in {1} nearby containers");
            Language.Add("ok_store_notfound", "No nearby container holds {0}");
            Language.Add("ok_store_nocycle", "No other container within reach");
        }

        private static void RegisterTrash()
        {
            Language.Add("ok_store_trash_ask", "Destroy {0}?");
            Language.Add("ok_store_destroy_junk_ask", "Destroy {0} junk stacks?");
            Language.Add("ok_store_trashed", "Destroyed {0}");
            Language.Add("ok_store_nojunk", "Nothing in the inventory is marked as junk");
            Language.Add("ok_store_drag_hint", "Drag a stack onto the trash can to destroy it, or Shift + click the can to click stacks away");
            Language.Add("ok_store_salvaged", "Salvaged {0}");
            Language.Add("ok_store_trash_mode", "Trash mode: click a stack to destroy it, let go of Shift to stop");
        }

        /// <summary>Localizes a word and fills its {0}, {1} placeholders.</summary>
        public static string Format(string word, params object[] args)
        {
            string text = Language.Localize(word);
            try
            {
                return string.Format(text, args);
            }
            catch (System.FormatException)
            {
                return text;
            }
        }
    }
}
