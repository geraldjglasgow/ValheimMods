using OpenKeep.Core;

namespace OpenKeep.Stow
{
    /// <summary>The "$ok_stow..." words of the module. <see cref="Register"/> hands them to the game's localization;
    /// <see cref="Format"/> localizes a word with placeholders and fills them.</summary>
    public static class StowWords
    {
        public const string QuickStack = "$ok_stow_quickstack";
        public const string StoreAll = "$ok_stow_storeall";
        public const string TopUp = "$ok_stow_topup";
        public const string Sort = "$ok_stow_sort";
        public const string Trash = "$ok_stow_trash";
        public const string Edit = "$ok_stow_edit";
        public const string Off = "$ok_stow_off";
        public const string Moved = "$ok_stow_moved";
        public const string MovedTo = "$ok_stow_moved_to";
        public const string Nothing = "$ok_stow_nothing";
        public const string NoContainer = "$ok_stow_nocontainer";
        public const string NearbyOff = "$ok_stow_nearby_off";
        public const string NoRoute = "$ok_stow_noroute";
        public const string Routed = "$ok_stow_routed";
        public const string StoredOne = "$ok_stow_storedone";
        public const string ToppedUp = "$ok_stow_toppedup";
        public const string ToppedUpFrom = "$ok_stow_toppedup_from";
        public const string Sorted = "$ok_stow_sorted";
        public const string FavouriteOn = "$ok_stow_favourite_on";
        public const string FavouriteOff = "$ok_stow_favourite_off";
        public const string SlotOn = "$ok_stow_slot_on";
        public const string SlotOff = "$ok_stow_slot_off";
        public const string JunkOn = "$ok_stow_junk_on";
        public const string JunkOff = "$ok_stow_junk_off";
        public const string FavouriteKept = "$ok_stow_favourite_kept";
        public const string EquippedKept = "$ok_stow_equipped_kept";
        public const string TrashAsk = "$ok_stow_trash_ask";
        public const string DestroyJunkAsk = "$ok_stow_destroy_junk_ask";
        public const string Trashed = "$ok_stow_trashed";
        public const string NoJunk = "$ok_stow_nojunk";
        public const string DragHint = "$ok_stow_drag_hint";
        public const string Found = "$ok_stow_found";
        public const string NotFound = "$ok_stow_notfound";
        public const string NoCycle = "$ok_stow_nocycle";
        public const string Salvaged = "$ok_stow_salvaged";

        public static void Register()
        {
            Language.Add("ok_stow_quickstack", "Quick stack");
            Language.Add("ok_stow_storeall", "Store all");
            Language.Add("ok_stow_topup", "Top up");
            Language.Add("ok_stow_sort", "Sort");
            Language.Add("ok_stow_trash", "Trash");
            Language.Add("ok_stow_edit", "Edit stow rules");
            Language.Add("ok_stow_off", "OpenKeep stow is disabled");
            Language.Add("ok_stow_moved", "Moved {0} stacks");
            Language.Add("ok_stow_moved_to", "Moved {0} stacks to {1}");
            Language.Add("ok_stow_nothing", "Nothing to move");
            Language.Add("ok_stow_nocontainer", "No container is open");
            Language.Add("ok_stow_nearby_off", "Quick stacking to nearby containers is disabled");
            Language.Add("ok_stow_noroute", "No nearby container takes {0}");
            Language.Add("ok_stow_routed", "Sent {0} to {1}");
            Language.Add("ok_stow_storedone", "Stored one {0}");
            Language.Add("ok_stow_toppedup", "Topped up {0} items");
            Language.Add("ok_stow_toppedup_from", "Topped up {0} items from {1}");
            Language.Add("ok_stow_sorted", "Sorted {0} stacks");
            RegisterMarks();
            RegisterTrash();
        }

        private static void RegisterMarks()
        {
            Language.Add("ok_stow_favourite_on", "{0} is now a favourite");
            Language.Add("ok_stow_favourite_off", "{0} is no longer a favourite");
            Language.Add("ok_stow_slot_on", "Slot {0} is now a favourite");
            Language.Add("ok_stow_slot_off", "Slot {0} is no longer a favourite");
            Language.Add("ok_stow_junk_on", "{0} is marked as junk");
            Language.Add("ok_stow_junk_off", "{0} is no longer marked as junk");
            Language.Add("ok_stow_favourite_kept", "{0} is a favourite and stays");
            Language.Add("ok_stow_equipped_kept", "{0} is equipped and stays");
            Language.Add("ok_stow_found", "{0} in {1} nearby containers");
            Language.Add("ok_stow_notfound", "No nearby container holds {0}");
            Language.Add("ok_stow_nocycle", "No other container within reach");
        }

        private static void RegisterTrash()
        {
            Language.Add("ok_stow_trash_ask", "Destroy {0}?");
            Language.Add("ok_stow_destroy_junk_ask", "Destroy {0} junk stacks?");
            Language.Add("ok_stow_trashed", "Destroyed {0}");
            Language.Add("ok_stow_nojunk", "Nothing in the inventory is marked as junk");
            Language.Add("ok_stow_drag_hint", "Drag a stack onto the trash can to destroy it");
            Language.Add("ok_stow_salvaged", "Salvaged {0}");
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
