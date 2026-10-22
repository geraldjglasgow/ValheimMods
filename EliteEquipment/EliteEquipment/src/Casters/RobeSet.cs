using System.Collections.Generic;
using UnityEngine;

using HairType = ItemDrop.ItemData.HelmetHairType;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// One tier's Seer's robes (features/casters.md, "Armour: the Seer's robes"): its three pieces (head, robe, trousers)
    /// built from the workshop bundle <c>ee_seer_&lt;tier&gt;</c>, their numbers (half the biome's heavy armour, +2 a level,
    /// -2% movement on robe and trousers where the heavy set has -5%: the game's own mage sets' pattern, user 2026-10-11),
    /// the eitr each gives, how the head piece hides hair, and the set bonus all three give together
    /// (<see cref="CasterStatus"/> makes it from <see cref="Bonus"/>). Its words are in translations/English.yml: the set's
    /// name <see cref="SetWord"/> and its bonus that + "_tip", each piece's <c>ee_seer_&lt;tier&gt;_&lt;part&gt;</c> and
    /// <c>_desc</c>, the split boots' <c>ee_boots_seer&lt;tier&gt;</c>.
    /// </summary>
    public sealed class RobeSet
    {
        public string Tier;
        public string Title;
        public HairType Hair;
        public float Armour;
        public float Movement;
        public float Eitr;
        public System.Action<SE_Stats> Bonus;
        public GearPiece Head;
        public GearPiece Robe;
        public GearPiece Trousers;

        public StatusEffect SetBonus;

        public string SetWord => "ee_seer_" + Tier + "_set";
        public string BonusName => "EE_SE_Seer" + Title + "Set";
        public int BonusHash => BonusName.GetStableHashCode();
        public string BootsKey => "Seer" + Title;
        public IReadOnlyList<GearPiece> Pieces => new[] { Head, Robe, Trousers };

        /// <summary>A piece of this set: its prefab, the game piece it copies, the one its recipe sits beside, its word and cost.</summary>
        public GearPiece Piece(string part, string game, string beside, params GearCost[] cost)
        {
            string title = char.ToUpperInvariant(part[0]) + part.Substring(1);
            return new GearPiece
            {
                Prefab = "EE_Seer_" + Title + "_" + title, Base = game, Bundle = "ee_seer_" + Tier, Asset = "ee_seer_" + Tier + "_" + part,
                Word = "ee_seer_" + Tier + "_" + part, Eitr = Eitr, Armour = Armour,
                Movement = part == "hood" ? 0f : Movement, Part = part, Set = this, RecipeBeside = beside, Cost = cost,
            };
        }

        /// <summary>A resistance as the set's bonus.</summary>
        public static System.Action<SE_Stats> Resist(params HitData.DamageType[] types) => effect =>
        {
            foreach (HitData.DamageType type in types)
                effect.m_mods.Add(new HitData.DamageModPair { m_type = type, m_modifier = HitData.DamageModifier.Resistant });
        };

        /// <summary>Blood Magic raised as the set's bonus.</summary>
        public static void BloodMagic(SE_Stats effect, float levels)
        {
            effect.m_skillLevel = Skills.SkillType.BloodMagic;
            effect.m_skillLevelModifier = levels;
        }
    }
}
