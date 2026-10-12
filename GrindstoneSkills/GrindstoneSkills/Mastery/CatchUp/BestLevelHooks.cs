using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Keeps <see cref="BestLevels"/> in step with the local player's skills. Every hook runs on the player's own client,
    /// where the game keeps their skills and custom data, and touches nothing on another player. Read from the game
    /// code 2026-10-11:
    /// <list type="bullet">
    /// <item>Game.SpawnPlayer makes the player local, loads the character (Player.Load) and then calls
    /// Player.OnSpawned: the moment the best levels are read, or seeded.</item>
    /// <item>Skills.RaiseSkill calls Player.OnSkillLevelup only when a skill gains a level, with the new stored level,
    /// for the game's skills and the mod's own: the one place a best level can rise in play.</item>
    /// <item>The console's raiseskill (Skills.CheatRaiseSkill, and <see cref="CustomSkillCheats"/> for the mod's own
    /// skills) sets levels without a level-up, so the best levels are raised to match afterwards. A negative amount
    /// lowers the level and keeps the best, so Catch Up can be tried that way.</item>
    /// <item>The console's resetskill calls Skills.ResetSkill for each skill it resets (all of them for "all"), and
    /// resetcharacter calls Player.ResetCharacter: both forget the best levels too, so a deliberate reset starts over
    /// instead of leaving a long catch-up. Nothing else in the game calls either.</item>
    /// </list>
    /// Dying (Skills.OnDeath), and a world that resets skills on death (Skills.Clear), keep the best levels: that is
    /// the loss Catch Up softens.
    /// </summary>
    public static class BestLevelHooks
    {
        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        private static class Spawned
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                if (LocalSkill.IsLocal(__instance))
                    HookGuard.Run("best levels", static player => BestLevels.Take(player), __instance);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnSkillLevelup))]
        private static class LevelUp
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance, Skills.SkillType skill, float level)
            {
                if (LocalSkill.IsLocal(__instance))
                    HookGuard.Run("best level", static up => BestLevels.Note(up.player, up.skill, up.level), (player: __instance, skill, level));
            }
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.CheatRaiseSkill))]
        private static class CheatRaise
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Skills __instance)
            {
                if (LocalSkill.IsLocal(__instance.m_player))
                    HookGuard.Run("best levels raiseskill", static player => BestLevels.CatchCurrent(player), __instance.m_player);
            }
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.ResetSkill))]
        private static class Reset
        {
            [HarmonyPostfix]
            private static void Postfix(Skills __instance, Skills.SkillType skillType)
            {
                if (LocalSkill.IsLocal(__instance.m_player))
                    HookGuard.Run("best levels resetskill", static reset => BestLevels.Forget(reset.player, reset.skillType), (player: __instance.m_player, skillType));
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.ResetCharacter))]
        private static class ResetCharacter
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                if (LocalSkill.IsLocal(__instance))
                    HookGuard.Run("best levels resetcharacter", static player => BestLevels.ForgetAll(player), __instance);
            }
        }
    }
}
