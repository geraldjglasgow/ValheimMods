using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Long Roll, on the local player's own client: a roll's invulnerability lasts up to 30% longer at Dodge 100,
    /// linear from nothing at 0. Read from the game code and the player's animator 2026-10-11:
    /// <list type="bullet">
    /// <item>Player.UpdateDodge sets m_dodgeInvincible when the roll starts, and the roll clip's DodgeMortal event
    /// (CharacterAnimEvent, Player.OnDodgeMortal) clears it. The window's length comes from the animation (the Dodge
    /// state plays the Dive clip at speed 1.7: about 0.41 s untouchable of a 0.88 s roll), so it is measured on every
    /// roll, from <see cref="RollHooks"/> seeing the roll start to that event, and the extra share is kept on after it.</item>
    /// <item>The game only counts the flag while the dodge animation plays (UpdateDodge writes flag and animation into
    /// m_dodgeInvincibleCached and the player's ZDO, which attackers on other machines read through
    /// IsDodgeInvincible), so the extension can never outlast the roll itself, and other players and creature owners
    /// see it as they see the game's window.</item>
    /// <item>The extension ends when its time is up, when a new roll starts (the game sets the flag for that roll),
    /// while the player teleports, when the switch turns off, and with the player object: a dead player's object is
    /// replaced at respawn and nothing carries over to the new one.</item>
    /// </list>
    /// </summary>
    public static class LongRoll
    {
        /// <summary>The extra share of the window at level 100.</summary>
        private const float ExtraAt100 = 0.3f;

        /// <summary>A measured window longer than this is no roll's (its start was missed), and that roll stays the game's.</summary>
        private const float LongestWindow = 1.5f;

        private static Player roller;
        private static float rollStart = float.NegativeInfinity;
        private static float extendedUntil = float.NegativeInfinity;

        /// <summary>How long the local player's last roll was untouchable in the game's own window, in seconds, or 0.</summary>
        public static float LastWindow { get; private set; }

        /// <summary>How much longer a roll stays untouchable at a Dodge factor (0..1), as a share of the game's window.</summary>
        public static float ExtraShare(float factor) => ExtraAt100 * Mathf.Clamp01(factor);

        /// <summary>The roll the local player just started: measure its window, and forget any extension of the last.</summary>
        internal static void OnRollStart(Player player)
        {
            roller = player;
            rollStart = Time.time;
            extendedUntil = float.NegativeInfinity;
        }

        /// <summary>Each physics step, before the game's dodge update: end an extension that is over.</summary>
        internal static void Tick(Player player)
        {
            if (!ReferenceEquals(roller, player))
            {
                roller = null;
                rollStart = float.NegativeInfinity;
                extendedUntil = float.NegativeInfinity;
                return;
            }
            if (rollStart > float.NegativeInfinity && Time.time - rollStart > LongestWindow)
                rollStart = float.NegativeInfinity;
            if (extendedUntil > float.NegativeInfinity && (Time.time >= extendedUntil || !DodgeSkill.Active || player.IsTeleporting()))
            {
                extendedUntil = float.NegativeInfinity;
                player.m_dodgeInvincible = false;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnDodgeMortal))]
        private static class Mortal
        {
            /// <summary>Whether the game had made this roll untouchable: a landing roll, or one already ended, never is.</summary>
            [HarmonyPrefix]
            private static void Prefix(Player __instance, out bool __state) => __state = __instance.m_dodgeInvincible;

            [HarmonyPostfix]
            private static void Postfix(Player __instance, bool __state)
            {
                if (!__state)
                    rollStart = float.NegativeInfinity;
                else if (rollStart > float.NegativeInfinity && ReferenceEquals(__instance, roller))
                    HookGuard.Run("long roll", static player => Extend(player), __instance);
            }
        }

        /// <summary>The game has just ended the roll's invulnerability: put it back on for the extra share.</summary>
        private static void Extend(Player player)
        {
            float window = Time.time - rollStart;
            rollStart = float.NegativeInfinity;
            if (window <= 0f || window > LongestWindow || !ReferenceEquals(player, Player.m_localPlayer))
                return;
            LastWindow = window;
            float extra = DodgeSkill.Active ? window * ExtraShare(DodgeSkill.Factor) : 0f;
            if (extra <= 0f || player.IsTeleporting())
                return;
            extendedUntil = Time.time + extra;
            player.m_dodgeInvincible = true;
        }
    }
}
