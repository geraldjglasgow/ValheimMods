using EliteCreaturesReborn.Display;
using EliteCreaturesReborn.Runtime;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Prepends a creature's mutation words to its hover name. Vanilla's GetHoverName already returns the localized
    /// creature name, so the result is decorated once more with the mutation words; the nameplate re-localizes it,
    /// which is a no-op for plain text. This makes the name the complete, authoritative tell everywhere it appears.
    /// </summary>
    [HarmonyPatch(typeof(Character), "GetHoverName")]
    public static class HoverNamePatch
    {
        private static void Postfix(Character __instance, ref string __result)
        {
            string current = __result;
            __result = Guard.Run("Character.GetHoverName", () => Decorate(__instance, current));
        }

        private static string Decorate(Character character, string name)
        {
            EliteController controller = character.GetComponent<EliteController>();
            if (controller == null || !controller.Ready || !Config.Configuration.ShowTraitNames.Value || !Within(character))
            {
                return name;
            }
            return Naming.Decorate(controller.Traits, name);
        }

        private static bool Within(Character character)
        {
            float limit = Config.Configuration.NameplateDistance.Value;
            Player local = Player.m_localPlayer;
            if (limit <= 0f || local == null)
            {
                return true;
            }
            return Vector3.Distance(local.transform.position, character.transform.position) <= limit;
        }
    }
}
