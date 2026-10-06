using EarthWright.Brush;
using EarthWright.Costs;
using EarthWright.Preview;
using HarmonyLib;

namespace EarthWright.Patches
{
    /// <summary>
    /// The one prefix and finalizer on <c>Player.UpdatePlacement</c>, which runs every frame in build mode: the
    /// local-player test is made once, then each module is asked in turn - the costs mark the placement frame, the
    /// floor key blocks the game's remove press, and dust removal swaps the tool's build effect. The finalizer ends
    /// both scopes even when the game's placement throws.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacement))]
    public static class UpdatePlacementPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance)
        {
            if (__instance == null || !ReferenceEquals(__instance, Player.m_localPlayer))
                return;
            PlacementCharges.BeginUpdate(__instance);
            FloorRemoveGuard.Apply(__instance);
            DustBuildEffect.Begin(__instance);
        }

        [HarmonyFinalizer]
        public static void Finalizer()
        {
            PlacementCharges.EndUpdate();
            DustBuildEffect.End();
        }
    }
}
