using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Fireplace.UpdateFireplace</c> (every two seconds on every client with the fire loaded): the fire's ZDO owner
    /// switches a listed torch at nightfall and daybreak. It runs even with the setting off, so torches put out earlier
    /// are lit again. High priority: before <see cref="FuelPatch"/>, so a torch put out now takes no fuel this tick.
    /// </summary>
    [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.UpdateFireplace))]
    public static class TorchPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.High)]
        public static void Postfix(Fireplace __instance)
        {
            if (FuelFires.OwnedPlayerFire(__instance))
                TorchSwitch.Tick(__instance);
        }
    }
}
