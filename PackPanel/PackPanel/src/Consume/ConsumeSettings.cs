using BepInEx.Configuration;
using Hotkeys;
using PackPanel.Core;
using SyncedConfig;
using UnityEngine;

namespace PackPanel.Consume
{
    /// <summary>
    /// The two keys of section "2. Slots" that eat and drink from the slots (<see cref="ConsumeKeys"/>), and in "5. Look"
    /// the bar that shows them on the screen (<see cref="ConsumeBar"/>). Keys are each
    /// player's own, so they are not synced. Z and B are free in the game (only its debug mode uses them, for flying and
    /// free building); OpenKeep's Find Key is also Z, but only inside the inventory, where these keys do nothing.
    /// </summary>
    public static class ConsumeSettings
    {
        public static ConfigEntry<KeyboardShortcut> FoodKey { get; private set; }
        public static ConfigEntry<KeyboardShortcut> MeadKey { get; private set; }
        public static ConfigEntry<bool> Bar { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            FoodKey = synced.Bind(InventorySettings.SlotsSection, "Food Key", new KeyboardShortcut(KeyCode.Z),
                "Outside the inventory, with no hammer, hoe or cultivator in hand: eats every food in the Food slots that can be eaten now, left to right. Per player.", synced: false);
            MeadKey = synced.Bind(InventorySettings.SlotsSection, "Mead Key", new KeyboardShortcut(KeyCode.B),
                "Outside the inventory, with no hammer, hoe or cultivator in hand: drinks every mead in the Mead slots that can be drunk now (one of each kind of effect), left to right. Per player.", synced: false);
            Bar = synced.Bind(InventorySettings.LookSection, "Food And Mead Bar", true,
                "In the bottom-left corner of the screen, under your health: a food square with the Food Key over it and a mead square with the Mead Key over it.", synced: false);
            Typing.AddWindow(() => synced.YamlEditor.IsOpen);
        }
    }
}
