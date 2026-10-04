using EliteCreaturesReborn.Mutations;
using HarmonyLib;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Hides a Cloning creature's nameplate and health bar while its body is hidden behind its decoy, in exact step with
    /// the body (<see cref="CloneBehaviour.Hidden"/>), so only the decoy's shows and nothing marks where the creature
    /// really is. The decoy's own plate reads the same name, stars and health.
    /// </summary>
    [HarmonyPatch(typeof(EnemyHud), "TestShow")]
    public static class CloneHudPatch
    {
        private static void Postfix(Character c, ref bool __result)
        {
            if (__result && c != null && c.TryGetComponent(out CloneBehaviour clone) && clone.Hidden)
            {
                __result = false;
            }
        }
    }
}
