using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The biomes a trial fights (features/arena.md section 3, "Trials"): from <see cref="First"/> to <see cref="Last"/>
    /// (0 Meadows to 7 the Deep North), five waves each, the fifth its boss. A biome's trial is that biome alone and ends
    /// with its boss; the Infinite trial is the highest biome the world has opened, round after round, each round a level
    /// harder (<see cref="Endless"/>, <see cref="ArenaTable.Level"/>). Chosen at Helgrind (<see cref="ArenaModes"/>) and
    /// kept in the core's ZDO as the trial begins.
    /// </summary>
    public readonly struct TrialSpan
    {
        public TrialSpan(int first, int last, bool endless)
        {
            First = Mathf.Clamp(first, 0, ArenaTable.Biomes - 1);
            Last = Mathf.Clamp(last, First, ArenaTable.Biomes - 1);
            Endless = endless;
        }

        /// <summary>The first biome, 0 (Meadows) to 7 (the Deep North).</summary>
        public int First { get; }

        /// <summary>The last biome; after its boss the trial ends, or with <see cref="Endless"/> goes on in it.</summary>
        public int Last { get; }

        /// <summary>True for the Infinite trial: past the last biome's boss its rounds come again, a level harder each.</summary>
        public bool Endless { get; }

        /// <summary>The boss wave of the last biome.</summary>
        public int LastBossWave => (Last - First + 1) * ArenaTable.WavesPerBiome;

        /// <summary>True when <paramref name="wave"/> is the trial's last: a biome's trial ends with its boss wave.</summary>
        public bool IsFinal(int wave) => !Endless && wave >= LastBossWave;
    }
}
