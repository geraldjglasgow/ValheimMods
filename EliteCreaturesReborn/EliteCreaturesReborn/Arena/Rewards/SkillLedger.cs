using System.Collections.Generic;
using EliteCreaturesReborn.Arena.Trial;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Rewards
{
    /// <summary>
    /// The skill XP the local player gains during a trial, per skill (features/arena.md section 3, "Rewards"), kept on the
    /// player's own machine for the bonus paid at the trial's end (<see cref="SkillBonus"/>). Every raise through the
    /// game's own path, <c>Skills.RaiseSkill</c>, passes here while the player is in the built hall and a trial runs:
    /// its skill and its factor are added up, with the number of raises. The factor is read first of every hook
    /// (<c>Priority.First</c>), before any mod multiplies it, so the bonus, raised the same way, is multiplied by them
    /// once as the gain was. A raise runs often (every hit, every stride of a run): the first tests are a flag and a
    /// field, then a distance; the trial is read only for a raise in the hall.
    /// </summary>
    [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
    internal static class SkillLedger
    {
        private static readonly Dictionary<Skills.SkillType, Vector2> Gains = new Dictionary<Skills.SkillType, Vector2>();
        private static long _trial;

        /// <summary>Each skill's gain in trial <paramref name="trial"/>: x the factors added up, y the raises; empty for
        /// any other trial.</summary>
        public static IReadOnlyDictionary<Skills.SkillType, Vector2> For(long trial)
        {
            if (trial != _trial)
            {
                Gains.Clear();
            }
            return Gains;
        }

        /// <summary>The bonus is paid: nothing more is owed for this trial.</summary>
        public static void Forget()
        {
            Gains.Clear();
            _trial = 0L;
        }

        [HarmonyPriority(Priority.First)]
        private static void Prefix(Skills __instance, Skills.SkillType skillType, float factor)
        {
            Player player = __instance.m_player;
            if (!ArenaHall.Built || player == null || player != Player.m_localPlayer
                || !ArenaHall.Contains(player.transform.position) || factor <= 0f)
            {
                return;
            }
            Patches.SafeCall.Run("arena skill ledger", static raise => Note(raise.skillType, raise.factor), (skillType, factor));
        }

        private static void Note(Skills.SkillType skill, float factor)
        {
            ArenaCore? core = ArenaCore.Current;
            long trial = ArenaState.RunningTrial(core != null ? core.Zdo : null);
            if (trial == 0L || skill == Skills.SkillType.None)
            {
                return;
            }
            if (trial != _trial)
            {
                Gains.Clear(); // a new trial: what an earlier one left unpaid is gone
                _trial = trial;
            }
            Gains.TryGetValue(skill, out Vector2 had);
            Gains[skill] = new Vector2(had.x + factor, had.y + 1f);
        }
    }
}
