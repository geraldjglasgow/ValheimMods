using BepInEx.Configuration;
using Hotkeys;
using PackPanel.Core;
using SyncedConfig;
using UnityEngine;

namespace PackPanel.Consume
{
    /// <summary>
    /// The keys of section "2. Slots" that eat and drink from the slots (<see cref="ConsumeKeys"/>, <see cref="MeadSlotKeys"/>),
    /// and in "5. Look" the bar that shows the Food Key and the Mead Key on the screen (<see cref="ConsumeBar"/>). Keys are
    /// each player's own, so they are not synced. Z and B are free in the game (only its debug mode uses them, for flying and
    /// free building); OpenKeep's Find Key is also Z, but only inside the inventory, where these keys do nothing. Left Alt
    /// is free in the game; the game's hotbar keys ignore it, so <see cref="MeadSlotKeys"/> holds the hotbar back while it is held.
    /// </summary>
    public static class ConsumeSettings
    {
        public static ConfigEntry<KeyboardShortcut> FoodKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> MeadKey { get; private set; }

        /// <summary>One key per Mead slot, left to right, as many as the most Mead slots there can be.</summary>
        public static ConfigEntry<KeyboardShortcut>[] MeadSlotKeys { get; } = new ConfigEntry<KeyboardShortcut>[InventorySettings.MaxGroup];

        public static ConfigEntry<bool> Bar { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            FoodKey = synced.Bind(InventorySettings.SlotsSection, "Food Key", new KeyboardShortcut(KeyCode.Z),
                "Outside the inventory, with no hammer, hoe or cultivator in hand: eats every food in the Food slots that can be eaten now, left to right. Per player.", synced: false);
            MeadKey = synced.Bind(InventorySettings.SlotsSection, "Mead Key", new KeyboardShortcut(KeyCode.B),
                "Outside the inventory, with no hammer, hoe or cultivator in hand: drinks every mead in the Mead slots that can be drunk now (one of each kind of effect), left to right. Per player.", synced: false);
            BindMeadSlotKeys(synced);
            Bar = synced.Bind(InventorySettings.LookSection, "Food And Mead Bar", true,
                "In the bottom-left corner of the screen, under your health: a food square with the Food Key over it, a mead square with the Mead Key over it, then a square per Mead slot showing its mead with its Mead Slot key over it.", synced: false);
            Typing.AddWindow(() => synced.YamlEditor.IsOpen);
        }

        /// <summary>Mead Slot 1 Key to Mead Slot 5 Key, Left Alt + 1 to 5 by default.</summary>
        private static void BindMeadSlotKeys(SyncedConfiguration synced)
        {
            for (int i = 0; i < MeadSlotKeys.Length; i++)
            {
                int number = i + 1;
                MeadSlotKeys[i] = synced.Bind(InventorySettings.SlotsSection, $"Mead Slot {number} Key", new KeyboardShortcut(KeyCode.Alpha1 + i, KeyCode.LeftAlt),
                    $"Outside the inventory, with no hammer, hoe or cultivator in hand: drinks the mead in Mead slot {number} (counted left to right), if it can be drunk now. While this key's modifiers are held (Left Alt by default), the number keys use no hotbar item. Per player.", synced: false);
            }
        }
    }
}
