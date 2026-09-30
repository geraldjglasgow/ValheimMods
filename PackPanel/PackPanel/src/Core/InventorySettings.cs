using BepInEx.Configuration;
using PackPanel.Backpacks;
using PackPanel.Ring;
using SyncedConfig;

namespace PackPanel.Core
{
    public enum InventoryTheme { Brown, Timber }

    /// <summary>
    /// Sections "1. Inventory" (the master switch, the grid, carry weight, death), "2. Slots" (every slot group) and
    /// "5. Look" (each player's own display keys); the key ring binds "3. Key Ring" (<see cref="KeyRingSettings"/>), the
    /// backpacks "4. Backpacks" (<see cref="BackpackSettings"/>). The grid's size, every slot count and the carry weight
    /// change what a player can carry, so they are synced and lockable; the labels and the look are each player's own.
    /// Values are read at use time; a change of the layout keys re-lays the inventory out at once (<see cref="InventoryModule"/>).
    /// </summary>
    public static class InventorySettings
    {
        public const string Section = "1. Inventory";
        public const string SlotsSection = "2. Slots";
        public const string LookSection = "5. Look";
        public const int GameWidth = 8;
        public const int GameRows = 4;
        public const float GameCarryWeight = 300f;
        public const int MaxWidth = 12;
        public const int MaxRows = 10;
        public const int MaxGroup = 5;

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<int> InventoryWidth { get; private set; }
        public static ConfigEntry<int> InventoryRows { get; private set; }
        public static ConfigEntry<bool> EquipmentSlots { get; private set; }
        public static ConfigEntry<int> SlotsPerGroup { get; private set; }
        public static ConfigEntry<int> UtilitySlots { get; private set; }
        public static ConfigEntry<bool> TrinketSlot { get; private set; }
        public static ConfigEntry<bool> BackpackSlot { get; private set; }
        public static ConfigEntry<string> BackpackItems { get; private set; }
        public static ConfigEntry<int> FoodSlots { get; private set; }
        public static ConfigEntry<bool> FoodSlotsFollowEating { get; private set; }
        public static ConfigEntry<int> MeadSlots { get; private set; }
        public static ConfigEntry<int> AmmoSlots { get; private set; }
        public static ConfigEntry<bool> CoinPurse { get; private set; }
        public static ConfigEntry<bool> KeepSlotsOnDeath { get; private set; }
        public static ConfigEntry<float> BaseCarryWeight { get; private set; }
        public static ConfigEntry<bool> SlotLabels { get; private set; }
        public static ConfigEntry<bool> BrownStyle { get; private set; }
        public static ConfigEntry<InventoryTheme> PanelTheme { get; private set; }
        public static ConfigEntry<float> FrameWidth { get; private set; }
        public static ConfigEntry<float> FrameJaggedness { get; private set; }
        public static ConfigEntry<bool> WeightUnderMinimap { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            BindGrid(synced);
            BindWorn(synced);
            BackpackSettings.Bind(synced);
            BindGroups(synced);
            BindCarried(synced);
            Consume.ConsumeSettings.Bind(synced);
            KeyRingSettings.Bind(synced);
            Tackle.TackleboxSettings.Bind(synced);
            BindDisplay(synced);
            BindTheme(synced);
        }

        private static void BindGrid(SyncedConfiguration synced)
        {
            Enabled = synced.Bind(Section, "Enabled", true,
                "Master switch for PackPanel: the bigger grid, the labelled slots, the coin purse, the key ring, the backpacks and the brown look. Off: the game's own inventory, its 8 x 4 grid (plus rows bought from the trader) and its wood look; every other PackPanel setting is ignored. Items in the slots move into the grid, what does not fit is dropped at your feet.");
            InventoryWidth = synced.Bind(Section, "Inventory Width", 8,
                "Columns of the inventory grid. The game has 8; the hotbar keys stay 1 to 8. Warning: items in columns beyond 8 are lost if PackPanel is removed, so empty them first.",
                acceptableValues: new AcceptableValueRange<int>(GameWidth, MaxWidth));
            InventoryRows = synced.Bind(Section, "Inventory Rows", 5,
                "Rows of the inventory grid. The game has 4; rows bought from the trader come on top of this. Items in rows beyond the game's are dropped at your feet if PackPanel is removed.",
                acceptableValues: new AcceptableValueRange<int>(GameRows, MaxRows));
            BaseCarryWeight = synced.Bind(Section, "Base Carry Weight", GameCarryWeight,
                "How much a player carries before being over-encumbered, before Megingjord and other effects add to it. The game has 300. The world's carry weight modifier still scales it.",
                acceptableValues: new AcceptableValueRange<float>(50f, 10000f));
            KeepSlotsOnDeath = synced.Bind(Section, "Keep Slots On Death", false,
                "Items in the gear, backpack, utility, trinket, food, mead and ammo slots stay with you when you die instead of going into your grave; the armour, utilities and trinket you wore are worn again when you wake. The coin purse, the key ring and the grid follow the game's rules.");
        }

        private static void BindWorn(SyncedConfiguration synced)
        {
            EquipmentSlots = synced.Bind(SlotsSection, "Equipment Slots", true,
                "Head, Chest, Legs and Back slots. Armour you wear sits in its slot; drop a piece on its slot to wear it, drag it out to take it off.");
            UtilitySlots = synced.Bind(SlotsSection, "Utility Slots", 3,
                "Utility slots (belts, the wishbone, the wisplight...) when Slots Per Group is 0. Every utility item in a slot is worn at once, so up to this many work together. 0: none, the game's single utility item.",
                acceptableValues: new AcceptableValueRange<int>(0, MaxGroup));
            TrinketSlot = synced.Bind(SlotsSection, "Trinket Slot", true,
                "A Trinket slot under the utilities. The trinket in it is the one you wear (the game wears one at a time); drop a trinket on it to wear it, drag it out to take it off.");
            BackpackSlot = synced.Bind(SlotsSection, "Backpack Slot", true,
                "A Backpack slot. PackPanel's backpacks are worn in it (see 4. Backpacks); it also holds a backpack from another mod (any item whose prefab name contains \"backpack\", or one listed in Backpack Items), but does not equip those.");
            BackpackItems = synced.Bind(SlotsSection, "Backpack Items", "",
                "More prefab names the Backpack slot accepts, comma separated.");
        }

        private static void BindGroups(SyncedConfiguration synced)
        {
            SlotsPerGroup = synced.Bind(SlotsSection, "Slots Per Group", 0,
                "One number for all four groups: 1 to 5 gives Utility, Food, Mead and Ammo this many slots each, whatever Utility Slots, Food Slots, Food Slots Follow Eating, Mead Slots and Ammo Slots say. 0: each group uses its own setting.",
                acceptableValues: new AcceptableValueRange<int>(0, MaxGroup));
        }

        private static void BindCarried(SyncedConfiguration synced)
        {
            FoodSlots = synced.Bind(SlotsSection, "Food Slots", 3, "Food slots when Food Slots Follow Eating is off and Slots Per Group is 0.",
                acceptableValues: new AcceptableValueRange<int>(0, MaxGroup));
            FoodSlotsFollowEating = synced.Bind(SlotsSection, "Food Slots Follow Eating", true,
                "As many food slots as foods a player can eat at once: FeastMaster's Food Slots when it is installed, the game's 3 otherwise (at most 5). Slots Per Group above 0 overrides it.");
            MeadSlots = synced.Bind(SlotsSection, "Mead Slots", 3, "Slots for meads and other drinks and potions, when Slots Per Group is 0.",
                acceptableValues: new AcceptableValueRange<int>(0, MaxGroup));
            AmmoSlots = synced.Bind(SlotsSection, "Ammo Slots", 3, "Slots for arrows, bolts, missiles and bait, when Slots Per Group is 0.",
                acceptableValues: new AcceptableValueRange<int>(0, MaxGroup));
            CoinPurse = synced.Bind(SlotsSection, "Coin Purse", true,
                "A purse slot at the bottom of the slot panel: coins you pick up go into it, traders take coins from it as from anywhere in the inventory.");
        }

        private static void BindDisplay(SyncedConfiguration synced)
        {
            SlotLabels = synced.Bind(LookSection, "Slot Labels", true, "The name of each slot on the slot.", synced: false);
            BrownStyle = synced.Bind(LookSection, "Brown Style", true,
                "The brown framed look of the inventory panels, their cells and the slot panel. Off: the game's wood.", synced: false);
            WeightUnderMinimap = synced.Bind(LookSection, "Weight Under Minimap", true,
                "Armor and carry weight in boxes in a column right of the minimap, followed by Elite Creatures Reborn's world tier when it is installed. The minimap and the status effects move a little left to make room.", synced: false);
        }

        private static void BindTheme(SyncedConfiguration synced)
        {
            PanelTheme = synced.Bind(LookSection, "Panel Theme", InventoryTheme.Timber,
                "Timber: shared warm wallpaper with softly chopped, beveled wood edges across inventory, keyring, crafting, character dialogs, settings and native wood menus. Brown: the original brown inventory artwork. Brown Style off restores the game's panels. Cosmetic, local to this player.", synced: false);
            FrameWidth = synced.Bind(LookSection, "Timber Border Width", 5.5f,
                "Custom bevel width in UI units. Live cosmetic tuning; no restart after this feature is installed.",
                acceptableValues: new AcceptableValueRange<float>(3f, 8f), synced: false);
            FrameJaggedness = synced.Bind(LookSection, "Timber Border Jaggedness", 1.5f,
                "Depth of broad irregular cuts along the wood silhouette, in UI units. Updates live.",
                acceptableValues: new AcceptableValueRange<float>(0f, 2.5f), synced: false);
        }
    }
}
