using EliteCreaturesReborn.Mutations;
using HarmonyLib;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Leaving a world - a logout or the game quitting, on every machine: the game takes down every object of the world
    /// here, and what this mod counted for that world's creatures (the Splintering cascades) goes with it, so nothing
    /// carries into the next world joined in the same session.
    /// </summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Shutdown))]
    public static class WorldExitPatch
    {
        private static void Postfix() => DescendantRegistry.Clear();
    }
}
