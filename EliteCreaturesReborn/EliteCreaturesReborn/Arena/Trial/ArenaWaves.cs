using System.Collections.Generic;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// A wave begins (features/arena.md section 3, "Waves" and "Bosses"), on the core's owner: its number, its timer and
    /// its creatures go into the core's ZDO. A regular wave queues its biome's creatures (<see cref="ArenaCreatures"/>) -
    /// 4 in the first wave of a biome, one more each wave within it, a quarter more for each player in the hall past the
    /// first - its kinds drawn by weight; whatever the last wave left waiting comes first. A boss wave queues the biome's
    /// boss alone (<see cref="ArenaBosses"/>), and every player of the trial reads which boss Hel sends.
    /// </summary>
    internal static class ArenaWaves
    {
        private static readonly Dictionary<int, int> Picked = new Dictionary<int, int>();

        public static void Begin(ArenaCore core, int wave, int players, long now)
        {
            ZDO zdo = core.Zdo!;
            ArenaState state = core.State;
            state.Wave = wave;
            state.Phase = ArenaPhase.Fighting;
            state.Next = now + ArenaState.Ms(ArenaTable.WaveSeconds(wave));
            ArenaQueue queue = ArenaQueue.Read(zdo);
            int planned = ArenaTable.IsBossWave(wave) ? PlanBoss(zdo, queue, wave) : PlanWave(queue, wave, players);
            queue.Write(zdo);
            state.Left = Mathf.Max(state.Left, queue.Count);
            core.BossSeenAt = Time.time;
            Log.Info($"Hel's Trial: wave {wave} ({ArenaCreatures.Biome(ArenaTable.BiomeIndex(wave))}), {planned} coming, "
                + $"{queue.Count} waiting in all, {players} player(s) in the hall");
        }

        private static int PlanWave(ArenaQueue queue, int wave, int players)
        {
            List<ArenaCreatures.Kind> kinds = ArenaCreatures.For(ArenaTable.BiomeIndex(wave));
            if (kinds.Count == 0)
            {
                return 0;
            }
            int count = ArenaTable.CountFor(wave, players);
            Picked.Clear();
            for (int i = 0; i < count; i++)
            {
                int kind = ArenaCreatures.Draw(kinds);
                Picked[kind] = (Picked.TryGetValue(kind, out int had) ? had : 0) + 1;
            }
            foreach (KeyValuePair<int, int> pick in Picked)
            {
                queue.Add(pick.Key, wave, pick.Value);
            }
            Picked.Clear();
            return count;
        }

        private static int PlanBoss(ZDO zdo, ArenaQueue queue, int wave)
        {
            string name = ArenaBosses.For(wave);
            GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            Character? boss = prefab != null ? prefab.GetComponent<Character>() : null;
            if (boss == null)
            {
                Log.Warn($"Hel's Trial: the game has no boss '{name}' for wave {wave}; the wave is skipped");
                return 0;
            }
            queue.Add(name.GetStableHashCode(), wave, 1);
            ArenaNet.Tell(zdo, ArenaWords.Boss, boss.m_name);
            return 1;
        }
    }
}
