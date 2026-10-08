using BepInEx.Configuration;
using PackPanel.Backpacks;
using PackPanel.Crafting;
using PackPanel.Layout;
using PackPanel.Look;
using PackPanel.Panels;
using PackPanel.Ring;
using PackPanel.Slots;
using PackPanel.Tackle;
using SyncedConfig;

namespace PackPanel.Core
{
    /// <summary>
    /// Binds and wires everything of the player's own inventory: a bigger grid, labelled slots beside it (worn gear, a
    /// backpack, up to five worn utilities, food, mead, ammo and a coin purse), a key ring, a tacklebox, the backpacks and
    /// the look.
    /// The slots are cells of the game's own inventory (<see cref="InventoryLayout"/>); the game's grid draws them and
    /// <see cref="SlotElements"/> moves them into the slot panel. A changed layout setting (or FeastMaster's food count, or
    /// OpenKeep's Separate Boots) re-lays the inventory out on the next frame.
    /// </summary>
    public static class InventoryModule
    {
        private static bool pending;

        public static void Initialize(SyncedConfiguration synced)
        {
            InventorySettings.Bind(synced);
            TimberFrame.Initialize();
            GamePanelTheme.Initialize();
            Words.Register();
            BackpackWords.Register();
            TackleboxWords.Register();
            Consume.ConsumeWords.Register();
            Panels.StatTipWords.Register();
            BackpacksFile.Initialize(synced);
            TackleboxesFile.Initialize(synced);
            CraftRecipes.Watch();
            WatchLayoutKeys();
            FoodCount.Changed += () => pending |= InventorySettings.FoodSlotsFollowEating.Value && !SlotCounts.Shared;
            OpenKeepLink.BootsChanged += () => pending |= InventorySettings.EquipmentSlots.Value;   // the Feet slot comes or goes
            InventorySettings.BrownStyle.SettingChanged += (sender, args) => PanelSize.Refresh();
            InventorySettings.PanelTheme.SettingChanged += (sender, args) => PanelSize.Refresh();
            InventorySettings.Enabled.SettingChanged += (sender, args) => PanelSize.Refresh();   // the brown look goes with it
            InventorySettings.SlotLabels.SettingChanged += (sender, args) => SlotElements.Invalidate();
            InventorySettings.SlotIcons.SettingChanged += (sender, args) => SlotElements.Invalidate();
        }

        /// <summary>Called every frame of the local player (<see cref="PlayerTick"/>): applies a layout change waiting from the settings.</summary>
        public static void ApplyPending(Player player)
        {
            if (!pending || player.IsDead())
                return;
            pending = false;
            if (LayoutApply.Wanted(player))
                LayoutApply.Apply(player, dropOverflow: true);
        }

        private static void WatchLayoutKeys()
        {
            Watch(InventorySettings.Enabled);
            Watch(InventorySettings.InventoryWidth);
            Watch(InventorySettings.InventoryRows);
            Watch(InventorySettings.EquipmentSlots);
            Watch(InventorySettings.SlotsPerGroup);
            Watch(InventorySettings.UtilitySlots);
            Watch(InventorySettings.TrinketSlot);
            Watch(InventorySettings.BackpackSlot);
            Watch(InventorySettings.FoodSlots);
            Watch(InventorySettings.FoodSlotsFollowEating);
            Watch(InventorySettings.MeadSlots);
            Watch(InventorySettings.AmmoSlots);
            Watch(InventorySettings.CoinPurse);
            Watch(KeyRingSettings.KeyRing);
            Watch(KeyRingSettings.KeyItems);
            Watch(TackleboxSettings.Enabled);
            Watch(TackleboxSettings.TackleItems);
            KeyStacks.Watch();
        }

        private static void Watch<T>(ConfigEntry<T> entry) => entry.SettingChanged += (sender, args) => pending = true;
    }
}
