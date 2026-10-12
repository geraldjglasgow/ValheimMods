using HarmonyLib;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The feet while charging (user, 2026-10-11: "as you aim around while charging your character should still move its
    /// feet to position body in line with where you're aiming"). The charging caster walks slowly (<see cref="ChargeStride"/>)
    /// and the body turns to where they aim, as it does while a bow is drawn (<see cref="Facing"/>). The game's player has
    /// no turn-on-the-spot motion, so while a charging caster standing still turns, the legs are given
    /// a sideways step towards the turn (<see cref="Steps"/>: the walk's sideways speed, which the game shares with every
    /// machine), quicker the faster the turn, nothing below <see cref="DeadZone"/> degrees a second. Local player only,
    /// on its own machine (the game moves only the characters it owns).
    /// </summary>
    public static class ChargeFeet
    {
        private const float DeadZone = 12f;
        private const float FullTurn = 240f;
        private const float StepSpeed = 1.2f;
        private const float Smoothing = 0.3f;

        private static readonly int Sideways = ZSyncAnimation.GetHash("sideway_speed");

        private static float lastYaw;
        private static float turnRate;

        [HarmonyPatch(typeof(Player), nameof(Player.AlwaysRotateCamera))]
        private static class Facing
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance, ref bool __result)
            {
                if (!__result && WindUp.Charging && __instance == Player.m_localPlayer)
                    __result = true;
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.UpdateWalking))]
        private static class Steps
        {
            [HarmonyPostfix]
            private static void Postfix(Character __instance, float dt)
            {
                if (__instance != Player.m_localPlayer || dt <= 0f)
                    return;
                float yaw = __instance.transform.eulerAngles.y;
                float rate = Mathf.DeltaAngle(lastYaw, yaw) / dt;
                lastYaw = yaw;
                bool standing = __instance.m_moveDir.sqrMagnitude < 0.01f;
                turnRate = WindUp.Charging && standing ? Mathf.Lerp(turnRate, rate, Smoothing) : 0f;
                if (Mathf.Abs(turnRate) >= DeadZone)
                    __instance.m_zanim.SetFloat(Sideways, Mathf.Clamp(turnRate / FullTurn, -1f, 1f) * StepSpeed);
            }
        }
    }
}
