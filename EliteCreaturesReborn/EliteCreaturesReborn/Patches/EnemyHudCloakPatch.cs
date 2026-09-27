using EliteCreaturesReborn.Mutations;
using HarmonyLib;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Suppresses the nameplate of a cloaked creature while it is beyond its visible distance, in step with its hidden
    /// renderers, so a cloaked creature gives away nothing - not even a nameplate - until you are close. A Blinking
    /// creature's nameplate hides the same way for the moment its body is veiled mid-blink, so it never lingers at the
    /// spot it left.
    /// </summary>
    [HarmonyPatch(typeof(EnemyHud), "TestShow")]
    public static class EnemyHudCloakPatch
    {
        private static void Postfix(Character c, ref bool __result)
        {
            if (!__result || c == null)
            {
                return;
            }
            CloakBehaviour cloak = c.GetComponent<CloakBehaviour>();
            if (cloak != null && cloak.Hidden)
            {
                __result = false;
            }
            BlinkBehaviour blink = c.GetComponent<BlinkBehaviour>();
            if (blink != null && blink.Veiled)
            {
                __result = false;
            }
        }
    }
}
