using EarthWright.Brush;
using EarthWright.Extras;
using EarthWright.Gear;
using EarthWright.Preview;
using HarmonyLib;

namespace EarthWright.Patches
{
    /// <summary>
    /// The one postfix on <c>ZInput.GetMouseScrollWheel</c>, which every system of the game reads: the rest of the game
    /// reads 0 while the F6 panel is open (the wheel scrolls the panel) or while the brush owns the wheel.
    /// </summary>
    [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseScrollWheel))]
    public static class ScrollWheelPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref float __result)
        {
            if (__result == 0f)
                return;
            if (PanelWindow.IsOpen)
            {
                __result = 0f;
                return;
            }
            try
            {
                if (ScrollInput.HideFromGame())
                    __result = 0f;
            }
            catch (System.Exception e)
            {
                BrushLog.Error("scroll capture", e);
            }
        }
    }

    /// <summary>
    /// The one postfix on <c>Player.GetRunSpeedFactor</c>: for the local player, sprinting is faster by "Movement
    /// Speed" while a terrain tool is held (Gear) and by the road's bonus on a road (Extras).
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.GetRunSpeedFactor))]
    public static class RunSpeedPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance, ref float __result)
        {
            if (ReferenceEquals(__instance, Player.m_localPlayer))
                __result *= ToolSpeed.Factor(__instance) * RoadTravel.SpeedFactor(__instance);
        }
    }
}
