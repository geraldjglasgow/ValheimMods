using BepInEx.Configuration;
using SyncedConfig;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Section "4. Casters": one switch for everything the casters add, off by default (user, 2026-10-09: "it should
    /// default to off"), synced and locked; read at use time.
    /// </summary>
    public static class CastersSettings
    {
        public const string Section = "4. Casters";
        public const string Key = "Wands and Spells";

        public static ConfigEntry<bool> Enabled { get; private set; }

        public static bool On => Enabled != null && Enabled.Value;

        public static void Initialize(SyncedConfiguration synced)
        {
            Enabled = synced.Bind(Section, Key, false,
                "A healer and caster line from the Meadows on: wands that cast spells chosen from the ones the character has " +
                "learned (2 held ready per wand: right mouse casts the first, right mouse held + left mouse the second; " +
                "the Spellbook button in the inventory prepares them), an off-hand focus and seer's robes that give eitr before the Mistlands. Meadows so far: the " +
                "Gnarled Wand, the Raven Fetish, the Deerhide Vestments; Mend, Hearthsong and Stag's Swiftness (Eikthyr). " +
                "Off by default. Off: none of it can be crafted, Eikthyr drops no spell, spell scrolls teach nothing, wands cast " +
                "nothing, the gear gives no eitr and no spell bar or Spellbook shows (items already made stay).");
        }
    }
}
