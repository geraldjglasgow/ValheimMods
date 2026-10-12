using HarmonyLib;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// A caster blinks instead of rolling (user, 2026-10-11: "if you have wand equipped ... you don't dodge: rather you blink,
    /// a little farther than the dodge distance"). Every dodge the game queues (block held and jump, crouch and jump, the
    /// gamepad's dodge; <see cref="CasterControls"/> for the caster's right mouse) is taken where the game would roll
    /// (<c>Player.UpdateDodge</c>, on the player's own machine) under the game's own conditions (on the ground, alive, not
    /// attacking, overloaded, dodging or staggered) and for the roll's stamina plus half that in eitr (user, 2026-10-11:
    /// "the blink should take as much stamina as dodging, and 1/2 that value of eitr"): the caster is set down up to <see cref="Distance"/> along the dodge's direction, stopped
    /// short of anything solid, on the ground there, with a flash at both ends that the players near see
    /// (<see cref="CasterFx"/>). The game shares the player's new place as it does any move. At most once every
    /// <see cref="Cooldown"/> s, about a roll's length.
    /// From the blink until <see cref="Safe"/> s after it the caster cannot be hit by what a roll dodges (user, 2026-10-11:
    /// "when you cast to .5 seconds after you blink you are invulnerable"): the postfix holds the game's own dodge flag up,
    /// the one the owner's damage check reads and the one shared in the ZDO for the attackers' machines.
    /// </summary>
    [HarmonyPatch(typeof(Player), "UpdateDodge")]
    public static class CasterBlink
    {
        private const float Distance = 5f;
        private const float Cooldown = 0.8f;
        private const float Lift = 0.3f;
        private const float GroundSearch = 4f;
        private const float Safe = 0.5f;
        private const float EitrShare = 0.5f;

        private static float ready;
        private static float safeUntil;
        private static int solidMask;

        [HarmonyPrefix]
        private static void Prefix(Player __instance)
        {
            if (__instance != Player.m_localPlayer || __instance.m_queuedDodgeTimer <= 0f || !CastersSettings.On
                || CasterGearWorn.Wand(__instance) == null || !Ready(__instance))
                return;
            // Taken here, so the game never rolls a caster: during the cooldown the queued dodge is simply dropped.
            __instance.m_queuedDodgeTimer = 0f;
            if (Time.time < ready)
                return;
            if (!Pay(__instance))
                return;
            ready = Time.time + Cooldown;
            safeUntil = Time.time + Safe;
            CasterSafe.Run("blink", () => Blink(__instance, __instance.m_queuedDodgeDir));
        }

        /// <summary>Holds the dodge flag up through the safe time; the game lowers it on its next pass after.</summary>
        [HarmonyPostfix]
        private static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer || Time.time >= safeUntil || __instance.m_dodgeInvincibleCached)
                return;
            __instance.m_dodgeInvincibleCached = true;
            __instance.m_nview.GetZDO()?.Set(ZDOVars.s_dodgeinv, true);
        }

        /// <summary>The roll's stamina and half that in eitr, both or neither; the short bar flashes as the game's does.</summary>
        private static bool Pay(Player player)
        {
            float stamina = player.GetDodgeStaminaUse();
            float eitr = stamina * EitrShare;
            bool haveStamina = player.HaveStamina(stamina);
            bool haveEitr = player.HaveEitr(eitr);
            if (!haveStamina)
                Hud.instance?.StaminaBarEmptyFlash();
            if (!haveEitr)
                Hud.instance?.EitrBarEmptyFlash();
            if (!haveStamina || !haveEitr)
                return false;
            player.UseStamina(stamina);
            player.UseEitr(eitr);
            return true;
        }

        /// <summary>The game's own conditions for a dodge; while they fail, the game keeps the dodge queued and so do we.</summary>
        private static bool Ready(Player player) =>
            player.IsOnGround() && !player.IsDead() && !player.InAttack() && !player.IsEncumbered() && !player.InDodge()
            && !player.IsStaggering();

        private static void Blink(Player player, Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                return;
            direction.Normalize();
            Vector3 from = player.transform.position;
            Vector3 to = Ground(from + direction * Reach(player, from, direction), from.y);
            CasterFx.Send(CasterFx.BoltHit, from + Vector3.up, ZDOID.None, null);
            player.transform.position = to;
            if (player.m_body != null)
            {
                player.m_body.position = to;
                player.m_body.linearVelocity = Vector3.zero;
            }
            CasterFx.Send(CasterFx.BoltHit, to + Vector3.up, ZDOID.None, null);
        }

        /// <summary>How far the caster's body fits along the way: a capsule of its size, lifted over small steps.</summary>
        private static float Reach(Player player, Vector3 from, Vector3 direction)
        {
            if (solidMask == 0)
                solidMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");
            CapsuleCollider body = player.m_collider as CapsuleCollider;
            float radius = body != null ? body.radius * 0.9f : 0.3f;
            float height = body != null ? body.height : 1.8f;
            Vector3 low = from + Vector3.up * (radius + Lift);
            Vector3 high = from + Vector3.up * Mathf.Max(radius + Lift, height - radius);
            return Physics.CapsuleCast(low, high, radius, direction, out RaycastHit hit, Distance, solidMask, QueryTriggerInteraction.Ignore)
                ? Mathf.Max(0f, hit.distance - 0.1f)
                : Distance;
        }

        /// <summary>The ground under a spot, looked for from above it; the caster's own height when there is none (they fall).</summary>
        private static Vector3 Ground(Vector3 spot, float height)
        {
            Vector3 top = new Vector3(spot.x, height + GroundSearch / 2f, spot.z);
            return Physics.Raycast(top, Vector3.down, out RaycastHit hit, GroundSearch, solidMask, QueryTriggerInteraction.Ignore)
                ? hit.point
                : new Vector3(spot.x, height, spot.z);
        }
    }
}
