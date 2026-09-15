using BepInEx.Configuration;
using SyncedConfig;
using UnityEngine;

namespace OpenKeep.Salvage
{
    /// <summary>
    /// Section "3. Salvage". Every gameplay entry is synced and lockable; the hotkey is per player. Values are read
    /// at use time so edits, reloads and server pushes apply at once.
    /// </summary>
    public static class SalvageSettings
    {
        public const string Section = "3. Salvage";

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<float> ReturnFraction { get; private set; }
        public static ConfigEntry<RoundingMode> Rounding { get; private set; }
        public static ConfigEntry<bool> AtLeastOne { get; private set; }
        public static ConfigEntry<bool> UpgradeMaterials { get; private set; }
        public static ConfigEntry<bool> RequireKnownRecipe { get; private set; }
        public static ConfigEntry<bool> RequireStation { get; private set; }
        public static ConfigEntry<KeyboardShortcut> SalvageKey { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            BindReturns(synced);
            BindConditions(synced);
        }

        private static void BindReturns(SyncedConfiguration synced)
        {
            Enabled = synced.Bind(Section, "Enabled", true,
                "The Salvage tab exists in the crafting panel and the salvage hotkey works.");
            ReturnFraction = synced.Bind(Section, "Return Fraction", 0.75f,
                "Fraction of each recipe material returned when an item is salvaged. OpenKeep.Salvage.yml can override it per item.",
                acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            Rounding = synced.Bind(Section, "Rounding", RoundingMode.Round,
                "How the fraction of each material's total cost becomes a whole number: Floor, Round (halves up) or Ceil.");
            AtLeastOne = synced.Bind(Section, "At Least One", true,
                "A material with a positive share never returns less than one.");
            UpgradeMaterials = synced.Bind(Section, "Upgrade Materials", true,
                "Items above quality 1 also return the materials of every upgrade level, by the same fraction.");
        }

        private static void BindConditions(SyncedConfiguration synced)
        {
            RequireKnownRecipe = synced.Bind(Section, "Require Known Recipe", true,
                "Only items whose recipe the character has discovered can be salvaged.");
            RequireStation = synced.Bind(Section, "Require Station", false,
                "Salvaging needs the recipe's crafting station within its build range (any level). Items whose recipe has no station are unaffected.");
            SalvageKey = synced.Bind(Section, "Salvage Key", new KeyboardShortcut(KeyCode.Backspace),
                "Hovering an item in the inventory: salvages the whole stack after a yes/no confirmation. Per player.", synced: false);
        }
    }
}
