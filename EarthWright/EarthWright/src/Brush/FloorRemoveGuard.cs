using HarmonyLib;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// The copy-floor-height key is the middle mouse button by default, which is also the game's "Remove" button in
    /// build mode. Prefix on <c>Player.UpdatePlacement</c>: while a terrain entry is selected and that key is held or
    /// released this frame, the game's remove press is blocked for the frame (its own <c>m_blockRemove</c> flag), so
    /// copying a floor's height never deconstructs it with a tool that can remove pieces. Local player only.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacement))]
    public static class FloorRemoveGuard
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance)
        {
            try
            {
                if (__instance != Player.m_localPlayer || !BrushState.Active || TargetKeys.Floor == null)
                    return;
                KeyCode key = TargetKeys.Floor.Value.MainKey;
                if (key != KeyCode.None && (Input.GetKey(key) || Input.GetKeyUp(key)))
                    __instance.m_blockRemove = true;
            }
            catch (System.Exception e)
            {
                BrushLog.Error("floor key remove guard", e);
            }
        }
    }
}
