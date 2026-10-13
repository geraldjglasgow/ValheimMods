using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// The score (features/arena.md section 3, "Score"): each kill 10 x (stars + 1) x the biome number, a boss 500 x the
    /// biome number. Weighed on the dying creature's owner as the game's death begins, while its ZDO still answers, and
    /// sent to the core's owner (one RPC, or a direct call when the same machine owns both), which adds it to the trial it
    /// names, if that trial still runs. A copy - a Phantom boss's, an Echo, a Cloning creature's decoy - scores nothing and
    /// is no kill, nor is the death of a boss phase that brings the next (Kall's first two).
    /// </summary>
    internal static class ArenaScore
    {
        /// <summary>A dying arena creature, on its owner.</summary>
        public static void Report(Character body, ZDO zdo)
        {
            long trial = ArenaTag.Trial(zdo);
            ArenaCore? core = ArenaCore.Current;
            if (trial == 0L || core == null || core.Zdo == null)
            {
                return;
            }
            bool kill = Counts(body);
            int points = kill ? ArenaTable.PointsFor(Stars(body), ArenaTag.Tier(zdo), body.IsBoss()) : 0;
            if (core.IsOwner)
            {
                Add(core, trial, points, kill);
            }
            else
            {
                core.View.InvokeRPC(ArenaKeys.KillRpc, trial, points, kill);
            }
        }

        /// <summary>The core's owner adds a kill to its trial; nothing when that trial is no longer the one running.</summary>
        public static void Add(ArenaCore core, long trial, int points, bool kill)
        {
            ArenaState state = core.State;
            if (!kill || !state.Running || state.TrialId != trial)
            {
                return;
            }
            state.Kills += 1;
            state.Score += Mathf.Max(0, points);
        }

        private static bool Counts(Character body)
        {
            EliteController? elite = body.GetComponent<EliteController>();
            CreatureTraits? traits = elite != null && elite.Ready ? elite.Traits : null;
            if (traits != null && (traits.PhantomCopy || traits.Echo || traits.Decoy))
            {
                return false;
            }
            return !body.IsBoss() || !ArenaBosses.HasNextPhase(body);
        }

        // The mod's stars, or the game's level when the mod gives this creature none.
        private static int Stars(Character body)
        {
            EliteController? elite = body.GetComponent<EliteController>();
            return elite != null && elite.Ready ? elite.Traits.Stars : Mathf.Max(0, body.GetLevel() - 1);
        }
    }

    /// <summary>
    /// Every death passes here on the dying creature's owner (the only machine the game's death runs on), at its start,
    /// while the ZDO is whole: one ZDO read tells an arena creature from the rest. Never throws into the death.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
    internal static class ArenaDeathPatch
    {
        private static void Prefix(Character __instance)
        {
            ZNetView nview = __instance.m_nview;
            ZDO? zdo = nview != null ? nview.GetZDO() : null;
            if (ArenaTag.IsArena(zdo) && nview!.IsOwner())
            {
                Patches.SafeCall.Run("Character.OnDeath arena score", static (body, data) => ArenaScore.Report(body, data),
                    __instance, zdo!);
            }
        }
    }
}
