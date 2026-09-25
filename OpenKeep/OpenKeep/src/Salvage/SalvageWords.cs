using OpenKeep.Core;

namespace OpenKeep.Salvage
{
    /// <summary>The "$ok_salvage..." words of the module. <see cref="Register"/> hands them to the game's localization.</summary>
    public static class SalvageWords
    {
        public const string Salvage = "$ok_salvage";
        public const string Edit = "$ok_salvage_edit";
        public const string Ask = "$ok_salvage_ask";
        public const string Empty = "$ok_salvage_empty";
        public const string Nothing = "$ok_salvage_nothing";
        public const string NoFit = "$ok_salvage_nofit";
        public const string Off = "$ok_salvage_off";
        public const string Cannot = "$ok_salvage_cannot";
        public const string NoRecipe = "$ok_salvage_norecipe";
        public const string Favourite = "$ok_salvage_favourite";
        public const string Excluded = "$ok_salvage_excluded";
        public const string ModData = "$ok_salvage_moddata";
        public const string Equipped = "$ok_salvage_equipped";
        public const string Unknown = "$ok_salvage_unknown";
        public const string NoStation = "$ok_salvage_nostation";
        public const string Done = "$ok_salvage_done";

        public static void Register()
        {
            Language.Add("ok_salvage", "Salvage");
            Language.Add("ok_salvage_edit", "Edit salvage rules");
            Language.Add("ok_salvage_ask", "Salvage this stack? It returns:");
            Language.Add("ok_salvage_empty", "Nothing in the inventory can be salvaged.");
            Language.Add("ok_salvage_nothing", "Salvaging this would return nothing");
            Language.Add("ok_salvage_nofit", "Not enough room for the salvaged materials");
            Language.Add("ok_salvage_off", "Salvaging is disabled");
            Language.Add("ok_salvage_cannot", "This item cannot be salvaged");
            Language.Add("ok_salvage_norecipe", "This item has no recipe to salvage");
            Language.Add("ok_salvage_favourite", "Favourite items are not salvaged");
            Language.Add("ok_salvage_excluded", "This item is excluded from salvaging");
            Language.Add("ok_salvage_moddata", "Another mod keeps data on this item; it is not salvaged");
            Language.Add("ok_salvage_equipped", "Unequip the item before salvaging it");
            Language.Add("ok_salvage_unknown", "You do not know this recipe yet");
            Language.Add("ok_salvage_nostation", "The recipe's crafting station is not in range");
            Language.Add("ok_salvage_done", "Salvaged");
        }
    }
}
