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
        /// <summary>The state was written by a newer version of the mod.</summary>
        NewerFormat,
    }

    /// <summary>
    /// The inputs every roll shares (rarity.md section 4): the item's slot info and tier ceiling, the rune's tier
    /// floor, the chaotic flag (Serpent: every tier the affix defines, uniformly, ceiling ignored), and the random
    /// source. Build one with
    /// <see cref="For"/>; the drop code passes its own ceiling for a base's tier.
    /// </summary>
    public sealed class RollContext
    {
        public SlotInfo Slot { get; set; } = SlotInfo.NotEligible;

        /// <summary>The item's tier ceiling, 1-7.</summary>
        public int Ceiling { get; set; } = 1;

        /// <summary>The lowest tier to roll, 0 = none; clamped to the ceiling.</summary>
        public int TierFloor { get; set; }

        public bool Chaotic { get; set; }

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
