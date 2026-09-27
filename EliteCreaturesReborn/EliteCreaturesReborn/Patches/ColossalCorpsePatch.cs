using EliteCreaturesReborn.Aspects;
using HarmonyLib;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// The boss's owner, as the game makes a dying boss's ragdoll: a Colossal boss's corpse is grown to its size (see
    /// <see cref="ColossalCorpse"/>). Runs inside the game's death, so a failure is reported and swallowed.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), "OnRagdollCreated")]
    public static class ColossalCorpseMadePatch
    {
        private static void Postfix(Humanoid __instance, Ragdoll ragdoll) =>
            SafeCall.Run("Humanoid.OnRagdollCreated (Colossal)", () => ColossalCorpse.Grow(__instance, ragdoll));
    }

    /// <summary>
    /// Every machine, as any ragdoll wakes: one recorded as a Colossal boss's corpse grows to the size on its record.
    /// </summary>
    [HarmonyPatch(typeof(Ragdoll), "Awake")]
    public static class ColossalCorpseWakePatch
    {
        private static void Postfix(Ragdoll __instance) =>
            SafeCall.Run("Ragdoll.Awake (Colossal)", () => ColossalCorpse.Wake(__instance));
    }
}
