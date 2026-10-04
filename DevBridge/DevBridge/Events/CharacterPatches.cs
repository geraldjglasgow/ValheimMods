using DevBridge.Server;
using HarmonyLib;

namespace DevBridge.Events
{
    /// <summary>Where the character events come from. Each patch only reads, is cheap, and hands its work to
    /// Publish.Safely, so a failure is logged and the game's own code goes on.</summary>
    internal static class CharacterPatches
    {
        // RPC_Damage decides a hit on the target's owner; the total is kept here before block and resistances cut it.
        [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
        private static class Arrive
        {
            private static void Prefix(Character __instance, HitData hit)
            {
                if (hit != null) Publish.Safely("hit", () => CharacterEvents.Arrive(__instance, hit));
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
                if (hit == null || __instance.m_lastHit != hit || ReferenceEquals(__state, hit)) return;
                Publish.Safely("hit", () => CharacterEvents.Hit(__instance, hit));
            }
        }

        // Every character but the player; with a death animation the game calls it on every machine, so only the owner tells.
        [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        private static class Died
        {
            private static void Prefix(Character __instance) => Publish.Safely("death", () =>
            {
                if (__instance.IsOwner()) CharacterEvents.Death(__instance);
            });
        }

        // Player.OnDeath replaces Character.OnDeath without calling it.
        [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
        private static class PlayerDied
        {
            private static void Prefix(Player __instance) => Publish.Safely("death", () =>
            {
                if (!__instance.IsOwner()) return;
                CharacterEvents.Death(__instance);
                PlayerEvents.Died(__instance);
            });
        }

        // Every Awake chain reaches Character.Awake. ZNetScene sets m_useInitZDO while it makes an object for an existing
        // ZDO (loaded from the world or from another machine); otherwise this machine created it. The event waits for the
        // plugin's next Update because a spawner sets the level just after Instantiate returns.
        [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
        private static class Appeared
        {
            private static void Postfix(Character __instance) => Publish.Safely("spawn", () => Later(__instance));

            private static void Later(Character character)
            {
                if (character.m_nview == null || character.m_nview.GetZDO() == null) return;
                bool loaded = ZNetView.m_useInitZDO;
                MainThread.Post(() => Publish.Safely("spawn", () => CharacterEvents.Spawn(character, loaded)));
            }
        }

        // Called on the owner when its AI notices (or forgets) a target.
        [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.SetAlerted))]
        private static class Alerted
        {
            private static void Prefix(BaseAI __instance, out bool __state) => __state = __instance.m_alerted;

            private static void Postfix(BaseAI __instance, bool __state)
            {
                if (__state || !__instance.m_alerted) return;
                Publish.Safely("boss", () =>
                {
                    if (__instance.m_character && __instance.m_character.IsBoss()) CharacterEvents.Boss("alerted", __instance.m_character);
                });
            }
        }
    }
}
