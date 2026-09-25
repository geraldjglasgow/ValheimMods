using System;
using System.Collections.Generic;

namespace EliteCrafting.Items
{
    /// <summary>The slot taxonomy affixes are assigned to (affixes.md section 2). Ids: <c>melee_weapon</c> ... <c>tool</c>.</summary>
    public enum ItemSlot
    {
        None,
        MeleeWeapon,
        RangedWeapon,
        MagicWeapon,
        Shield,
        Head,
        Chest,
        Legs,
        Cape,
        UtilityItem,
        Tool,
    }

    /// <summary>One- or two-handed, for <c>requires.hands</c>. <see cref="None"/> on items that are not held weapons.</summary>
    public enum Hands
    {
        None,
        One,
        Two,
    }

    /// <summary>Item properties an affix may require (<c>requires.traits</c>, all of).</summary>
    [Flags]
    public enum ItemTraits
    {
        None = 0,
        WearsOut = 1,
        MovementPenalty = 2,
        Builds = 4,
        Projectile = 8,
        Ammo = 16,
        CanParry = 32,
    }

    /// <summary>
    /// What an item type is, for affix purposes: its slot, hands, traits and governing skills. Computed once per
    /// <c>SharedData</c> (per item type) by <see cref="ItemSlots.Classify"/>; immutable.
    /// </summary>
    public sealed class SlotInfo
    {
        public static readonly SlotInfo NotEligible = new SlotInfo(ItemSlot.None, Hands.None, ItemTraits.None,
            Array.Empty<Skills.SkillType>());

        public SlotInfo(ItemSlot slot, Hands hands, ItemTraits traits, IReadOnlyList<Skills.SkillType> skills)
        {
            Slot = slot;
            Hands = hands;
            Traits = traits;
            GoverningSkills = skills;
        }

        public ItemSlot Slot { get; }
        public Hands Hands { get; }
        public ItemTraits Traits { get; }

        /// <summary>The item's own skill, plus WoodCutting when it chops and Pickaxes when it mines.</summary>
        public IReadOnlyList<Skills.SkillType> GoverningSkills { get; }

        public bool HasTraits(ItemTraits required) => (Traits & required) == required;

        public bool IsGovernedBy(Skills.SkillType skill)
        {
            for (int i = 0; i < GoverningSkills.Count; i++)
            {
                if (GoverningSkills[i] == skill)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
