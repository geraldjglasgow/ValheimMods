using HarmonyLib;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// A creature's animator on the server always animates. Unity skips animators nobody sees, and a dedicated server sees
    /// nothing (a hosting player sees only their own area), but attacks land from animation events and the animation the
    /// players see is the owner's: a culled animator would never strike and would stand frozen for everyone.
    /// </summary>
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.Awake))]
    internal static class AlwaysAnimate
    {
        private static void Postfix(BaseAI __instance)
        {
            if (!ServerRun.Active)
            {
                return;
            }
            foreach (Animator animator in __instance.GetComponentsInChildren<Animator>(true))
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }
    }
}
