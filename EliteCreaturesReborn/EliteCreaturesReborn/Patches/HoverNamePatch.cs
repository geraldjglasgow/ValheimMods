using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Runtime;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Prepends a creature's mutation words to its hover name. Vanilla's GetHoverName already returns the localized
    /// creature name, so the result is decorated once more with the mutation words; the nameplate re-localizes it,
    /// which is a no-op for plain text. This makes the name the complete, authoritative tell everywhere it appears. The
    /// game asks for it every frame a nameplate shows, so each creature keeps its decorated name
    /// (<see cref="EliteController.Name"/>) and builds it again only when the name it decorates changes.
    /// </summary>
    [HarmonyPatch(typeof(Character), "GetHoverName")]
    public static class HoverNamePatch
    {
        private static void Postfix(Character __instance, ref string __result)
        {
            if (Configuration.ShowTraitNames.Value)
            {
                __result = Guard.Run("Character.GetHoverName", static (character, name) => Decorate(character, name),
                    __instance, __result);
            }
        }

        private static string Decorate(Character character, string name)
        {
            EliteController? controller = ReadyElites.Of(character);
            if (controller == null || !Within(character)
                || Disguise.Holds(character)) // a dormant mimic keeps the chest's name, undecorated
            {
                return name;
            }
            return controller.Name.Decorate(controller.Traits, name);
        }

        private static bool Within(Character character)
        {
            float limit = Config.Configuration.NameplateDistance.Value;
            Player local = Player.m_localPlayer;
            if (limit <= 0f || local == null)
            {
                return true;
            }
            return (local.transform.position - character.transform.position).sqrMagnitude <= limit * limit;
        }
    }
}
