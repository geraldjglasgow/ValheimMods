using System.Collections.Generic;

namespace EliteEquipment.Casters
{
    /// <summary>The spells built so far (the Meadows; features/casters.md has all 22), in Spellbook order.</summary>
    public static class Spells
    {
        public const string MendKey = "mend";

        public static readonly Spell Mend = new Spell
        {
            Key = MendKey, Icon = "ee_spellicon_mend", Eitr = 8f, Kind = SpellKind.Heal,
        };

        public static readonly Spell Hearthsong = new Spell
        {
            Key = "hearthsong", Icon = "ee_spellicon_hearthsong", Eitr = 15f, Kind = SpellKind.Chant,
            Effect = "EE_SE_Hearthsong", Item = "EE_Spell_Hearthsong", ItemIcon = "ee_spell_hearthsong_icon",
        };

        public static readonly Spell Swiftness = new Spell
        {
            Key = "swiftness", Icon = "ee_spellicon_swiftness", Eitr = 15f, Kind = SpellKind.Chant,
            Effect = "EE_SE_Swiftness", Item = "EE_Spell_Swiftness", ItemIcon = "ee_spell_swiftness_icon",
            HueShift = 0.2f, Saturation = 0.75f,
        };

        public static readonly IReadOnlyList<Spell> All = new[] { Mend, Hearthsong, Swiftness };

        public static Spell Find(string key)
        {
            foreach (Spell spell in All)
            {
                if (spell.Key == key)
                    return spell;
            }
            return null;
        }

        public static Spell ByItem(string prefab)
        {
            foreach (Spell spell in All)
            {
                if (spell.Item != null && spell.Item == prefab)
                    return spell;
            }
            return null;
        }
    }
}
