using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// The one Player.UpdateDodge patch behind the Dodging perks. The game runs UpdateDodge every physics step, only on
    /// the player's own client and only while they are alive (Player.FixedUpdate), so every other player returns at the
    /// first line. It counts down the dodge the player queued (Player.Dodge, 0.5 s) and starts the roll when it can: the
    /// timer is set to exactly 0, m_dodgeInvincible is set and the stamina is spent (GetDodgeStaminaUse). Read from the
    /// game code 2026-10-11. Before it runs, <see cref="LongRoll"/> ends an extension whose time is up and
    /// <see cref="HeavyRoll"/> lifts the encumbrance check for a roll that may start; after it, a roll that started is
    /// measured by Long Roll. The lift is dropped in a finalizer, so it never outlives the call.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateDodge))]
    public static class RollHooks
    {
        [HarmonyPrefix]
        private static void Prefix(Player __instance, float dt, out bool __state)
        {
            __state = false;
            if (ReferenceEquals(__instance, Player.m_localPlayer))
                __state = HookGuard.Run("dodge update", static args => Before(args.player, args.dt), (player: __instance, dt), false);
        }

        [HarmonyPostfix]
        private static void Postfix(Player __instance, bool __state)
        {
            if (__state && __instance.m_queuedDodgeTimer == 0f)
                HookGuard.Run("long roll start", static player => LongRoll.OnRollStart(player), __instance);
        }

        [HarmonyFinalizer]
        private static void Finalizer() => HeavyRoll.Drop();

        /// <summary>Whether a queued dodge may start in this step: the game's own test, before its countdown.</summary>
        private static bool Before(Player player, float dt)
        {
            LongRoll.Tick(player);
            bool pending = player.m_queuedDodgeTimer - dt > 0f;
            if (pending)
                HeavyRoll.Lift(player);
            return pending;
        }
    }
}
