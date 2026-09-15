using BepInEx.Configuration;
using OpenKeep.Core;
using SyncedConfig;
using UnityEngine;

namespace OpenKeep.Stow
{
    /// <summary>
    /// Section "2. Stow". Ranges, the nearby switch and the ground pickup rules change gameplay and are synced and
    /// lockable; hotkeys, sort orders, confirmations and the overlay are per player. Values are read at use time.
    /// </summary>
    public static class StowSettings
    {
        public const string Section = "2. Stow";

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<KeyboardShortcut> QuickStackKey { get; private set; }
        public static ConfigEntry<bool> QuickStackNearby { get; private set; }
        public static ConfigEntry<KeyboardShortcut> StowAllKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> TopUpKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> SortInventoryKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> SortContainerKey { get; private set; }
        public static ConfigEntry<SortOrder> SortOrder { get; private set; }
        public static ConfigEntry<bool> AutoSortContainers { get; private set; }
        public static ConfigEntry<bool> AutoSortInventory { get; private set; }
        public static ConfigEntry<KeyboardShortcut> FavouriteItemKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> FavouriteSlotKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> JunkKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> TrashKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> DestroyJunkKey { get; private set; }
        public static ConfigEntry<bool> ConfirmTrash { get; private set; }
        public static ConfigEntry<bool> TrashUsesSalvage { get; private set; }
        public static ConfigEntry<KeyboardShortcut> RouteModifier { get; private set; }
        public static ConfigEntry<KeyboardShortcut> StoreOneKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> FindKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> DumpKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> CyclePreviousKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> CycleNextKey { get; private set; }
        public static ConfigEntry<bool> CycleWithWheel { get; private set; }
        public static ConfigEntry<float> NearbyRange { get; private set; }
        public static ConfigEntry<bool> ShowFavourites { get; private set; }
        public static ConfigEntry<bool> GroundPickup { get; private set; }
        public static ConfigEntry<float> PickupRange { get; private set; }
        public static ConfigEntry<float> PickupInterval { get; private set; }
        public static ConfigEntry<float> PickupDelay { get; private set; }
        public static ConfigEntry<bool> PickupOnlyHeldItems { get; private set; }
        public static ConfigEntry<int> ButtonRowOffset { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            BindMoves(synced);
            BindSorting(synced);
            BindFavourites(synced);
            BindTrash(synced);
            BindClicks(synced);
            BindCycling(synced);
            BindPickup(synced);
        }

        private static void BindMoves(SyncedConfiguration synced)
        {
            Enabled = synced.Bind(Section, "Enabled", true,
                "Master switch of the Stow module: panel buttons, hotkeys, sorting, trashing, routing, chest cycling and ground pickup.");
            QuickStackKey = synced.Bind(Section, "Quick Stack Key", new KeyboardShortcut(KeyCode.Q),
                "Inventory open: every non-favourite stack whose item already exists in the open container (or, with no container open, in any nearby container) moves there.", synced: false);
            QuickStackNearby = synced.Bind(Section, "Quick Stack Nearby", true,
                "Quick stack also reaches every nearby container, not only the open one. Off also disables the Dump key.");
            StowAllKey = synced.Bind(Section, "Stow All Key", RenamedKeys.Carry(synced.Config, Section, "Store All Key", "Stow All Key", new KeyboardShortcut(KeyCode.G)),
                "Inventory open with a container: every non-favourite item moves into the open container as far as it fits.", synced: false);
            TopUpKey = synced.Bind(Section, "Top Up Key", RenamedKeys.Carry(synced.Config, Section, "Restock Key", "Top Up Key", new KeyboardShortcut(KeyCode.R)),
                "Inventory open: every stack in the inventory that is not full is topped up from the open container or nearby containers. Favourite items included.", synced: false);
            NearbyRange = synced.Bind(Section, "Nearby Range", 20f,
                "Metres from the player for quick stack, top up, dump, route, find and cycle.",
                acceptableValues: new AcceptableValueRange<float>(1f, 100f));
            ButtonRowOffset = synced.Bind(Section, "Button Row Offset", 0,
                "Moves the Stow button row up (positive) or down (negative) by this many pixels from its default place under the container panel.", synced: false);
        }

        private static void BindSorting(SyncedConfiguration synced)
        {
            SortInventoryKey = synced.Bind(Section, "Sort Inventory Key", new KeyboardShortcut(KeyCode.T),
                "Inventory open: sorts the player inventory by Sort Order. The hotbar, favourite slots and equipped items stay where they are.", synced: false);
            SortContainerKey = synced.Bind(Section, "Sort Container Key", new KeyboardShortcut(KeyCode.Y),
                "Inventory open with a container: sorts the open container by Sort Order.", synced: false);
            SortOrder = synced.Bind(Section, "Sort Order", Stow.SortOrder.Category,
                "Category (item type, then name), Name, Weight (heaviest first) or Value (most valuable first). Stacks of the same item merge while sorting.", synced: false);
            AutoSortContainers = synced.Bind(Section, "Auto Sort Containers", false,
                "A container is sorted when it is opened.", synced: false);
            AutoSortInventory = synced.Bind(Section, "Auto Sort Inventory", false,
                "The player inventory is sorted when the panel opens.", synced: false);
        }

        private static void BindFavourites(SyncedConfiguration synced)
        {
            FavouriteItemKey = synced.Bind(Section, "Favourite Item Key", new KeyboardShortcut(KeyCode.F),
                "Hovering an item: the item name becomes a favourite (never quick stacked, stored, dumped, trashed or salvaged). Press again to remove.", synced: false);
            FavouriteSlotKey = synced.Bind(Section, "Favourite Slot Key", new KeyboardShortcut(KeyCode.F, KeyCode.LeftShift),
                "Hovering a slot of the player inventory: the slot becomes a favourite (its content never moves, never sorted). Press again to remove.", synced: false);
            ShowFavourites = synced.Bind(Section, "Show Favourites", true,
                "Favourite items get a small star in the corner of the slot, favourite slots a coloured border, junk a small cross.", synced: false);
        }

        private static void BindTrash(SyncedConfiguration synced)
        {
            JunkKey = synced.Bind(Section, "Junk Key", RenamedKeys.Carry(synced.Config, Section, "Trash Flag Key", "Junk Key", new KeyboardShortcut(KeyCode.J)),
                "Hovering an item: the item name is marked as junk; Destroy Junk destroys every junk stack. Press again to unmark it.", synced: false);
            TrashKey = synced.Bind(Section, "Trash Key", new KeyboardShortcut(KeyCode.Delete),
                "Hovering an item: the stack is trashed after confirmation.", synced: false);
            DestroyJunkKey = synced.Bind(Section, "Destroy Junk Key", RenamedKeys.Carry(synced.Config, Section, "Trash Flagged Key", "Destroy Junk Key", new KeyboardShortcut(KeyCode.Delete, KeyCode.LeftShift)),
                "Destroys every junk stack in the inventory after confirmation.", synced: false);
            ConfirmTrash = synced.Bind(Section, "Confirm Trash", true,
                "The game's popup asks before anything is destroyed; gamepad confirm and cancel work.", synced: false);
            TrashUsesSalvage = synced.Bind(Section, "Trash Uses Salvage", false,
                "Trashing a whole stack from the inventory salvages it instead (section 3) when the item can be salvaged.", synced: false);
        }

        private static void BindClicks(SyncedConfiguration synced)
        {
            RouteModifier = synced.Bind(Section, "Route Modifier", new KeyboardShortcut(KeyCode.LeftControl),
                "Held with a left click on an inventory item: the stack is sent to the nearest nearby container that already holds the item, an item of its group, or accepts it in OpenKeep.Stow.yml.", synced: false);
            StoreOneKey = synced.Bind(Section, "Store One Key", new KeyboardShortcut(KeyCode.V),
                "On a hovered inventory item: one item of the stack goes to the open container, or to the nearest container holding it.", synced: false);
            FindKey = synced.Bind(Section, "Find Key", new KeyboardShortcut(KeyCode.Z),
                "Hovering an item: every nearby container holding it is marked for Link Seconds (the Reach setting) with a line from the player and a floating count.", synced: false);
            DumpKey = synced.Bind(Section, "Dump Key", new KeyboardShortcut(KeyCode.D, KeyCode.LeftAlt),
                "Outside the inventory: quick stack to every nearby container.", synced: false);
        }

        private static void BindCycling(SyncedConfiguration synced)
        {
            CyclePreviousKey = synced.Bind(Section, "Cycle Previous Key", new KeyboardShortcut(KeyCode.LeftArrow),
                "With a container open: closes it and opens the previous nearby container, ordered by angle around the player. Only containers the game would keep open (within its auto close distance) take part.", synced: false);
            CycleNextKey = synced.Bind(Section, "Cycle Next Key", new KeyboardShortcut(KeyCode.RightArrow),
                "With a container open: closes it and opens the next nearby container.", synced: false);
            CycleWithWheel = synced.Bind(Section, "Cycle With Wheel", true,
                "The mouse wheel over the container grid cycles too.", synced: false);
        }

        private static void BindPickup(SyncedConfiguration synced)
        {
            GroundPickup = synced.Bind(Section, "Ground Pickup", false,
                "Containers whose prefab has pickup: true in OpenKeep.Stow.yml pull dropped items from the ground by rule.");
            PickupRange = synced.Bind(Section, "Pickup Range", 10f,
                "Metres around the container.", acceptableValues: new AcceptableValueRange<float>(1f, 50f));
            PickupInterval = synced.Bind(Section, "Pickup Interval", 5f,
                "Seconds between sweeps of one container.", acceptableValues: new AcceptableValueRange<float>(1f, 120f));
            PickupDelay = synced.Bind(Section, "Pickup Delay", 30f,
                "Seconds an item must lie on the ground before a container takes it, measured from the item's spawn time, so players can pick up their own drops.",
                acceptableValues: new AcceptableValueRange<float>(0f, 3600f));
            PickupOnlyHeldItems = synced.Bind(Section, "Pickup Only Held Items", true,
                "A container only takes items it already holds; the YAML accept list adds more. Off: it takes everything its refuse list allows.");
        }
    }
}
