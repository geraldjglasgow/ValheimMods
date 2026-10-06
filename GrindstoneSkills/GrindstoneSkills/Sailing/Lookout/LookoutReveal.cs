using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The enemies a lookout pulse reached, and their name tags, on each client aboard the pulsing ship. The game's
    /// EnemyHud shows a creature's tag (name, stars, health, alert state) only within 10 m (TestShow) and only for a
    /// minute after the player looked at it (the hud's hover timer). For each revealed creature TestShow says yes at
    /// any distance and the hover timer is held at 0, until the reveal ends; the game then drops tags beyond 10 m on
    /// its own. The pulse is a snapshot: a creature that was inside the radius keeps its tag wherever it goes.
    /// Enemies are the game's own (BaseAI.IsEnemy, so tamed creatures are not); bosses are left out, since their
    /// tag is the boss bar at the top of the screen. Once the latest reveal has ended the list is emptied, so the hud
    /// patches go back to one test and no destroyed creature is kept.
    /// </summary>
    public static class LookoutReveal
    {
        private static readonly Dictionary<Character, float> revealed = new Dictionary<Character, float>();

        /// <summary>When the last of the current reveals ends.</summary>
        private static float latestUntil;

        /// <summary>Reveals every enemy within <paramref name="radius"/> (flat) of the centre for some seconds; returns how many.</summary>
        public static int Reveal(Player player, Vector3 center, float radius, float seconds)
        {
            Prune();
            float until = Time.time + Mathf.Max(0f, seconds);
            latestUntil = Mathf.Max(latestUntil, until);
            int count = 0;
            foreach (Character character in Character.GetAllCharacters())
            {
                if (!IsTarget(player, character, center, radius))
                    continue;
                revealed[character] = Mathf.Max(until, revealed.TryGetValue(character, out float earlier) ? earlier : 0f);
                count++;
            }
            return count;
        }

        public static bool IsRevealed(Character character) =>
            character != null && revealed.TryGetValue(character, out float until) && Time.time < until;

        private static bool IsTarget(Player player, Character character, Vector3 center, float radius)
        {
            if (character == null || character.IsPlayer() || character.IsDead() || character.IsBoss() || character.m_hideHud)
                return false;
            Vector3 offset = character.transform.position - center;
            offset.y = 0f;
            return offset.sqrMagnitude <= radius * radius && BaseAI.IsEnemy(player, character);
        }

        /// <summary>Drops ended reveals and creatures that are gone.</summary>
        private static void Prune()
        {
            List<Character> ended = new List<Character>();
            foreach (KeyValuePair<Character, float> entry in revealed)
            {
                if (entry.Key == null || Time.time >= entry.Value)
                    ended.Add(entry.Key);
            }
            foreach (Character character in ended)
                revealed.Remove(character);
        }

        [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.TestShow))]
        private static class Show
        {
            [HarmonyPostfix]
            private static void Postfix(Character c, ref bool __result)
            {
                if (!__result && revealed.Count > 0 && IsRevealed(c))
                    __result = true;
            }
        }

        [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.UpdateHuds))]
        private static class KeepShown
        {
            [HarmonyPrefix]
            private static void Prefix(EnemyHud __instance)
            {
                if (revealed.Count == 0)
                    return;
                if (Time.time >= latestUntil)
                {
                    revealed.Clear();
                    return;
                }
                foreach (KeyValuePair<Character, EnemyHud.HudData> hud in __instance.m_huds)
                {
                    if (IsRevealed(hud.Key))
                        hud.Value.m_hoverTimer = 0f;
                }
            }
        }
    }
}
