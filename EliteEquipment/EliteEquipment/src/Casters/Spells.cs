using System.Collections.Generic;

namespace EliteEquipment.Casters
{
    /// <summary>The spells built so far (the Meadows', Elder's Volley, Cleanse and the nine more of 2026-11-02; features/casters.md has them all), in Spellbook order.</summary>
    public static class Spells
    {
        public const string MendKey = "mend";

        public static readonly Spell Mend = new Spell
        {
            Key = MendKey, Icon = "ee_spellicon_mend", Eitr = 8f, Kind = SpellKind.Heal,
            Item = "EE_Spell_Mend", ItemIcon = "ee_spell_mend_icon",
            Motion = SpellClips.Reach, ChargeEffect = SpellFx.ChargeMend,
        };

        public static readonly Spell Hearthsong = new Spell
        {
            Key = "hearthsong", Icon = "ee_spellicon_hearthsong", Eitr = 15f, Kind = SpellKind.Chant,
            Effect = "EE_SE_Hearthsong", Item = "EE_Spell_Hearthsong", ItemIcon = "ee_spell_hearthsong_icon",
            Motion = SpellClips.Raise, ChargeEffect = SpellFx.ChargeHearth, ReleaseEffect = SpellFx.CastHearth,
        };

        public static readonly Spell Swiftness = new Spell
        {
            Key = "swiftness", Icon = "ee_spellicon_swiftness", Eitr = 15f, Kind = SpellKind.Chant,
            Effect = "EE_SE_Swiftness", Item = "EE_Spell_Swiftness", ItemIcon = "ee_spell_swiftness_icon",
            HueShift = 0.2f, Saturation = 0.75f, Boss = "Eikthyr",
            Motion = SpellClips.Sweep, ChargeEffect = SpellFx.ChargeSwift, ReleaseEffect = SpellFx.CastSwift,
        };

        /// <summary>
        /// Six of the Elder's seeds from the wand toward the aim (<see cref="VolleyCast"/>), with its own motion and effects
        /// (features/casters.md, "Spell animations": the wand thrust forward and held, a recoil per seed; green seeds
        /// spiralling into the tip; a green burst at the tip per seed).
        /// </summary>
        public static readonly Spell Volley = new Spell
        {
            Key = "volley", Icon = "ee_spellicon_volley", Eitr = 20f, Kind = SpellKind.Strike,
            Item = "EE_Spell_Volley", ItemIcon = "ee_spell_volley_icon",
            HueShift = 0.2f, Tier = 2, Boss = "gd_king",
            Motion = SpellClips.Thrust, ChargeEffect = SpellFx.ChargeVolley, ReleaseEffect = SpellFx.CastVolley,
        };

        /// <summary>
        /// Takes every harmful status effect off the friend marked, or the caster, with a small heal (<see cref="Cleanse"/>):
        /// a Swamp loot page, Mend's reach motion, a pale water-blue charge at the tip and a washing splash on the target.
        /// </summary>
        public static readonly Spell Cleanse = new Spell
        {
            Key = "cleanse", Icon = "ee_spellicon_cleanse", Eitr = 12f, Kind = SpellKind.Heal,
            Item = "EE_Spell_Cleanse", ItemIcon = "ee_spell_cleanse_icon",
            HueShift = 0.45f, Saturation = 0.6f, Tier = 3,
            Motion = SpellClips.Reach, ChargeEffect = SpellFx.ChargeCleanse, ReleaseEffect = SpellFx.CastCleanse,
        };

        /// <summary>
        /// Moder's frost from the wand toward the aim, six bursts on the thrust's six <c>Hit</c>s (<see cref="BreathCast"/>):
        /// Moder's boss page, a frost charge at the tip, Moder's own breath as its release.
        /// </summary>
        public static readonly Spell Breath = new Spell
        {
            Key = "breath", Icon = "ee_spellicon_breath", Eitr = 25f, Kind = SpellKind.Strike,
            Item = "EE_Spell_Breath", ItemIcon = "ee_spell_breath_icon",
            HueShift = 0.45f, Saturation = 0.5f, Tier = 4, Boss = "Dragon",
            Motion = SpellClips.Thrust, ChargeEffect = BreathCast.Charge,
        };

        /// <summary>A shield on the friend marked, or the caster (<see cref="Casters.Ward"/>): a Mountain loot page.</summary>
        public static readonly Spell Ward = new Spell
        {
            Key = "ward", Icon = "ee_spellicon_ward", Eitr = 20f, Kind = SpellKind.Ward,
            Item = "EE_Spell_Ward", ItemIcon = "ee_spell_ward_icon",
            HueShift = 0.45f, Saturation = 0.4f, Tier = 4,
            Motion = SpellClips.Reach, ChargeEffect = SpellFx.ChargeWard, ReleaseEffect = SpellFx.CastWard,
        };

        /// <summary>A dome at the caster's feet, less damage inside (<see cref="Casters.Sanctuary"/>): The Queen's page.</summary>
        public static readonly Spell Sanctuary = new Spell
        {
            Key = "sanctuary", Icon = "ee_spellicon_sanctuary", Eitr = 40f, Kind = SpellKind.Ward,
            Item = "EE_Spell_Sanctuary", ItemIcon = "ee_spell_sanctuary_icon",
            Saturation = 0.5f, Tier = 6, Boss = "SeekerQueen",
            Motion = SpellClips.Raise, ChargeEffect = SpellFx.ChargeWard, ReleaseEffect = SpellFx.CastSanctuary,
        };

        /// <summary>A chant of more damage (<see cref="Casters.WarChant"/>): a Plains loot page.</summary>
        public static readonly Spell WarChant = new Spell
        {
            Key = "warchant", Icon = "ee_spellicon_warchant", Eitr = 25f, Kind = SpellKind.Chant,
            Effect = "EE_SE_WarChant", Item = "EE_Spell_WarChant", ItemIcon = "ee_spell_warchant_icon",
            HueShift = 0.88f, Tier = 5,
            Motion = SpellClips.Raise, ChargeEffect = SpellFx.ChargeWar, ReleaseEffect = SpellFx.CastWarchant,
        };

        /// <summary>A chant of the feather cape's slow fall (<see cref="Casters.FeatherFall"/>): a Mountain loot page.</summary>
        public static readonly Spell FeatherFall = new Spell
        {
            Key = "featherfall", Icon = "ee_spellicon_featherfall", Eitr = 15f, Kind = SpellKind.Chant,
            Effect = "EE_SE_FeatherFall", Item = "EE_Spell_FeatherFall", ItemIcon = "ee_spell_featherfall_icon",
            HueShift = 0.45f, Saturation = 0.2f, Tier = 4,
            Motion = SpellClips.Sweep, ChargeEffect = SpellFx.ChargeFeather, ReleaseEffect = SpellFx.CastFeatherfall,
        };

        /// <summary>A healing circle on the ground where the caster aims, 8 s (<see cref="Casters.Spring"/>): a Swamp loot page.</summary>
        public static readonly Spell Spring = new Spell
        {
            Key = "spring", Icon = "ee_spellicon_spring", Eitr = 25f, Kind = SpellKind.Heal,
            Item = "EE_Spell_Spring", ItemIcon = "ee_spell_spring_icon",
            HueShift = 0.45f, Saturation = 0.6f, Tier = 3,
            Motion = SpellClips.Raise, ChargeEffect = SpellFx.ChargeCleanse, ReleaseEffect = HealReleases.Spring,
        };

        /// <summary>50 stamina to every player within 10 m (<see cref="Casters.SecondWind"/>): a Plains loot page.</summary>
        public static readonly Spell SecondWind = new Spell
        {
            Key = "secondwind", Icon = "ee_spellicon_secondwind", Eitr = 20f, Kind = SpellKind.Heal,
            Item = "EE_Spell_SecondWind", ItemIcon = "ee_spell_secondwind_icon",
            HueShift = 0.2f, Saturation = 0.75f, Tier = 5,
            Motion = SpellClips.Sweep, ChargeEffect = SpellFx.ChargeSwift, ReleaseEffect = HealReleases.SecondWind,
        };

        /// <summary>Mend jumping on to up to 4 friends, 20% less each (<see cref="Casters.ChainMend"/>): a Mistlands loot page.</summary>
        public static readonly Spell ChainMend = new Spell
        {
            Key = "chainmend", Icon = "ee_spellicon_chainmend", Eitr = 20f, Kind = SpellKind.Heal,
            Item = "EE_Spell_ChainMend", ItemIcon = "ee_spell_chainmend_icon", Tier = 6,
            Motion = SpellClips.Reach, ChargeEffect = SpellFx.ChargeMend, ReleaseEffect = HealReleases.ChainArc,
        };

        /// <summary>A friend dead within 3 min back at their grave (<see cref="Casters.Revive"/>): only in dungeon chests, 2%.</summary>
        public static readonly Spell Revive = new Spell
        {
            Key = "revive", Icon = "ee_spellicon_revive", Eitr = 50f, Kind = SpellKind.Heal,
            Item = "EE_Spell_Revive", ItemIcon = "ee_spell_revive_icon", ChestOnly = 0.02f,
            Motion = SpellClips.Reach, ChargeEffect = SpellFx.ChargeMend, ReleaseEffect = HealReleases.Revive,
        };

        public static readonly IReadOnlyList<Spell> All = new[]
        {
            Mend, Hearthsong, Swiftness, Volley, Cleanse, Breath, Ward, Sanctuary, WarChant, FeatherFall,
            Spring, SecondWind, ChainMend, Revive,
        };

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
                if (spell.Item == prefab)
                    return spell;
            }
            return null;
        }
    }
}
