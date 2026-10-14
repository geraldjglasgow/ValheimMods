using EliteCreaturesReborn.Loot;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// Souls (features/arena.md section 3, "Souls"): each kill is worth 2 x the biome tier x (stars + 1), x 1.5 for each
    /// mutation it carries; a boss 50 x the tier x (stars + 1) x its aspects' loot multiplier (<see cref="AspectLoot"/>,
    /// ECR's own value, all of a Bountiful boss's multiplied). Weighed on the dying creature's owner as the game's death
    /// begins, while its ZDO still answers, and sent to the core's owner (one RPC, or a direct call when the same machine
    /// owns both), which adds them to the trial it names, if that trial still runs: every player of the trial gets them
    /// in full (<see cref="SoulWallets"/>). A copy - a Phantom boss's, an Echo, a Cloning creature's decoy - is worth
    /// nothing and is no kill, nor is the death of a boss phase that brings the next (Kall's first two), nor a creature
    /// the river under the arena drowned (flown out over the walls and fallen in). A boss that falls may leave a cache of
    /// healing meads (<see cref="BossCache"/>).
    /// </summary>
    internal static class ArenaSouls
    {
        /// <summary>The river's surface under the arena, in arena coordinates (the hall's lake).</summary>
        private const float RiverSurface = -8f;

        /// <summary>A dying arena creature, on its owner.</summary>
        public static void Report(Character body, ZDO zdo)
        {
            long trial = ArenaTag.Trial(zdo);
            ArenaCore? core = ArenaCore.Current;
            ZDO? coreZdo = core != null ? core.Zdo : null;
            if (trial == 0L || coreZdo == null)
            {
                return;
            }
            EliteController? elite = body.GetComponent<EliteController>();
            CreatureTraits? traits = elite != null && elite.Ready ? elite.Traits : null;
            bool kill = Counts(body, traits);
            int souls = kill ? Worth(body, elite, traits, ArenaTag.Tier(zdo)) : 0;
            Send(core!, trial, souls, kill);
            if (kill && body.IsBoss())
            {
                BossCache.Drop(body, ArenaTag.Tier(zdo), coreZdo);
            }
        }

        /// <summary>The core's owner adds a kill's souls to its trial; nothing when that trial is no longer the one
        /// running.</summary>
        public static void Add(ArenaCore core, long trial, int souls, bool kill)
        {
            ArenaState state = core.State;
            if (!kill || !state.Running || state.TrialId != trial)
            {
                return;
            }
            state.Kills += 1;
            state.Souls += Mathf.Max(0, souls);
        }

        private static void Send(ArenaCore core, long trial, int souls, bool kill)
        {
            if (core.IsOwner)
            {
                Add(core, trial, souls, kill);
            }
            else
            {
                core.View.InvokeRPC(ArenaKeys.KillRpc, trial, souls, kill);
            }
        }

        private static int Worth(Character body, EliteController? elite, CreatureTraits? traits, int tier)
        {
            int stars = traits != null ? traits.Stars : Mathf.Max(0, body.GetLevel() - 1);
            int mutations = traits != null ? Mutations(traits.Mask) : 0;
            float aspects = elite != null && traits != null ? AspectLoot.Factor(elite) : 1f;
            return ArenaTable.SoulsFor(stars, tier, body.IsBoss(), mutations, aspects);
        }

        private static int Mutations(int mask)
        {
            int count = 0;
            for (int bits = mask; bits != 0; bits &= bits - 1)
            {
                count++;
            }
            return count;
        }

        private static bool Counts(Character body, CreatureTraits? traits)
        {
            if (traits != null && (traits.PhantomCopy || traits.Echo || traits.Decoy))
            {
                return false;
            }
            return !Drowned(body) && (!body.IsBoss() || !ArenaBosses.HasNextPhase(body));
        }

        // The river under the arena kills whatever falls in (the hall's own lake, a drowning hit, or a death below its
        // surface): the creature is gone from its wave, but nobody killed it.
        private static bool Drowned(Character body)
        {
            HitData? last = body.m_lastHit;
            return (last != null && last.m_hitType == HitData.HitType.Drowning)
                || body.transform.position.y < ArenaHall.Origin.y + RiverSurface;
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
                Patches.SafeCall.Run("Character.OnDeath arena souls", static (body, data) => ArenaSouls.Report(body, data),
                    __instance, zdo!);
            }
        }
    }
}
