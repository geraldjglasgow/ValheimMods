using EliteCraftingLink;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Rewards
{
    /// <summary>
    /// Every fixed number of Hel's rewards (features/arena.md section 3, "Rewards"), paid at a trial's end to every player
    /// of it. Idols: one per boss wave beaten, the Forge of Potential's own (the game's <c>Upgrader0</c> ... <c>7</c>
    /// <c>Weapon</c>/<c>Armor</c>, Battle and Protection Idols), at the boss's biome's tier (Meadows 0, Wooden ... Deep
    /// North 7, Bloodgold; every boss after Kall another 7), Battle and Protection in turn from the first boss. Coins:
    /// the player's unspent souls over 2. Skill XP: what the player gained in the trial again times the waves cleared
    /// over 10, at most twice.
    /// With EliteCrafting, for each wave cleared: runes drawn from EliteCrafting's own default weights at the wave's tier
    /// (its tiers are the biomes, Meadows 1 to the Deep North 8, as the trial's are), about half its open-world rate for the
    /// same kills (its 35-70% a kill would be 3-6 for a wave's 8 creatures; every player of the trial gets the whole
    /// reward and the trial can be run again at once), and a trophy's worth of essence at the Rune Table (2 Meadows ... 14
    /// Ashlands, 16 the Deep North); for each boss wave beaten, the boss's own runes in EliteCrafting (its rolls and
    /// guaranteed extras) and its trophy's essence (20 Eikthyr ... 80 Fader, 90 Kall).
    /// </summary>
    internal static class RewardTable
    {
        /// <summary>The highest idol tier: Bloodgold, Kall's; every boss after him gives it again.</summary>
        public const int TopIdolTier = 7;

        /// <summary>Coins: the player's unspent souls over this.</summary>
        public const int SoulsPerCoin = 2;

        /// <summary>Skill XP: the gain again times the waves cleared over this, at most <see cref="MaxXpFactor"/>.</summary>
        public const float WavesForFullXp = 10f;
        public const float MaxXpFactor = 2f;

        /// <summary>Runes for a regular wave cleared, by tier 1-8 (index 0 = Meadows).</summary>
        public static readonly int[] RunesPerWave = { 1, 2, 2, 2, 2, 2, 3, 3 };

        /// <summary>Runes for a boss wave beaten, by the boss's tier: EliteCrafting's boss rune rolls plus its guaranteed
        /// extras (Eikthyr 2 + 3, the Elder 2 + 1, Bonemass 3 + 1, Moder 3 + 1, Yagluth 4 + 1, the Queen 4 + 2, Fader
        /// 5 + 2, Kall as Fader).</summary>
        public static readonly int[] BossRunes = { 5, 3, 4, 4, 5, 6, 7, 7 };

        /// <summary>Essence for a regular wave cleared: one trophy of the biome at the Rune Table.</summary>
        public static readonly int[] EssencePerWave = { 2, 4, 6, 8, 10, 12, 14, 16 };

        /// <summary>Essence for a boss wave beaten: the boss's trophy at the Rune Table.</summary>
        public static readonly int[] BossEssence = { 20, 30, 40, 50, 60, 70, 80, 90 };

        /// <summary>The runes in the order of <see cref="RuneWeights"/>.</summary>
        public static readonly string[] Runes =
        {
            CraftingPrefabs.Awakening, CraftingPrefabs.Recasting, CraftingPrefabs.Ascension, CraftingPrefabs.Cleansing,
            CraftingPrefabs.Sealed,
        };

        /// <summary>Each rune's weight by tier 1-8: EliteCrafting's default economy (its `drops.runes`), the Dvergr Chisel
        /// left out.</summary>
        public static readonly int[][] RuneWeights =
        {
            new[] { 37, 26, 21, 18, 15, 14, 12, 10 },
            new[] { 50, 50, 50, 50, 50, 50, 50, 50 },
            new[] { 0, 10, 12, 13, 13, 12, 12, 11 },
            new[] { 3, 3, 3, 3, 4, 4, 4, 5 },
            new[] { 0, 0, 2, 3, 4, 5, 6, 7 },
        };

        /// <summary>The idol of the trial's <paramref name="boss"/>-th boss (1 for its first): Battle on the odd ones,
        /// Protection on the even ones, at the boss's biome tier (<paramref name="tier"/> 1 Meadows ... 8 Deep North:
        /// idol 0 Wooden ... 7 Bloodgold).</summary>
        public static string IdolFor(int boss, int tier)
        {
            int idol = Mathf.Clamp(tier - 1, 0, TopIdolTier);
            return "Upgrader" + idol + (boss % 2 == 1 ? "Weapon" : "Armor");
        }

        /// <summary>A table index from a tier 1-8.</summary>
        public static int Index(int tier) => Mathf.Clamp(tier - 1, 0, RunesPerWave.Length - 1);
    }
}
