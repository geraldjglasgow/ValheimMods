
namespace EliteEquipment.Casters
{
    /// <summary>What a spell does when cast.</summary>
    public enum SpellKind
    {
        /// <summary>Heals one friend: the one nearest the crosshair, or the caster.</summary>
        Heal,

        /// <summary>A status effect on everyone friendly within <see cref="Spell.ChantRange"/>, the caster included.</summary>
        Chant,
    }

    /// <summary>
    /// One spell (features/casters.md): its key (saved on characters and wands, never renamed), words, icon in the
    /// <c>ee_spells_meadows</c> bundle, eitr cost, kind, a chant's status effect, and the colour of its effects. A spell
    /// other than Mend is learned from its item (<see cref="Item"/>).
    /// </summary>
    public sealed class Spell
    {
        public const float ChantRange = 10f;
        public const float ChantSeconds = 60f;

        public string Key;
        public string Name;
        public string Description;
        public string Icon;
        public float Eitr;
        public SpellKind Kind;

        /// <summary>
        /// The colour of its cast flash and ring: the workshop authors them in Hearthsong's gold (hue 0.10), and a spell of
        /// another colour turns them by <see cref="HueShift"/> and scales their saturation by <see cref="Saturation"/>
        /// (<c>EffectTint.Shift</c>; the workshop's numbers, ee_casterfx_meadows/BRIEF.md).
        /// </summary>
        public float HueShift;
        public float Saturation = 1f;

        public bool Gold => HueShift == 0f && Saturation == 1f;

        /// <summary>The chant's status effect prefab name (<c>EE_SE_*</c>); none for a heal.</summary>
        public string Effect;

        /// <summary>The item that teaches it (<c>EE_Spell_*</c>), its icon in the bundle; none for a spell every character knows.</summary>
        public string Item;
        public string ItemIcon;

        public string Token => "$ee_spell_" + Key;
        public string DescriptionToken => "$ee_spell_" + Key + "_desc";
        public string ItemToken => "$ee_spellitem_" + Key;
        public bool KnownByAll => Item == null;
    }
}
