using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Game._RequestRespawn</c> prefix: the respawn starts (the game saves and destroys the dead player), so a choice
    /// of bed still open ends here with the bed chosen so far, whoever asked for the respawn.
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game._RequestRespawn))]
    public static class BedChoiceEndPatch
    {
        [HarmonyPrefix]
        public static void Prefix() => BedChoice.Close();
    }
}
