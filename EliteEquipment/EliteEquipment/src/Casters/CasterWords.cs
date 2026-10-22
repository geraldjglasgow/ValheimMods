using Translations;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The casters' words are in translations/English.yml: the gear's, spells', sets' and foods' (their prefabs and status
    /// effects carry them as tokens), the Spellbook's, the messages and the tooltip lines. This builds the ones two places
    /// share.
    /// </summary>
    public static class CasterWords
    {
        /// <summary>The button a slot casts with, as the spell bar and the Spellbook label it.</summary>
        public static string KeyOf(int slot) => Lang.Get(slot == 0 ? "ee_spell_key_first" : "ee_spell_key_second");
    }
}
