using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// A creature waking on the server (BaseAI.Awake, once per creature made or loaded): its animators are set for who runs
    /// it (<see cref="AlwaysAnimate"/>), and one the server has just made is noted, to go to a lone player once its
    /// spawn-time code has run (<see cref="FreshSpawns"/>). One patch for both.
    /// </summary>
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.Awake))]
    internal static class CreatureWake
    {
        private static void Postfix(BaseAI __instance)
        {
            if (!ServerRun.Active)
            {
                return;
            }
            AlwaysAnimate.OnWake(__instance);
            FreshSpawns.Note(__instance);
        }
    }
}
