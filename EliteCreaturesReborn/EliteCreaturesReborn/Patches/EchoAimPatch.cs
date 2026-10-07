using EliteCreaturesReborn.Aspects;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Where a creature aims its throws, breaths and spells from a point: an echo aims at the spot its boss's target stood
    /// on when the boss made the same move, `delay` seconds ago (<see cref="EchoLink.TryAim"/>), so a replayed throw flies
    /// where the boss's flew. Asked only as an attack fires; with no echo aiming it is one count check.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetAimDir))]
    public static class EchoAimPatch
    {
        private static void Postfix(Humanoid __instance, Vector3 fromPoint, ref Vector3 __result)
        {
            if (EchoLink.TryAim(__instance, out Vector3 point))
            {
                Vector3 to = point - fromPoint;
                if (to.sqrMagnitude > 0.0001f)
                {
                    __result = to.normalized;
                }
            }
        }
    }
}
