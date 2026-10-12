using HarmonyLib;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Walking while charging (user, 2026-10-11: "when casting there is still some slow movement you can do, I want to allow
    /// that same slow movement when charging a spell"): while the local player charges (<see cref="WindUp.Charging"/>, no
    /// attack running yet), they move at the wand's cast speed, the game's own attack slow-down (its
    /// <c>m_speedFactor</c>), as they do once the cast plays.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), "GetAttackSpeedFactorMovement")]
    public static class ChargeStride
    {
        [HarmonyPostfix]
        private static void Postfix(Humanoid __instance, ref float __result)
        {
            if (!WindUp.Charging || __instance != Player.m_localPlayer)
                return;
            ItemDrop.ItemData wand = CasterGearWorn.Wand(__instance);
            if (wand != null)
                __result = Mathf.Min(__result, wand.m_shared.m_attack.m_speedFactor);
        }
    }
}
