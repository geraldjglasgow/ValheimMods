using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Heavy Roll, the level 75 perk, on the local player's own client: the player can dodge while over-encumbered, and
    /// such a roll costs twice the stamina. Read from the game code 2026-10-11:
    /// <list type="bullet">
    /// <item>The game refuses a dodge while Player.IsEncumbered() twice: Player.Dodge does not queue it, and
    /// Player.UpdateDodge does not start it. While either runs for a player Heavy Roll applies to, and only then,
    /// IsEncumbered answers false for that player (<see cref="Lift"/>, dropped in each patch's finalizer), so the game's
    /// own code queues the dodge (with its usual 0.1 Dodge experience) and starts the roll. Movement, stamina
    /// regeneration, jumping and blocking still see the load.</item>
    /// <item>The roll's cost comes from Player.GetDodgeStaminaUse, called inside UpdateDodge for the stamina check and
    /// the spend. The postfix there runs last (after the game's Dodge skill, gear, status effects and Defense's dodge
    /// stamina perk) and doubles it while the encumbrance is lifted. A perfect dodge's refund (RPC_HitWhileDodging) is
    /// the game's share of the ordinary cost.</item>
    /// <item>The Dodge state is entered from Any State in the player's animator, so the roll plays from the
    /// over-encumbered walk as from any other.</item>
    /// </list>
    /// </summary>
    public static class HeavyRoll
    {
        /// <summary>The Dodge level Heavy Roll needs.</summary>
        private const float Level = 75f;

        /// <summary>How many times the ordinary cost a roll under a heavy load costs.</summary>
        private const float CostMultiplier = 2f;

        /// <summary>The player whose encumbrance the dodge path has lifted for the moment, or null.</summary>
        private static Player lifted;

        public static float UnlockLevel => Level;
        public static float Cost => CostMultiplier;

        /// <summary>Lifts the encumbrance check for the dodge path now running, if the player carries too much.</summary>
        internal static void Lift(Player player)
        {
            if (DodgeSkill.Reached(Level) && player.IsEncumbered())
                lifted = player;
        }

        internal static void Drop() => lifted = null;

        [HarmonyPatch(typeof(Player), nameof(Player.Dodge))]
        private static class Queue
        {
            [HarmonyPrefix]
            private static void Prefix(Player __instance)
            {
                if (ReferenceEquals(__instance, Player.m_localPlayer))
                    HookGuard.Run("heavy roll", static player => Lift(player), __instance);
            }

            [HarmonyFinalizer]
            private static void Finalizer() => Drop();
        }

        [HarmonyPatch(typeof(Player), nameof(Player.IsEncumbered))]
        private static class Load
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance, ref bool __result)
            {
                if (__result && ReferenceEquals(__instance, lifted))
                    __result = false;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetDodgeStaminaUse))]
        private static class Stamina
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Player __instance, ref float __result)
            {
                if (ReferenceEquals(__instance, lifted))
                    __result *= CostMultiplier;
            }
        }
    }
}
