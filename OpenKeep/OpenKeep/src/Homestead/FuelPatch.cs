using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Fireplace.UpdateFireplace</c> runs every two seconds on every client that has the fire loaded (an
    /// <c>InvokeRepeating</c> from <c>Awake</c>); only the ZDO owner burns fuel in it. The postfix refills on that owner
    /// after the burn. Every other machine returns after two field reads. Runs after <see cref="TorchPatch"/>, so a torch
    /// switched off in this tick takes nothing.
    /// </summary>
    [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.UpdateFireplace))]
    public static class FuelPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Fireplace __instance)
        {
            if (FuelSettings.AutoFuel.Value && FuelFires.OwnedPlayerFire(__instance))
                FuelRefill.Tick(__instance);
        }
    }
}
