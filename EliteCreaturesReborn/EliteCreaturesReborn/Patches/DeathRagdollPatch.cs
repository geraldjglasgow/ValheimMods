using EliteCreaturesReborn.Mutations;
using HarmonyLib;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// The owner of a dying creature, as the game sets up the ragdoll its death just made: noted, so a Bloated death names
    /// its own corpse (see <see cref="DeathRagdoll"/>). Runs inside the game's death, so a failure is reported and
    /// swallowed.
    /// </summary>
    [HarmonyPatch(typeof(Ragdoll), "Setup")]
    public static class DeathRagdollPatch
    {
        private static void Postfix(Ragdoll __instance) =>
            SafeCall.Run("Ragdoll.Setup (Bloated corpse)", () => DeathRagdoll.Made(__instance));
    }
}
