using System;
using System.Collections.Generic;
using PatchGuard;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Arena creatures arriving (features/arena.md section 3, "Waves"), on the core's owner, which then owns them, while
    /// fewer than 20 live. Each of the four archways sends its own creatures one at a time, 0.8 to 1.2 seconds apart, out
    /// of its recess under its open portal (<see cref="ArenaSpots.Archway"/>), with the game's spawn sound; a boss comes
    /// on the rune circle at the floor's centre with the game's spawn effect. In the frame it is made, before its
    /// controller's first roll, it is tagged with the trial and its wave (<see cref="ArenaTag"/>), rolled
    /// (<see cref="ArenaRoll"/>) and set hunting the players at once (<see cref="ArenaHunt"/>), so it runs out into the
    /// hall; its <see cref="ArenaFighter"/> goes on with it, so the next count already sees it. One that cannot be made
    /// leaves the queue all the same, logged once per kind, so a wave never waits on it.
    /// </summary>
    internal static class ArenaSpawn
    {
        /// <summary>The game's own creature spawn effect and its sound, for a boss; a creature out of an archway has the
        /// portal to show it and only the sound.</summary>
        private static readonly string[] BossEffects = { "vfx_spawn", "sfx_spawn" };
        private static readonly string[] ArchwayEffects = { "sfx_spawn" };

        private static readonly HashSet<int> Warned = new HashSet<int>();

        /// <summary>Sends what is due, with <paramref name="alive"/> of the trial alive now; how many arrived.</summary>
        public static int Send(ArenaCore core, ArenaQueue queue, int alive)
        {
            ArenaState state = core.State;
            int before = queue.Count, made = 0;
            if (queue.BossDue && alive < ArenaTable.MaxAlive)
            {
                made += Release(state, queue, ArenaQueue.BossLane);
            }
            float now = Time.time;
            for (int lane = 0; lane < ArenaTable.Archways; lane++)
            {
                if (alive + made < ArenaTable.MaxAlive && now >= core.ArchwayNext[lane] && queue.In(lane) > 0)
                {
                    core.ArchwayNext[lane] = now + ArenaTable.ArchwayPace();
                    made += Release(state, queue, lane);
                }
            }
            if (queue.Count != before)
            {
                queue.Write(core.Zdo!);
            }
            return made;
        }

        // The next creature of a lane: 1 when it arrived, 0 when none was due or it could not be made.
        private static int Release(ArenaState state, ArenaQueue queue, int lane)
        {
            if (!queue.Take(lane, out int prefabHash, out int wave))
            {
                return 0;
            }
            GameObject? prefab = Creature(prefabHash);
            if (prefab == null)
            {
                return 0;
            }
            bool boss = lane == ArenaQueue.BossLane;
            ArenaSpots.Spot spot = boss ? ArenaSpots.RuneCircle() : ArenaSpots.Archway(lane);
            try
            {
                Make(prefab, spot, state, wave, boss);
            }
            catch (Exception e)
            {
                WarnOnce(prefabHash, e);
                return 0;
            }
            Effects(spot.Position, boss ? BossEffects : ArchwayEffects);
            return 1;
        }

        // The creature prefab of a queued kind; null (warned once) when this game has none.
        private static GameObject? Creature(int prefabHash)
        {
            GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefabHash) : null;
            if (prefab == null || prefab.GetComponent<Character>() == null)
            {
                WarnOnce(prefabHash, null);
                return null;
            }
            return prefab;
        }

        private static void Make(GameObject prefab, ArenaSpots.Spot spot, ArenaState state, int wave, bool boss)
        {
            GameObject made = Object.Instantiate(prefab, spot.Position, spot.Facing);
            Character? body = made.GetComponent<Character>();
            ZNetView? view = made.GetComponent<ZNetView>();
            ZDO? zdo = view != null ? view.GetZDO() : null;
            if (body == null || zdo == null)
            {
                Object.Destroy(made);
                throw new InvalidOperationException("made no networked creature");
            }
            try
            {
                ArenaTag.Write(zdo, state.TrialId, boss, state.TierOf(wave), wave);
                ArenaRoll.Roll(body, state.Span, wave);
                ArenaHunt.Begin(body);
                made.AddComponent<ArenaFighter>();
            }
            catch
            {
                ZNetScene.instance.Destroy(made); // half made: gone, rather than left in the hall untagged
                throw;
            }
        }

        // The game's own effects and sounds: networked, so every client sees and hears them.
        private static void Effects(Vector3 at, string[] names)
        {
            foreach (string name in names)
            {
                GameObject? effect = ZNetScene.instance.GetPrefab(name);
                if (effect != null)
                {
                    Object.Instantiate(effect, at, Quaternion.identity);
                }
            }
        }

        private static void WarnOnce(int prefabHash, Exception? e)
        {
            if (!Warned.Add(prefabHash))
            {
                return;
            }
            if (e != null)
            {
                Guard.Report(e, $"arena creature {prefabHash}");
                return;
            }
            Util.Log.Warn($"Hel's Trial: no creature prefab with hash {prefabHash} in this game; it is skipped");
        }
    }
}
