using HarmonyLib;
using PatchGuard;

namespace GrindstoneSkills
{
    /// <summary>
    /// The one EnvMan.SetEnv postfix behind every feature that changes the fog the local player sees, in this order:
    /// Sailing's Fog Lift (<see cref="FogLiftView"/>), then Swimming's underwater view (<see cref="UnderwaterView"/>),
    /// which wins while the camera is under water. SetEnv runs on every client once a frame and sets the fog from the
    /// weather each time, so each feature scales or replaces the game's fresh value and nothing builds up. When SetEnv
    /// stops early because there is no camera, the fog was not set and is left alone.
    /// </summary>
    [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.SetEnv))]
    public static class EnvFogPass
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            if (Utils.GetMainCamera() != null)
                Guard.Run("fog", static () => Apply());
        }

        private static void Apply()
        {
            FogLiftView.Apply();
            UnderwaterView.Apply();
        }
    }
}
