using EliteCreaturesReborn.Aspects;
using EliteCreaturesReborn.Breeding;
using EliteCreaturesReborn.Horde;
using EliteCreaturesReborn.Runtime;
using HarmonyLib;
using PatchGuard;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Gives every creature an <see cref="EliteController"/> as it wakes, except players and what players build
    /// (<see cref="PlayerBuilt"/>). The controller does the rest on its own first frame - by then any spawner has already
    /// set the vanilla star level, so nothing is lost. A boss resolves there on the boss table, so this stays a cheap attach.
    /// A creature the world's own spawning is making right now is tagged wild here, for the horde (<see cref="WildSpawn"/>).
    /// </summary>
    [HarmonyPatch(typeof(Character), "Awake")]
    public static class CharacterLifecyclePatch
    {
        private static void Postfix(Character __instance) =>
            Guard.Run("Character.Awake attach", static character => Attach(character), __instance);

        private static void Attach(Character character)
        {
            if (character.IsPlayer() || PlayerBuilt.Is(character) || character.GetComponent<EliteController>() != null)
            {
                return;
            }
            EliteController controller = character.gameObject.AddComponent<EliteController>();
            WildSpawn.Notice(character); // a creature the world's spawning is making right now is tagged wild
            EchoBody.VeilIfEcho(character); // an Echoing boss's echo, waking from its ZDO here: veiled before it is ever drawn
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
