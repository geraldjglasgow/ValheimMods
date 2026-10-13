using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The boss of each boss wave (features/arena.md section 3, "Bosses"), every tenth: the biome's boss, Eikthyr to Kall
    /// Fimbulbringer, and from wave 90 the same cycle again, with more stars. Names checked in the running game
    /// (2026-10-12). Kall comes as <c>FrozenKing</c>; his later phases (<c>FrozenKing_p2</c>, <c>_p3</c>) are made by the
    /// game as each phase dies, and the hall adopts each as the wave's boss (<see cref="ArenaFighter"/>).
    /// </summary>
    internal static class ArenaBosses
    {
        private static readonly string[] Names =
        {
            "Eikthyr", "gd_king", "Bonemass", "Dragon", "GoblinKing", "SeekerQueen", "Fader", "FrozenKing",
        };

        /// <summary>The boss prefab's name for a boss wave.</summary>
        public static string For(int wave) => Names[(Mathf.Max(1, wave / ArenaTable.WavesPerBiome) - 1) % Names.Length];

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
