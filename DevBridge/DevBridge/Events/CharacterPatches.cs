using System;
using System.Collections.Generic;
using DevBridge.Hitbox;
using DevBridge.Server;
using HarmonyLib;

namespace DevBridge.Events
{
    /// <summary>Where the character events come from. Each patch only reads, is cheap, and hands its work to
    /// Publish.Safely, so a failure is logged and the game's own code goes on. Until the first /events or /scenario call
    /// (EventLog.Recording) each leaves on one flag test; after it, a call with nothing to record leaves before any work
    /// and allocates nothing (static lambdas, arguments passed in), and an event allocates only its own data.</summary>
    internal static class CharacterPatches
    {
        // RPC_Damage decides a hit on the target's owner; the total is kept here before block and resistances cut it.
        // The one RPC_Damage patch: /hitbox's record of a hit on the local player is asked from here too.
        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        private static class Arrive
        {
            private static void Prefix(Character __instance, HitData hit)
            {
                if (hit == null) return;
                if (EventLog.Recording) Publish.Safely("hit", static (c, h) => CharacterEvents.Arrive(c, h), __instance, hit);
                if (HitboxView.On) HitboxPatches.Arrived(__instance, hit);
            }
        }

        // ApplyDamage takes the health, for RPC_Damage and for burning, poison and smoke ticks alike. The game sets
        // m_lastHit to the hit only once it is past every early return, so that is the sign the damage landed.
        [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
        private static class Apply
        {
            private static void Prefix(Character __instance, out HitData __state) => __state = __instance.m_lastHit;

            private static void Postfix(Character __instance, HitData hit, HitData __state)
            {
                if (!EventLog.Recording || hit == null || __instance.m_lastHit != hit || ReferenceEquals(__state, hit)) return;
                Publish.Safely("hit", static (c, h) => CharacterEvents.Hit(c, h), __instance, hit);
            }
        }

        // Every character but the player; with a death animation the game calls it on every machine, so only the owner tells.
        [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        private static class Died
        {
            private static void Prefix(Character __instance)
            {
                if (EventLog.Recording) Publish.Safely("death", static c =>
                {
                    if (c.IsOwner()) CharacterEvents.Death(c);
                }, __instance);
            }
        }

        // Player.OnDeath replaces Character.OnDeath without calling it.
        [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
        private static class PlayerDied
        {
            private static void Prefix(Player __instance)
            {
                if (EventLog.Recording) Publish.Safely("death", static p =>
                {
                    if (!p.IsOwner()) return;
                    CharacterEvents.Death(p);
                    PlayerEvents.Died(p);
                }, __instance);
            }
        }

        // Every Awake chain reaches Character.Awake. ZNetScene sets m_useInitZDO while it makes an object for an existing
        // ZDO (loaded from the world or from another machine); otherwise this machine created it. The event waits for the
        // plugin's next Update because a spawner sets the level just after Instantiate returns: the characters wait in a
        // list, and one cached action posted for the whole list reads them.
        [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
        private static class Appeared
        {
            private static readonly List<(Character Who, bool Loaded)> Waiting = new List<(Character, bool)>();
            private static readonly Action Flush = FlushWaiting;

            private static void Postfix(Character __instance)
            {
                if (EventLog.Recording) Publish.Safely("spawn", static c => Later(c), __instance);
            }

            private static void Later(Character character)
            {
                if (character.m_nview == null || character.m_nview.GetZDO() == null) return;
                if (Waiting.Count == 0) MainThread.Post(Flush);
                Waiting.Add((character, ZNetView.m_useInitZDO));
            }

            private static void FlushWaiting()
            {
                for (int i = 0; i < Waiting.Count; i++)
                    Publish.Safely("spawn", static (c, loaded) => CharacterEvents.Spawn(c, loaded), Waiting[i].Who, Waiting[i].Loaded);
                Waiting.Clear();
            }
        }

        // Called on the owner when its AI notices (or forgets) a target; only a boss newly alerted is told.
        [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.SetAlerted))]
        private static class Alerted
        {
            private static void Prefix(BaseAI __instance, out bool __state) => __state = __instance.m_alerted;

            private static void Postfix(BaseAI __instance, bool __state)
            {
                if (!EventLog.Recording || __state || !__instance.m_alerted) return;
                Character boss = __instance.m_character;
                if (boss && boss.IsBoss()) Publish.Safely("boss", static c => CharacterEvents.Boss("alerted", c), boss);
            }
        }
    }
}
