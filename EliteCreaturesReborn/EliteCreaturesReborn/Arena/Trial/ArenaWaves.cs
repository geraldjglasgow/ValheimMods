using System.Collections.Generic;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// A wave begins and a wave ends (features/arena.md section 3, "Waves", "Bosses" and "Breaks"), on the core's owner;
    /// a wave is a round. As it begins, its number, its three-minute limit and its creatures go into the core's ZDO. A
    /// regular wave queues its biome's creatures (<see cref="ArenaCreatures"/>) - 6 in the first wave of a biome, two more
    /// each wave within it (6, 8, 10, 12 alone), a quarter more for each player in the hall past the first - its kinds drawn
    /// by weight, each given to an archway in turn, all of the hall's archways round the wall (the next wave starts where
    /// this one stopped, so no side is favoured and players in the middle are come at from every side), and every
    /// archway with creatures to send opens its portal a moment before its first. A boss wave queues the biome's boss alone, for the rune circle
    /// (<see cref="ArenaBosses"/>), and every player of the trial reads which boss Hel sends. As it ends - cleared, or
    /// swept when its time ran out or Hel recalled its stragglers (<see cref="ArenaRecall"/>) - its creatures still
    /// standing vanish, scoring nothing, the players read that it is
    /// cleared, and the break before the next one begins. A wave called early (<see cref="ArenaCalls"/>) begins on top of
    /// the one fighting; the break waits for the last of them (<see cref="ArenaClears"/>).
    /// </summary>
    internal static class ArenaWaves
    {
        /// <summary>The kinds drawn for a wave and the archway each goes to, counted: (kind hash, archway) packed.</summary>
        private static readonly Dictionary<long, int> Picked = new Dictionary<long, int>();

        /// <summary>The countdown or a break is over: the next wave begins, a round of its own.</summary>
        public static void Begin(ArenaCore core, int wave, int players, long now)
        {
            ArenaQueue queue = ArenaQueue.Read(core.Zdo!);
            queue.Clear(); // a round: nothing of the last wave waits
            core.WaveBusyAt.Clear();
            Open(core, queue, wave, players, now);
        }

        /// <summary>A player called the next wave (<see cref="ArenaCalls"/>): it begins now, on top of the one fighting,
        /// whose creatures have all come out. Its five minutes are the time left for every wave fighting.</summary>
        public static void Call(ArenaCore core, int players, long now) =>
            Open(core, ArenaQueue.Read(core.Zdo!), core.State.Wave + 1, players, now);

        private static void Open(ArenaCore core, ArenaQueue queue, int wave, int players, long now)
        {
            ZDO zdo = core.Zdo!;
            ArenaState state = core.State;
            state.Wave = wave;
            state.Phase = ArenaPhase.Fighting;
            ArenaReady.Clear(zdo);
            state.Next = now + ArenaState.Ms(ArenaTable.WaveLimitSeconds);
            int planned = ArenaTable.IsBossWave(wave) ? PlanBoss(zdo, queue, wave) : PlanWave(state, queue, wave, players);
            queue.Write(zdo);
            state.Portals = queue.LaneMask();
            state.Left = ArenaFighters.Alive(state.TrialId) + queue.Count;
            state.WaveSize = state.Left;
            state.RecallAt = 0L;
            OpenArchways(core);
            Heightmap.Biome biome = ArenaCreatures.Biome(ArenaTable.BiomeIndex(state.StartBiome, wave));
            Log.Info($"Hel's Trial: wave {wave} ({biome}), {planned} coming, {players} player(s) in the hall");
        }

        /// <summary>Every wave fighting is over: <paramref name="swept"/> when the time ran out, or Hel recalled the
        /// stragglers, with creatures still standing. They vanish (those this machine owns at once, the rest on their
        /// owners as they read the swept wave), the break begins, and the players read that wave <paramref name="told"/>
        /// (the last to clear) is cleared; after the trial's first, that Hel pays when the trial ends.</summary>
        public static void Rest(ArenaCore core, long now, bool swept, int told)
        {
            ZDO zdo = core.Zdo!;
            ArenaState state = core.State;
            float rest = ArenaTable.BreakAfter(told);
            string words = state.Swept == 0 ? ArenaWords.ClearedFirst : ArenaWords.Cleared;
            state.Swept = state.Wave;
            state.RecallAt = 0L;
            state.Done = 0;
            new ArenaQueue().Write(zdo);
            core.WaveBusyAt.Clear();
            state.Portals = 0;
            state.Left = 0;
            state.Next = now + ArenaState.Ms(rest);
            state.Phase = ArenaPhase.Break;
            ArenaFighters.VanishOwned(state.TrialId);
            ArenaNet.Tell(zdo, words, told.ToString(), Mathf.RoundToInt(rest).ToString());
            Log.Info($"Hel's Trial: wave {told} {(swept ? "swept, its stragglers gone" : "cleared")}; {rest:0} s break");
        }

        private static int PlanWave(ArenaState state, ArenaQueue queue, int wave, int players)
        {
            List<ArenaCreatures.Kind> kinds = ArenaCreatures.For(ArenaTable.BiomeIndex(state.StartBiome, wave));
            if (kinds.Count == 0)
            {
                return 0;
            }
            int count = ArenaTable.CountFor(wave, players);
            int archways = ArenaSpots.ArchwayCount();
            int first = Mathf.Abs(state.SpawnTurn) % archways;
            Picked.Clear();
            Pick(kinds, count, first, archways, ArenaTable.EliteCap(state.StartBiome, wave, players));
            foreach (KeyValuePair<long, int> pick in Picked)
            {
                queue.Add((int)(pick.Key >> 8), wave, (int)(pick.Key & 0xFF), pick.Value);
            }
            Picked.Clear();
            state.SpawnTurn = (first + count) % archways;
            return count;
        }

        // The wave's kinds drawn, each given to the next archway in turn. Elites only while the wave has room for one and
        // never two out of one archway: an elite drawn past that is drawn again from the biome's other kinds.
        private static void Pick(List<ArenaCreatures.Kind> kinds, int count, int first, int archways, int elites)
        {
            int eliteLanes = 0;
            for (int i = 0; i < count; i++)
            {
                int lane = (first + i) % archways;
                bool room = elites > 0 && (eliteLanes & 1 << lane) == 0;
                int prefab = ArenaCreatures.Draw(kinds, room, out bool elite);
                if (elite && room)
                {
                    elites--;
                    eliteLanes |= 1 << lane;
                }
                long key = (long)prefab << 8 | (long)lane;
                Picked[key] = (Picked.TryGetValue(key, out int had) ? had : 0) + 1;
            }
        }

        private static int PlanBoss(ZDO zdo, ArenaQueue queue, int wave)
        {
            string name = ArenaBosses.For(new ArenaState(zdo).StartBiome, wave);
            GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            Character? boss = prefab != null ? prefab.GetComponent<Character>() : null;
            if (boss == null)
            {
                Log.Warn($"Hel's Trial: the game has no boss '{name}' for wave {wave}; the wave is skipped");
                return 0;
            }
            queue.Add(name.GetStableHashCode(), wave, ArenaQueue.BossLane, 1);
            ArenaNet.Tell(zdo, ArenaWords.Boss, boss.m_name);
            return 1;
        }

        // Each archway sends its first creature a moment after its portal opens, the four a little apart.
        private static void OpenArchways(ArenaCore core)
        {
            float now = Time.time;
            for (int i = 0; i < core.ArchwayNext.Length; i++)
            {
                core.ArchwayNext[i] = now + ArenaTable.PortalLead + Random.Range(0f, ArenaTable.ArchwayStagger);
            }
        }
    }
}
