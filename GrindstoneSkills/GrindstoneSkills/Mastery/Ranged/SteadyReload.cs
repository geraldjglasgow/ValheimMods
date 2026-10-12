using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Crossbows' milestone, Steady Reload (section 43): at Crossbows level 50, sprinting, jumping or dodging pauses a
    /// crossbow reload instead of restarting it. Runs on the local player's own client, which owns the player and their
    /// action queue. Read from the game code 2026-10-11:
    /// <list type="bullet">
    /// <item>A reload is a minor action in Player.m_actionQueue whose m_time counts up to its m_duration
    /// (Player.UpdateActionQueue); when it gets there the weapon is loaded.</item>
    /// <item>Player.CheckRun (every fixed step while sprinting with stamina), Player.OnJump and Player.UpdateDodge (as a
    /// roll starts) empty the queue with Player.ClearActionQueue, dropping the reload and its progress.</item>
    /// <item>On the next fixed step the weapon is still not loaded, Player.UpdateWeaponLoading queues a fresh reload
    /// (Player.QueueReloadAction, m_time 0); while sprinting, every step, and each is dropped again.</item>
    /// </list>
    /// The prefix on ClearActionQueue keeps the dropped crossbow reload's weapon and time; the postfix on QueueReloadAction
    /// gives the fresh reload that time back when it is for the same weapon within <see cref="Window"/> seconds. So the
    /// reload waits while the player sprints and goes on from where it was once they stop, land or roll. A reload dropped
    /// any other way (a weapon change through Player.CancelReloadAction, a shot through Player.ResetLoadedWeapon) is not
    /// kept and restarts as in the game, as does every reload with the switch off or below level 50. The loaded state other
    /// players see is still the game's own (ZDOVars.s_weaponLoaded, written by Player.SetWeaponLoaded).
    /// </summary>
    public static class SteadyReload
    {
        /// <summary>How long a dropped reload's progress is kept for the next queued reload, in seconds; the game queues it one fixed step later.</summary>
        private const float Window = 0.5f;

        private static ItemDrop.ItemData keptWeapon;
        private static float keptTime;
        private static float keptAt;

        /// <summary>The perk's tip on the Crossbows page.</summary>
        public static string Tip =>
            "Sprinting, jumping or dodging pauses a crossbow reload instead of restarting it: it goes on from where it was once you stop.";

        [HarmonyPatch(typeof(Player), nameof(Player.ClearActionQueue))]
        private static class Cleared
        {
            [HarmonyPrefix]
            private static void Prefix(Player __instance)
            {
                if (ReferenceEquals(__instance, Player.m_localPlayer) && __instance.m_actionQueue.Count > 0)
                    HookGuard.Run("steady reload", static player => Keep(player), __instance);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.QueueReloadAction))]
        private static class Queued
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                if (keptWeapon != null && ReferenceEquals(__instance, Player.m_localPlayer))
                    HookGuard.Run("steady reload resume", static player => Resume(player), __instance);
            }
        }

        /// <summary>Notes the crossbow reload in the queue about to be emptied, if it had made progress.</summary>
        private static void Keep(Player player)
        {
            Player.MinorActionData reload = Reload(player.m_actionQueue);
            if (reload == null || reload.m_time <= 0f || !RangedMastery.Trains(reload.m_item, Skills.SkillType.Crossbows)
                || !RangedMastery.Active(Skills.SkillType.Crossbows))
                return;
            keptWeapon = reload.m_item;
            keptTime = reload.m_time;
            keptAt = Time.time;
        }

        /// <summary>
        /// Gives the reload just queued the kept progress, when it is for the same weapon and was kept just now. When the
        /// game queued nothing (a reload it blocks for a moment), the progress waits for the next try within the window.
        /// </summary>
        private static void Resume(Player player)
        {
            Player.MinorActionData reload = Reload(player.m_actionQueue);
            if (reload == null)
                return;
            ItemDrop.ItemData weapon = keptWeapon;
            keptWeapon = null;
            if (Time.time - keptAt <= Window && reload.m_item == weapon && reload.m_time <= 0f && RangedMastery.Active(Skills.SkillType.Crossbows))
                reload.m_time = Mathf.Min(keptTime, reload.m_duration);
        }

        private static Player.MinorActionData Reload(List<Player.MinorActionData> queue)
        {
            for (int i = 0; i < queue.Count; i++)
            {
                if (queue[i].m_type == Player.MinorActionData.ActionType.Reload)
                    return queue[i];
            }
            return null;
        }
    }
}
