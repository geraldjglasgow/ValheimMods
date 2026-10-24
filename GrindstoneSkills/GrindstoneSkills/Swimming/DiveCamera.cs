using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// The camera follows a diver under water, on the local player's client. The game keeps the camera at least
    /// m_minWaterDistance above the water (GameCamera.GetCameraPosition, each frame), which would leave it at the surface
    /// looking down at the water while the player dives. While the local player dives (<see cref="Diving.Under"/>) a
    /// prefix lowers that limit out of reach for this one call, and a finalizer puts the value back on every path, so the
    /// camera is the game's everywhere else; the game's own collision still keeps it out of the ground and walls.
    /// </summary>
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.GetCameraPosition))]
    public static class DiveCamera
    {
        /// <summary>A water distance no camera reaches.</summary>
        private const float NoLimit = -100000f;

        [HarmonyPrefix]
        private static void Prefix(GameCamera __instance, out float __state)
        {
            __state = float.NaN;
            if (!Diving.Under || !GameSkillSettings.Swimming.Value)
                return;
            __state = __instance.m_minWaterDistance;
            __instance.m_minWaterDistance = NoLimit;
        }

        [HarmonyFinalizer]
        private static void Finalizer(GameCamera __instance, float __state)
        {
            if (!float.IsNaN(__state))
                __instance.m_minWaterDistance = __state;
        }
    }
}
