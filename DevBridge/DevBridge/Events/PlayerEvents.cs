using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace DevBridge.Events
{
    /// <summary>player events: the local player spawning, dying, teleporting and gaining a skill level. Built on the main
    /// thread; the patches below only read.</summary>
    internal static class PlayerEvents
    {
        private static float teleportStart;

        private static void Add(string what, Player player, Dictionary<string, object> facts)
        {
            var data = new Dictionary<string, object> { ["what"] = what, ["name"] = player.GetPlayerName() };
            foreach (KeyValuePair<string, object> fact in facts) data[fact.Key] = fact.Value;
            EventLog.Add("player", data);
        }

        /// <summary>Spawned on entering the world, respawned after a death; first is this session's first spawn.</summary>
        private static void Spawned(Player player, bool valkyrie)
        {
            Game game = Game.instance;
            Add(game && game.m_respawnAfterDeath ? "respawned" : "spawned", player, new Dictionary<string, object>
            {
                ["first"] = game && game.m_firstSpawn,
                ["valkyrie"] = valkyrie,
                ["position"] = CharacterFacts.Position(player),
            });
        }

        internal static void Died(Player player)
        {
            var facts = new Dictionary<string, object>();
            CharacterEvents.Cause(facts, player.m_lastHit);
            facts["position"] = CharacterFacts.Position(player);
            Add("died", player, facts);
        }

        private static void Teleporting(Player player, Vector3 to, bool distant)
        {
            teleportStart = Time.time;
            Add("teleport", player, new Dictionary<string, object>
            {
                ["from"] = Fmt.V3(player.m_teleportFromPos),
                ["to"] = Fmt.V3(to),
                ["distance"] = Fmt.R(Vector3.Distance(player.m_teleportFromPos, to)),
                ["distant"] = distant,
            });
        }

        // The game puts a player whose portal was blocked back where they started, so arriving away from the target is that.
        private static void Arrived(Player player)
        {
            Vector3 at = player.transform.position, target = player.m_teleportTargetPos;
            Add("arrived", player, new Dictionary<string, object>
            {
                ["position"] = Fmt.V3(at),
                ["blocked"] = new Vector2(at.x - target.x, at.z - target.z).magnitude > 1f,
                ["seconds"] = Fmt.R(Time.time - teleportStart),
            });
        }

        private static bool Local(Player player) => player != null && player == Player.m_localPlayer;

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        private static class Spawn
        {
            private static void Postfix(Player __instance, bool spawnValkyrie) =>
                Publish.Safely("player", () => Spawned(__instance, spawnValkyrie));
        }

        [HarmonyPatch(typeof(Player), nameof(Player.TeleportTo))]
        private static class Teleport
        {
            private static void Postfix(Player __instance, Vector3 pos, bool distantTeleport, bool __result)
            {
                if (__result) Publish.Safely("player", () => { if (Local(__instance)) Teleporting(__instance, pos, distantTeleport); });
            }
        }

        // Runs every physics step; the work happens only on the step the teleport ends.
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateTeleport))]
        private static class Teleported
        {
            private static void Prefix(Player __instance, out bool __state) => __state = __instance.m_teleporting;

            private static void Postfix(Player __instance, bool __state)
            {
                if (__state && !__instance.m_teleporting) Publish.Safely("player", () => { if (Local(__instance)) Arrived(__instance); });
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnSkillLevelup))]
        private static class Skill
        {
            private static void Postfix(Player __instance, Skills.SkillType skill, float level) => Publish.Safely("player", () =>
            {
                if (Local(__instance)) Add("skill", __instance, new Dictionary<string, object> { ["skill"] = skill.ToString(), ["level"] = (int)level });
            });
        }
    }
}
