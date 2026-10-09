using EliteCreaturesPack.Core;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// The holder's input while on a Bone Ballista, through the game's doodad control (which would otherwise let go on
    /// attack, as a ship's helm does): attack shoots or says there is nothing to shoot, the other buttons that would let
    /// go or swing do nothing (jump and dodge still let go), and after the game has worked out the frame's movement the
    /// holder's feet are walked to their spot behind the ballista (<see cref="BallistaOperator.Steer"/>). Local player
    /// only, from PlayerController.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
    public static class BallistaControls
    {
        private static void Prefix(Player __instance, ref bool attack, ref bool attackHold, ref bool secondaryAttack,
            ref bool secondaryAttackHold, ref bool block, ref bool blockHold)
        {
            if (__instance.GetDoodadController() is not BallistaControl control)
            {
                return;
            }
            if (attack)
            {
                SafeCall.Run("bone ballista trigger", static (c, p) => BallistaOperator.Trigger(c, p), control, __instance);
            }
            (attack, attackHold, secondaryAttack, secondaryAttackHold, block, blockHold) = (false, false, false, false, false, false);
        }

        private static void Postfix(Player __instance)
        {
            if (__instance.GetDoodadController() is BallistaControl control)
            {
                SafeCall.Run("bone ballista steer", static (c, p) => BallistaOperator.Steer(c, p), control, __instance);
            }
        }
    }

    /// <summary>
    /// The holder's view stays within the ballista's own turn and moves at its pace: the camera's look turns no further
    /// than 45 degrees either side of where the ballista was placed, nor further up than it tilts or down than 30 degrees
    /// (no looking behind), and sideways no faster than the ballista turns and the holder can side-step
    /// (<see cref="BallistaOperator.YawRate"/>): the mouse beyond that is not taken, so the view, the ballista and the
    /// holder's feet move together and stop together when the mouse stops. Local player only, after each mouse movement.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.SetMouseLook))]
    public static class BallistaLookLimit
    {
        /// <summary>The view's tilt: as far up as the ballista; further down, since the camera sits above it.</summary>
        private const float LookUp = 25f, LookDown = 30f;

        private static BallistaControl? held;
        private static float yaw;

        private static void Postfix(Player __instance)
        {
            if (__instance.GetDoodadController() is not BallistaControl control || !control.IsValid())
            {
                held = null;
                return;
            }
            float placed = control.transform.eulerAngles.y;
            if (control != held)
            {
                (held, yaw) = (control, control.Yaw);   // the view starts where the ballista points
            }
            float wanted = Mathf.Clamp(Mathf.DeltaAngle(placed, __instance.m_lookYaw.eulerAngles.y), -BallistaOperator.YawLimit, BallistaOperator.YawLimit);
            yaw = Mathf.MoveTowards(yaw, wanted, BallistaOperator.YawRate * Time.deltaTime);
            __instance.m_lookYaw = Quaternion.Euler(0f, placed + yaw, 0f);
            __instance.m_lookPitch = Mathf.Clamp(__instance.m_lookPitch, -LookUp, LookDown);
            __instance.UpdateEyeRotation();
            __instance.m_lookDir = __instance.m_eye.forward;
        }
    }

    /// <summary>
    /// The holder's facing is the ballista's (<see cref="BallistaOperator.Face"/>): the way they walk while walking up
    /// to it, then the way it points, kept while their feet shuffle sideways, back or forward, so the walk plays as a side
    /// step. Every character passes through here each physics step; anyone else is untouched.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.UpdateRotation))]
    public static class BallistaFacing
    {
        private static bool Prefix(Character __instance, float dt, ref float __result)
        {
            if (__instance is not Player player || player.m_doodadController is not BallistaControl control)
            {
                return true;
            }
            __result = BallistaOperator.Face(control, player, dt);
            return false;
        }
    }
}
