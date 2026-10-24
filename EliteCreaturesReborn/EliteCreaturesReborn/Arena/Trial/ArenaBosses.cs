using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The boss of each boss wave (features/arena.md section 3, "Bosses"), every fifth: the biome's boss, from the trial's
    /// first biome's to the last biome's, and in the Infinite trial the same boss again, round after round, with more stars. Names checked in the running game
    /// (2026-10-12). Kall comes as <c>FrozenKing</c>; his later phases (<c>FrozenKing_p2</c>, <c>_p3</c>) are made by the
    /// game as each phase dies, and the hall adopts each as the wave's boss (<see cref="ArenaFighter"/>).
    /// </summary>
    internal static class ArenaBosses
    {
        private static readonly string[] Names =
        {
            "Eikthyr", "gd_king", "Bonemass", "Dragon", "GoblinKing", "SeekerQueen", "Fader", "FrozenKing",
        };

        private static readonly string?[] DefeatKeys = new string?[Names.Length];

        /// <summary>The boss prefab's name for a boss wave of the trial: each biome's own, and past the last biome's boss
        /// the bosses from the first biome on again, with more stars.</summary>
        public static string For(TrialSpan span, int wave)
        {
            int boss = Mathf.Max(1, wave / ArenaTable.WavesPerBiome) - 1;
            return Names[span.First + boss % (span.Last - span.First + 1)];
        }

        /// <summary>The global key the game sets when biome <paramref name="index"/>'s boss dies, read once from the boss
        /// prefab ("defeated_bonemass"); empty when the game has no such boss or it sets none.</summary>
        public static string DefeatKey(int index)
        {
            if (index < 0 || index >= Names.Length)
            {
                return "";
            }
            if (DefeatKeys[index] == null && ZNetScene.instance != null)
            {
                GameObject? prefab = ZNetScene.instance.GetPrefab(Names[index]);
                Character? boss = prefab != null ? prefab.GetComponent<Character>() : null;
                DefeatKeys[index] = boss != null ? boss.m_defeatSetGlobalKey ?? "" : "";
            }
            return DefeatKeys[index] ?? "";
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
