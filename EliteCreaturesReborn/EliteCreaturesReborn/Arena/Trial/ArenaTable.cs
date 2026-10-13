using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Every fixed number of Hel's Trial (features/arena.md section 3: "all fixed numbers in the code"), and the sums made
    /// from them: the wave's biome, how many come, their extra stars and mutation chance, the pace out of the archways,
    /// the break after a wave and the score.
    /// </summary>
    public static class ArenaTable
    {
        /// <summary>Seconds from Hel's waking to the first wave.</summary>
        public const float CountdownSeconds = 10f;

        /// <summary>Arena creatures alive at once at most; the rest wait.</summary>
        public const int MaxAlive = 20;

        /// <summary>The break after a cleared wave, before the next one comes; longer after a boss wave.</summary>
        public const float BreakSeconds = 15f;
        public const float BossBreakSeconds = 20f;

        /// <summary>A wave not cleared within three minutes counts as cleared: its stragglers vanish, scoring nothing.</summary>
        public const float WaveLimitSeconds = 180f;

        /// <summary>Seconds with nothing of the wave alive or still to come before it counts as cleared: a boss whose death
        /// brings its next phase (Kall Fimbulbringer), or a Splintering creature's copies, are gone for a moment before
        /// the next ones join the wave.</summary>
        public const float ClearedSeconds = 2f;

        /// <summary>The archways creatures come out of; each sends one at a time.</summary>
        public const int Archways = 4;

        /// <summary>Seconds between two creatures out of the same archway: a random pace between these.</summary>
        public const float ArchwayPaceMin = 0.8f;
        public const float ArchwayPaceMax = 1.2f;

        /// <summary>Seconds a portal is open before its first creature comes out (it fades in meanwhile), plus up to
        /// <see cref="ArchwayStagger"/> so the four do not send their first at the same instant.</summary>
        public const float PortalLead = 0.6f;
        public const float ArchwayStagger = 0.4f;

        /// <summary>Waves per biome; the last of each is the boss wave.</summary>
        public const int WavesPerBiome = 10;

        /// <summary>Biomes in the trial (Meadows to the Deep North); past them it stays in the last.</summary>
        public const int Biomes = 8;

        /// <summary>Creatures in the first wave of a biome; each later wave of it brings one more.</summary>
        public const int FirstWaveCount = 4;

        /// <summary>A quarter more creatures for each player past the first.</summary>
        public const float PerExtraPlayer = 0.25f;

        /// <summary>The mutation chance: 5% in the first wave, 3% more each wave, at most 60%.</summary>
        public const float MutationStart = 5f;
        public const float MutationStep = 3f;
        public const float MutationMax = 60f;

        /// <summary>No arena creature or boss is given more stars than ECR's maximum (its Extreme difficulty's).</summary>
        public const int MaxStars = Raids.RaidTable.MaxRaiderStars;

        /// <summary>A kill scores 10 x (stars + 1) x the biome number; a boss 500 x the biome number.</summary>
        public const int KillPoints = 10;
        public const int BossPoints = 500;

        /// <summary>Seconds of the owner's clock between steps of the trial: fine enough for the archways' pace.</summary>
        public const float TickSeconds = 0.25f;

        /// <summary>Seconds with no living player in the hall before the trial ends: a hand-over to a new owner, which may
        /// need a moment to see the players, never ends it.</summary>
        public const float EmptySeconds = 5f;

        /// <summary>Seconds between stamps of <see cref="ArenaKeys.SeenAt"/> while players are in the hall.</summary>
        public const float SeenStampSeconds = 1f;

        /// <summary>Creatures waiting at most; a wave planned past this brings fewer.</summary>
        public const int MaxQueued = 120;

        /// <summary>True for a boss wave: every tenth.</summary>
        public static bool IsBossWave(int wave) => wave > 0 && wave % WavesPerBiome == 0;

        /// <summary>The biome's place in the trial for a wave, 0 (Meadows) to 7 (Deep North).</summary>
        public static int BiomeIndex(int wave) => Mathf.Clamp((wave - 1) / WavesPerBiome, 0, Biomes - 1);

        /// <summary>The biome number for scoring, 1 to 8.</summary>
        public static int BiomeNumber(int wave) => BiomeIndex(wave) + 1;

        /// <summary>The wave's place within its biome, 1 to 10 (10 the boss wave); past wave 80 it counts 1 to 10
        /// again.</summary>
        public static int WaveInBiome(int wave) => (Mathf.Max(1, wave) - 1) % WavesPerBiome + 1;

        /// <summary>Waves of ten past the Deep North's, each one more star: 1 for waves 81-90, 2 for 91-100...</summary>
        public static int PastLastBiome(int wave)
        {
            int past = wave - WavesPerBiome * Biomes;
            return past > 0 ? (past - 1) / WavesPerBiome + 1 : 0;
        }

        /// <summary>Extra stars for a creature of this wave: +1 from the 4th wave of each biome, +2 from the 8th, and
        /// one more for every ten waves past 80.</summary>
        public static int ExtraStars(int wave)
        {
            int inBiome = WaveInBiome(wave);
            int biome = inBiome >= 8 ? 2 : inBiome >= 4 ? 1 : 0;
            return biome + PastLastBiome(wave);
        }

        /// <summary>The percent chance that a creature of this wave which rolled no mutation is given one.</summary>
        public static float MutationChance(int wave) =>
            Mathf.Min(MutationMax, MutationStart + MutationStep * (Mathf.Max(1, wave) - 1));

        /// <summary>How many creatures a (regular) wave brings for this many players in the hall.</summary>
        public static int CountFor(int wave, int players)
        {
            int baseCount = FirstWaveCount + WaveInBiome(wave) - 1;
            float scale = 1f + PerExtraPlayer * Mathf.Max(0, players - 1);
            return Mathf.Max(1, Mathf.RoundToInt(baseCount * scale));
        }

        /// <summary>The break after a wave is cleared, in seconds.</summary>
        public static float BreakAfter(int wave) => IsBossWave(wave) ? BossBreakSeconds : BreakSeconds;

        /// <summary>The time until an archway sends its next creature.</summary>
        public static float ArchwayPace() => Random.Range(ArchwayPaceMin, ArchwayPaceMax);

        /// <summary>A kill's score.</summary>
        public static int PointsFor(int stars, int biomeNumber, bool boss) =>
            boss ? BossPoints * biomeNumber : KillPoints * (Mathf.Max(0, stars) + 1) * biomeNumber;
    }
}
