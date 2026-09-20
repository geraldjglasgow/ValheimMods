using System.Collections.Generic;
using EliteCreaturesReborn.Loot;
using EliteCreaturesReborn.Runtime;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Hands a kill's freshly built drop list to the loot engine. Because the mod keeps creatures at vanilla level 1,
    /// the game applies no level loot bonus of its own, so the engine's result is the whole story. Runs on the dying
    /// creature's owner (where the game builds the drop list); reads its traits, replicated from the owner's roll, so
    /// the outcome is the same wherever the creature was rolled. A creature the mod has not resolved is left vanilla.
    /// </summary>
    [HarmonyPatch(typeof(CharacterDrop), "GenerateDropList")]
    public static class LootPatch
    {
        private static void Postfix(CharacterDrop __instance, List<KeyValuePair<GameObject, int>> __result) =>
            Guard.Run("CharacterDrop.GenerateDropList", () => Rework(__instance, __result));

        private static void Rework(CharacterDrop drop, List<KeyValuePair<GameObject, int>> result)
        {
            Character character = Traverse.Create(drop).Field("m_character").GetValue<Character>();
            EliteController? controller = character != null ? character.GetComponent<EliteController>() : null;
            if (controller == null || !controller.Ready || result == null)
            {
                return;
            }
            LootEngine.Rework(drop, controller, result);
        }
    }
}
