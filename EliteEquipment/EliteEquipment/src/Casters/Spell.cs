using Translations;

namespace EliteEquipment.Casters
{
    /// <summary>What a spell does when cast.</summary>
    public enum SpellKind
    {
        /// <summary>Heals one friend: the one nearest the crosshair, or the caster.</summary>
        Heal,

        /// <summary>A status effect on everyone friendly within <see cref="Spell.ChantRange"/>, the caster included.</summary>
        Chant,

        /// <summary>Damage cast where the caster aims, never at a friend (Elder's Volley: <see cref="VolleyCast"/>; Moder's Breath: <see cref="BreathCast"/>).</summary>
        Strike,

        /// <summary>
        /// A protection: Ward on one friend, aimed like a heal (<see cref="Casters.Ward"/>); Sanctuary's dome at the
        /// caster's feet (<see cref="Casters.Sanctuary"/>).
        /// </summary>
        Ward,
    }

    /// <summary>
    /// One spell (features/casters.md): its key (saved on spellbooks and wands, never renamed; its words in
    /// translations/English.yml are <c>ee_spell_&lt;key&gt;</c>, <c>_desc</c> and its page's
    /// <c>ee_spellitem_&lt;key&gt;</c>), icon in the <c>ee_spells_meadows</c> bundle, eitr cost, kind, a chant's status
    /// effect, the colour of its effects, its tier and the boss that drops it. A wand casts it only while its page is in
    /// the spellbook in the left hand (<see cref="BookPages"/>).
    /// </summary>
    public sealed class Spell
    {
        public const float ChantRange = 10f;
        public const float ChantSeconds = 60f;

        public string Key;
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

        /// <summary>Its page item (<c>EE_Spell_*</c>) and the page's icon in the bundle.</summary>
        public string Item;
        public string ItemIcon;

        /// <summary>Its biome's place in the game's order, 1 the Meadows to 7 the Ashlands (<see cref="PageDrops"/>).</summary>
        public int Tier = 1;

        /// <summary>The boss whose death drops its page, one per player present (<see cref="BossPages"/>); null for the rest.</summary>
        public string Boss;

        /// <summary>
        /// Found only in dungeon chests, this likely in each as its loot is first made, whatever the biome (<see cref="ChestPages"/>;
        /// Revive's 2%); never from creatures or by <see cref="PageDrops"/>' biome rule. 0 for the rest.
        /// </summary>
        public float ChestOnly;

        /// <summary>
        /// The spell's own motion and effects (features/casters.md, "Spell animations"): its upper-body clip in
        /// <c>ee_casteranim</c> (<see cref="SpellClips"/>, played by <see cref="SpellMotion"/>, the cast at its <c>Hit</c>), its
        /// charge from the cast's start to that <c>Hit</c> and its release where the cast lands, both in
        /// <c>ee_casterfx_meadows</c> (<see cref="SpellFx"/>; a volley's release at the tip with every seed,
        /// <see cref="VolleyCast"/>). Null, or missing from the bundle, means the shared ones: the game staff's cast motion
        /// (<see cref="WandItem"/>), the white charge, the cast flash <see cref="CasterFx.Cast"/> and the chant ring. Mend has
        /// no release of its own: its mend bolt and motes are its release.
        /// </summary>
        public string Motion;
        public string ChargeEffect;
        public string ReleaseEffect;

        /// <summary>The skill its cost is cut by: a strike's Elemental Magic (its hits raise it), else Blood Magic.</summary>
        public Skills.SkillType Skill => Kind == SpellKind.Strike ? Skills.SkillType.ElementalMagic : Skills.SkillType.BloodMagic;

        /// <summary>The Spellbook's word for its kind.</summary>
        public string KindWord => Kind == SpellKind.Heal ? "ee_spellbook_kind_heal"
            : Kind == SpellKind.Strike ? "ee_spellbook_kind_strike"
            : Kind == SpellKind.Ward ? "ee_spellbook_kind_ward" : "ee_spellbook_kind_chant";

        public string Token => Lang.Token("ee_spell_" + Key);
        public string DescriptionToken => Lang.Token("ee_spell_" + Key + "_desc");
        public string ItemToken => Lang.Token("ee_spellitem_" + Key);

        /// <summary>Found on creatures and in dungeon chests: every page but a boss's and a chest-only one (Mend's too since 2026-11-02).</summary>
        public bool Loot => Boss == null && ChestOnly <= 0f;
    }
}
