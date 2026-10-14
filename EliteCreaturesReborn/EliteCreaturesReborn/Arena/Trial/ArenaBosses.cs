using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The boss of each boss wave (features/arena.md section 3, "Bosses"), every fifth: the biome's boss, from the trial's
    /// first biome's to Kall Fimbulbringer, and after him the same cycle again from the first biome's, with more stars. Names checked in the running game
    /// (2026-10-12). Kall comes as <c>FrozenKing</c>; his later phases (<c>FrozenKing_p2</c>, <c>_p3</c>) are made by the
    /// game as each phase dies, and the hall adopts each as the wave's boss (<see cref="ArenaFighter"/>).
    /// </summary>
    internal static class ArenaBosses
    {
        private static readonly string[] Names =
        {
            "Eikthyr", "gd_king", "Bonemass", "Dragon", "GoblinKing", "SeekerQueen", "Fader", "FrozenKing",
        };

        /// <summary>The boss prefab's name for a boss wave of a trial that began at biome <paramref name="start"/>: each
        /// biome's own, and past the Deep North the bosses from the first biome on again, with more stars.</summary>
        public static string For(int start, int wave)
        {
            int first = Mathf.Clamp(start, 0, Names.Length - 1);
            int boss = Mathf.Max(1, wave / ArenaTable.WavesPerBiome) - 1;
            return Names[first + boss % (Names.Length - first)];
        }

        /// <summary>True when the creature's death brings a boss in its place (a phase of Kall's), read from the death
        /// effects the game plays on its owner as it dies: that death is not the boss's fall.</summary>
        public static bool HasNextPhase(Character boss)
        {
            EffectList.EffectData[]? effects = boss.m_deathEffects != null ? boss.m_deathEffects.m_effectPrefabs : null;
            if (effects == null)
            {
                return false;
            }
            foreach (EffectList.EffectData effect in effects)
            {
                Character? next = effect != null && effect.m_enabled && effect.m_prefab != null
                    ? effect.m_prefab.GetComponent<Character>() : null;
                if (next != null && next.m_boss)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
