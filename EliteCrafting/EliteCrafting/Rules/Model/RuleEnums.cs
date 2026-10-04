using System;

namespace EliteCrafting.Rules
{
    // Every enum here maps to the YAML's snake_case ids through Core.EnumIds (PascalCase ↔ snake_case).

    public enum AffixValueType { Percent, Flat, Flag }

    public enum AffixUnit { None, Ms, Deg, M, Min }

    public enum AffixCategory { Offense, Defense, Utility }

    public enum AffixCondition { None, HealthCritical }

    public enum HookDifficulty { None, Easy, Medium, Hard }

    /// <summary>What a rune does: Awakening and Ascension promote, Shaping and Consecrated add, Cleansing strips, the
    /// Serpent Rune corrupts, Recasting rerolls.</summary>
    public enum StoneVerb { Promote, Add, Strip, Corrupt, Reroll }

    /// <summary>The Serpent Rune's outcomes; every one seals the item.</summary>
    public enum CorruptOutcome { SealOnly, AddInscription, ChaoticReroll }

    /// <summary>Damage types a <c>damage_type</c> or <c>element</c> param names; groups are unions.</summary>
    [Flags]
    public enum DamageMask
    {
        None = 0,
        Blunt = 1,
        Slash = 2,
        Pierce = 4,
        Fire = 8,
        Frost = 16,
        Lightning = 32,
        Poison = 64,
        Spirit = 128,
        Physical = Blunt | Slash | Pierce,
        Elemental = Fire | Frost | Lightning | Poison,
        All = Physical | Elemental | Spirit,
    }
}
