using HarmonyLib;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The wand's bolt leaves from the wand's tip (user, 2026-10-11: "the magic spell should literally be on the tip of
    /// the wand when casting"), not from where the game starts the staff's cast (its attack origin, height and range, made
    /// for a staff two metres long). On the caster's machine, which fires the bolt: the spawn point is the tip in the hand
    /// at the trigger, and the aim is worked out again from there to what the caster aims at.
    /// </summary>
    [HarmonyPatch(typeof(Attack), nameof(Attack.GetProjectileSpawnPoint))]
    public static class TipSpawn
    {
        private const float Reach = 80f;
        private const float PastCaster = 0.6f;

        private static int aimMask;

        [HarmonyPostfix]
        private static void Postfix(Attack __instance, ref Vector3 spawnPoint, ref Vector3 aimDir)
        {
            if (!CastersSettings.On || !(__instance.m_character is Player player) || !CasterGearWorn.IsWand(__instance.m_weapon))
                return;
            Transform tip = Tip(player);
            if (tip == null)
                return;
            spawnPoint = tip.position;
            aimDir = (AimPoint(player) - spawnPoint).normalized;
        }

        /// <summary>
        /// What the crosshair is on: the game's camera ray, from just past the caster (so it never meets them), to the first
        /// thing it hits within <see cref="Reach"/> m, or that far along it. The game's own aim for a player is only the look
        /// direction, which from the wand's tip, beside and below the camera, runs parallel to the crosshair and misses it.
        /// </summary>
        public static Vector3 AimPoint(Player player)
        {
            Transform eye = GameCamera.instance != null ? GameCamera.instance.transform : null;
            if (eye == null)
                return player.transform.position + player.GetLookDir() * Reach;
            if (aimMask == 0)
                aimMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle",
                    "character", "character_net", "character_ghost", "hitbox", "character_noenv");
            float past = Mathf.Max(0f, Vector3.Dot(player.transform.position - eye.position, eye.forward)) + PastCaster;
            Vector3 from = eye.position + eye.forward * past;
            return Physics.Raycast(from, eye.forward, out RaycastHit hit, Reach, aimMask, QueryTriggerInteraction.Ignore)
                ? hit.point
                : from + eye.forward * Reach;
        }

        /// <summary>The tip of the wand in a player's hand on this machine, or null.</summary>
        public static Transform Tip(Player player)
        {
            VisEquipment vis = player != null ? player.m_visEquipment : null;
            GameObject held = vis != null ? vis.m_rightItemInstance : null;
            return held != null ? HeldModel.Tip(held.transform) : null;
        }
    }
}
