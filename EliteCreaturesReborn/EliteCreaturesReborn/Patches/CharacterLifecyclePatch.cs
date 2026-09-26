using EliteCreaturesReborn.Aspects;
using EliteCreaturesReborn.Breeding;
using EliteCreaturesReborn.Runtime;
using HarmonyLib;
using PatchGuard;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Gives every non-player creature an <see cref="EliteController"/> as it wakes. The controller does the rest on
    /// its own first frame - by then any spawner has already set the vanilla star level, so nothing is lost. Players
    /// are filtered inside the controller, and a boss resolves there on the boss table, so this stays a cheap attach.
    /// </summary>
    [HarmonyPatch(typeof(Character), "Awake")]
    public static class CharacterLifecyclePatch
    {
        private static void Postfix(Character __instance) =>
            Guard.Run("Character.Awake attach", () => Attach(__instance));

        private static void Attach(Character character)
        {
            if (character.IsPlayer() || character.GetComponent<EliteController>() != null)
            {
                return;
            }
            EliteController controller = character.gameObject.AddComponent<EliteController>();
            if (character.IsBoss())
            {
                AltarSummon.Claim(controller); // a boss an altar is instantiating right now takes the aspect it offered for
            }
            else
            {
                Lineage.Claim(controller); // a creature being born, hatched or grown right now takes what it inherits
            }
        }
    }
}
