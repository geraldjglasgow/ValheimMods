using System;
using System.Collections.Generic;
using PatchGuard;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Arena creatures arriving (features/arena.md section 3, "Waves"), on the core's owner, which then owns them: a few
    /// each step from the queue, while fewer than 20 live. A creature comes out of the four archways in turn
    /// (<c>spawn_0</c> to <c>_3</c>), a boss on the rune circle at the floor's centre, each with the game's own spawn
    /// effect. In the frame it is made, before its controller's first roll, it is tagged with the trial
    /// (<see cref="ArenaTag"/>), rolled (<see cref="ArenaRoll"/>), and set hunting the players at once
    /// (<see cref="ArenaHunt"/>); its <see cref="ArenaFighter"/> goes on with it, so the next count already sees it. One
    /// that cannot be made leaves the queue all the same, logged once per kind, so a wave never waits on it.
    /// </summary>
    internal static class ArenaSpawn
    {
        /// <summary>The game's own creature spawn effect and its sound.</summary>
        private static readonly string[] SpawnEffects = { "vfx_spawn", "sfx_spawn" };

        /// <summary>Metres around a spawn point a creature may appear, so a wave does not stand in one spot.</summary>
        private const float Scatter = 1.5f;

        private static readonly HashSet<int> Warned = new HashSet<int>();

        /// <summary>Sends up to <paramref name="room"/> (at most a few a step); how many arrived.</summary>
        public static int Send(ArenaCore core, ArenaQueue queue, int room)
        {
            ZDO zdo = core.Zdo!;
            ArenaState state = core.State;
            int made = 0;
            int tries = Mathf.Min(room, ArenaTable.MostPerTick);
            for (int i = 0; i < tries && queue.Take(out int prefab, out int wave); i++)
            {
                made += Arrive(state, prefab, wave) ? 1 : 0;
            }
            queue.Write(zdo);
            return made;
        }

        private static bool Arrive(ArenaState state, int prefabHash, int wave)
        {
            GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefabHash) : null;
            if (prefab == null || prefab.GetComponent<Character>() == null)
            {
                WarnOnce(prefabHash, null);
                return false;
            }
            bool boss = ArenaTable.IsBossWave(wave);
            Vector3 pos = boss ? BossSpot() : NextSpot(state);
            try
            {
                Make(prefab, pos, state.TrialId, wave, boss);
            }
            catch (Exception e)
            {
                WarnOnce(prefabHash, e);
                return false;
            }
            Effects(pos);
            return true;
        }

        private static void Make(GameObject prefab, Vector3 pos, long trial, int wave, bool boss)
        {
            GameObject made = Object.Instantiate(prefab, pos, Facing(pos));
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
                ArenaTag.Write(zdo, trial, boss, ArenaTable.BiomeNumber(wave));
                ArenaRoll.Roll(body, wave);
                ArenaHunt.Begin(body);
                made.AddComponent<ArenaFighter>();
            }
            catch
            {
                ZNetScene.instance.Destroy(made); // half made: gone, rather than left in the hall untagged
                throw;
            }
        }

        // The next archway in turn, a little to one side or the other, on the floor.
        private static Vector3 NextSpot(ArenaState state)
        {
            int count = ArenaKeys.SpawnPoints.Count;
            int turn = (state.SpawnTurn % count + count) % count;
            state.SpawnTurn = (turn + 1) % count;
            bool known = ArenaHall.TryPoint(ArenaKeys.SpawnPoints[turn], out Vector3 point, out _);
            Vector3 spot = known ? point : ArenaHall.Origin;
            Vector2 aside = UnityEngine.Random.insideUnitCircle * Scatter;
            return OnFloor(spot + new Vector3(aside.x, 0f, aside.y));
        }

        // The rune circle at the floor's centre: the throne's own boss_stand is up on the dais.
        private static Vector3 BossSpot() => OnFloor(ArenaHall.Origin);

        private static Vector3 OnFloor(Vector3 at)
        {
            if (ZoneSystem.instance != null && ZoneSystem.instance.FindFloor(at + Vector3.up, out float floor))
            {
                at.y = floor;
            }
            at.y += 0.2f;
            return at;
        }

        // Facing the hall's centre.
        private static Quaternion Facing(Vector3 from)
        {
            Vector3 toward = ArenaHall.Origin - from;
            toward.y = 0f;
            return toward.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toward) : Quaternion.identity;
        }

        // The game's own creature spawn effect and sound: networked effects, so every client sees them.
        private static void Effects(Vector3 at)
        {
            foreach (string name in SpawnEffects)
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
