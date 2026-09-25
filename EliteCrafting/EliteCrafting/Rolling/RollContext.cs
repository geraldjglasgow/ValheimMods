using System;
using EliteCrafting.Items;
using EliteCrafting.Rules;

namespace EliteCrafting.Rolling
{
    /// <summary>Why a roll could not be made. A failed roll never changes anything.</summary>
    public enum RollFailure
    {
        None,
        /// <summary>The pool cannot supply the affixes the roll needs (<c>$ecf_msg_no_eligible_affix</c>).</summary>
        NoEligibleAffix,
        /// <summary>The item cannot carry affixes (stackable, no slot, a stone).</summary>
        NotMagicBase,
        /// <summary>The item already holds its rarity's maximum.</summary>
        Full,
        /// <summary>The item holds its rarity's minimum (or nothing removable).</summary>
        AtMinimum,
        /// <summary>No affix whose value can be rerolled (all dormant or bound).</summary>
        NothingToReroll,
        /// <summary>The state was written by a newer version of the mod.</summary>
        NewerFormat,
        /// <summary>An essence's family has no eligible member for this item (<c>$ecf_msg_essence_no_match</c>).</summary>
        NoFamilyMatch,
    }

    /// <summary>How a removal picks its affix.</summary>
    public enum RemovePick
    {
        /// <summary>Uniformly among removable affixes.</summary>
        Random,
        /// <summary>Dormant affixes first, then the lowest tier (Sigil of Culling).</summary>
        LowestTier,
    }

    /// <summary>
    /// The inputs every roll shares (rarity.md section 4): the item's slot info and tier ceiling, the stone's tier
    /// floor, an optional category steer, the chaotic flag (Serpent: every tier the affix defines, uniformly, ceiling
    /// ignored), an id the roll must not pick (Turmoil's just-removed affix), and the random source. Build one with
    /// <see cref="For"/>; the drop code passes its own ceiling for a base's tier.
    /// </summary>
    public sealed class RollContext
    {
        public SlotInfo Slot { get; set; } = SlotInfo.NotEligible;

        /// <summary>The item's tier ceiling, 1-7.</summary>
        public int Ceiling { get; set; } = 1;

        /// <summary>The lowest tier to roll, 0 = none; clamped to the ceiling.</summary>
        public int TierFloor { get; set; }

        /// <summary>Only affixes of this category (War / Warding / Fortune sigils); null = any.</summary>
        public AffixCategory? Category { get; set; }

        public bool Chaotic { get; set; }

        /// <summary>Never pick this id in this roll.</summary>
        public string? ExcludeId { get; set; }

        public Random Random { get; set; } = RollRandom.Create();

        /// <summary>The rules to roll under; the running rules unless a caller pins a snapshot.</summary>
        public RuleSet Rules { get; set; } = ActiveRules.Current;

        /// <summary>A context for an item: its slot info, its tier ceiling (<see cref="ItemTier"/>), an optional stone floor.</summary>
        public static RollContext For(ItemDrop.ItemData item, int tierFloor = 0)
        {
            return new RollContext
            {
                Slot = ItemSlots.Classify(item),
                Ceiling = ItemTier.Of(item),
                TierFloor = tierFloor,
            };
        }
    }
}
