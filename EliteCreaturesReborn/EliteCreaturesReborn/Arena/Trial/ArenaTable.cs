using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Every fixed number of Hel's Trial (features/arena.md section 3: "all fixed numbers in the code"), and the sums made
    /// from them: the wave's biome, how many come, their extra stars and mutation chance, the pace out of the archways,
    /// the break after a wave, the souls, Hel's Anvil and the boss cache.
    /// </summary>
    public static class ArenaTable
    {
        /// <summary>Seconds from Hel's waking to the first wave.</summary>
        public const float CountdownSeconds = 10f;

        /// <summary>Arena creatures alive at once at most; the rest wait. 24 since the arena went outdoors (2026-10-13:
        /// 130 m across, 8 archways; it was 20 in the 55 m hall).</summary>
        public const int MaxAlive = 24;

        /// <summary>The break after a cleared wave, before the next one comes; longer after a boss wave.</summary>
        public const float BreakSeconds = 20f;
        public const float BossBreakSeconds = 25f;

        /// <summary>A wave not cleared within five minutes counts as cleared: its stragglers vanish, scoring nothing.
        /// Five since the arena went outdoors (it was three): creatures come out of the walls about 63 m from the
        /// centre, and a slow one (a Blob, an Abomination) takes half a minute to get there.</summary>
        public const float WaveLimitSeconds = 300f;

        /// <summary>Seconds with nothing of the wave alive or still to come before it counts as cleared: a boss whose death
        /// brings its next phase (Kall Fimbulbringer), or a Splintering creature's copies, are gone for a moment before
        /// the next ones join the wave.</summary>
        public const float ClearedSeconds = 2f;

        /// <summary>The most archways a hall may have (the outdoor arena's 8, <c>portal_0</c> to <c>_7</c>); each sends
        /// one at a time. A wave uses the archways the built hall has (<see cref="ArenaSpots.ArchwayCount"/>).</summary>
        public const int Archways = 8;

        /// <summary>Seconds between two creatures out of the same archway: a random pace between these.</summary>
        public const float ArchwayPaceMin = 0.8f;
        public const float ArchwayPaceMax = 1.2f;

        /// <summary>Seconds a portal is open before its first creature comes out (it fades in meanwhile), plus up to
        /// <see cref="ArchwayStagger"/> so the four do not send their first at the same instant.</summary>
        public const float PortalLead = 0.6f;
        public const float ArchwayStagger = 0.4f;

        /// <summary>Waves fighting at once at most, as players call the next early (Hel's hover): the room of
        /// <see cref="ArenaState.Done"/>.</summary>
        public const int MaxWavesAtOnce = 30;

        /// <summary>Waves per biome; the last of each is the boss wave.</summary>
        public const int WavesPerBiome = 5;

        /// <summary>Biomes in the trial (Meadows to the Deep North); past them it stays in the last.</summary>
        public const int Biomes = 8;

        /// <summary>Creatures in the first wave of a biome; each later wave of it brings <see cref="MoreEachWave"/> more:
        /// 6, 8, 10, 12 alone, round eight archways, then the boss.</summary>
        public const int FirstWaveCount = 6;
        public const int MoreEachWave = 2;

        /// <summary>A biome's wave from which its creatures have one star more.</summary>
        public const int StarWaveInBiome = 3;

        /// <summary>A kind is an elite (besides a large body) when its base health is over this times its biome's middle
        /// kind's.</summary>
        public const float EliteHealthRatio = 2.5f;

        /// <summary>Elites a wave may hold: none in a biome's first two waves, 1 in its 3rd, 2 in its 4th; past the Deep
        /// North's boss 3, one more every 10 waves, at most 6; and one more for every two players past the first.</summary>
        public const int EliteFromWave = 3;
        public const int EliteEndless = 3;
        public const int EliteEndlessEvery = 10;
        public const int EliteEndlessMax = 6;
        public const int ElitePlayersEach = 2;

        /// <summary>A quarter more creatures for each player past the first.</summary>
        public const float PerExtraPlayer = 0.25f;

        /// <summary>The mutation chance: 15% in the first wave, 4% more each wave, at most 60% (by wave 13).</summary>
        public const float MutationStart = 15f;
        public const float MutationStep = 4f;
        public const float MutationMax = 60f;

        /// <summary>No arena creature or boss is given more stars than ECR's maximum (its Extreme difficulty's).</summary>
        public const int MaxStars = Raids.RaidTable.MaxRaiderStars;

        /// <summary>A kill's souls: 2 x the biome tier x (stars + 1), x 1.5 for each mutation it carries; a boss's 50 x
        /// the tier x (stars + 1) x its aspects' loot multiplier (ECR's own, all of a Bountiful boss's multiplied).</summary>
        public const int SoulsPerTier = 2;
        public const int BossSoulsPerTier = 50;
        public const float SoulsPerMutation = 1.5f;

        /// <summary>Hel's Anvil: 10 souls x the biome tier of the wave being fought or coming, x 1.5 for each repair the
        /// player has already made in this trial.</summary>
        public const int AnvilSoulsPerTier = 10;
        public const float AnvilCostGrowth = 1.5f;

        /// <summary>The boss cache: the chance, when a boss falls, that healing meads drop for the players.</summary>
        public const float CacheChance = 60f;

        /// <summary>Meads per player of the trial in a boss cache: 1 to 3.</summary>
        public const int CacheMeadsMin = 1;
        public const int CacheMeadsMax = 3;

        /// <summary>Metres from the floor's centre within which the cache falls: on the rune circle (22 m across).</summary>
        public const float CacheCircle = 9f;

        /// <summary>Seconds of the owner's clock between steps of the trial: fine enough for the archways' pace.</summary>
        public const float TickSeconds = 0.25f;

        /// <summary>Seconds with no living player in the hall before the trial ends: a hand-over to a new owner, which may
        /// need a moment to see the players, never ends it.</summary>
        public const float EmptySeconds = 5f;

        /// <summary>Seconds between stamps of <see cref="ArenaKeys.SeenAt"/> while players are in the hall.</summary>
        public const float SeenStampSeconds = 1f;

        /// <summary>Creatures waiting at most; a wave planned past this brings fewer.</summary>
        public const int MaxQueued = 120;

        /// <summary>True for a boss wave: every fifth, the last of its biome.</summary>
        public static bool IsBossWave(int wave) => wave > 0 && wave % WavesPerBiome == 0;

        /// <summary>The biome's place for a wave of a trial that began at <paramref name="start"/> (0 Meadows to 7 Deep
        /// North, from the strongest player's gear, <see cref="ArenaPower"/>): five waves each, the Deep North for
        /// good after its boss.</summary>
        public static int BiomeIndex(int start, int wave) =>
            Mathf.Clamp(Mathf.Clamp(start, 0, Biomes - 1) + (Mathf.Max(1, wave) - 1) / WavesPerBiome, 0, Biomes - 1);

        /// <summary>The biome tier, 1 to 8: what souls, rewards and the anvil go by.</summary>
        public static int BiomeNumber(int start, int wave) => BiomeIndex(start, wave) + 1;

        /// <summary>The wave's place within its biome, 1 to 5 (5 the boss wave); past the Deep North's boss it counts 1
        /// to 5 again.</summary>
        public static int WaveInBiome(int wave) => (Mathf.Max(1, wave) - 1) % WavesPerBiome + 1;

        /// <summary>The Deep North's boss wave for a trial that began at <paramref name="start"/>.</summary>
        public static int LastBossWave(int start) => (Biomes - Mathf.Clamp(start, 0, Biomes - 1)) * WavesPerBiome;

        /// <summary>Rounds of five waves past the Deep North's boss, each one more star: 1 for the next five, 2 for the
        /// five after...</summary>
        public static int PastLastBiome(int start, int wave)
        {
            int past = wave - LastBossWave(start);
            return past > 0 ? (past - 1) / WavesPerBiome + 1 : 0;
        }

        /// <summary>The most elites a regular wave holds (<see cref="EliteFromWave"/>); a boss wave is its boss alone.</summary>
        public static int EliteCap(int start, int wave, int players)
        {
            int extra = Mathf.Max(0, players - 1) / ElitePlayersEach;
            int past = wave - LastBossWave(start);
            if (past > 0)
            {
                return Mathf.Min(EliteEndlessMax, EliteEndless + past / EliteEndlessEvery) + extra;
            }
            int inBiome = WaveInBiome(wave);
            return inBiome < EliteFromWave ? 0 : inBiome - EliteFromWave + 1 + extra;
        }

        /// <summary>Extra stars for a creature of this wave: +1 from the 3rd wave of each biome, and one more for every
        /// five waves past the Deep North's boss.</summary>
        public static int ExtraStars(int start, int wave) =>
            (WaveInBiome(wave) >= StarWaveInBiome ? 1 : 0) + PastLastBiome(start, wave);

        /// <summary>The percent chance that a creature of this wave which rolled no mutation is given one.</summary>
        public static float MutationChance(int wave) =>
            Mathf.Min(MutationMax, MutationStart + MutationStep * (Mathf.Max(1, wave) - 1));

        /// <summary>How many creatures a (regular) wave brings for this many players in the hall.</summary>
        public static int CountFor(int wave, int players)
        {
            int baseCount = FirstWaveCount + MoreEachWave * (WaveInBiome(wave) - 1);
            float scale = 1f + PerExtraPlayer * Mathf.Max(0, players - 1);
            return Mathf.Max(1, Mathf.RoundToInt(baseCount * scale));
        }

        /// <summary>The break after a wave is cleared, in seconds.</summary>
        public static float BreakAfter(int wave) => IsBossWave(wave) ? BossBreakSeconds : BreakSeconds;

        /// <summary>The time until an archway sends its next creature.</summary>
        public static float ArchwayPace() => Random.Range(ArchwayPaceMin, ArchwayPaceMax);

        /// <summary>A creature's souls: <paramref name="aspectFactor"/> counts for a boss, <paramref name="mutations"/>
        /// for any other.</summary>
        public static int SoulsFor(int stars, int tier, bool boss, int mutations, float aspectFactor)
        {
            float souls = (boss ? BossSoulsPerTier : SoulsPerTier) * Mathf.Max(1, tier) * (Mathf.Max(0, stars) + 1f);
            souls *= boss ? Mathf.Max(0f, aspectFactor) : Mathf.Pow(SoulsPerMutation, Mathf.Max(0, mutations));
            return Mathf.RoundToInt(souls);
        }

        /// <summary>What Hel's Anvil asks of a player now.</summary>
        public static int AnvilCost(int tier, int repairs) =>
            Mathf.RoundToInt(AnvilSoulsPerTier * Mathf.Max(1, tier) * Mathf.Pow(AnvilCostGrowth, Mathf.Max(0, repairs)));
    }
}
