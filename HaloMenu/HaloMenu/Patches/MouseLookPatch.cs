using HaloMenu.Runtime;
using HarmonyLib;
using UnityEngine;

namespace HaloMenu.Patches
{
    /// <summary>Suspends camera look while a ring is open: verified against Player.SetMouseLook and
    /// PlayerController.LateUpdate in assembly_valheim (build 25253764), the same choke point the game itself
    /// uses to suspend look for its own built-in radial menu (Hud.InRadial). Movement keys are untouched - this
    /// patch only ever zeroes the look vector, never blocks input.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.SetMouseLook))]
    internal static class MouseLookPatch
    {
        private static void Prefix(Player __instance, ref Vector2 mouseLook)
        {
            if (RingRegistry.AnyRingOpen && __instance == Player.m_localPlayer)
                mouseLook = Vector2.zero;
        }
    }
}
