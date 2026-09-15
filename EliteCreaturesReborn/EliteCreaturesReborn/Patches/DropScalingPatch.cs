using System.Collections.Generic;
using EliteCreaturesReborn.Runtime;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Multiplies a creature's loot by its star `drops` line. Because the mod keeps creatures at vanilla level 1, the
    /// game applies no level loot bonus of its own, so this multiplier is the whole story. Only quantities are scaled;
    /// which items drop is untouched. `drops` takes no mutation contribution, so it is used exactly as written.
    /// Runs on the dying creature's owner (where the game builds the drop list); reads its traits, replicated from the
    /// owner's roll, so the multiplier is the same wherever the creature was rolled.
    /// </summary>
    [HarmonyPatch(typeof(CharacterDrop), "GenerateDropList")]
    public static class DropScalingPatch
    {
        private static void Postfix(CharacterDrop __instance, List<KeyValuePair<GameObject, int>> __result) =>
            Guard.Run("CharacterDrop.GenerateDropList", () => Scale(__instance, __result));

        private static void Scale(CharacterDrop drop, List<KeyValuePair<GameObject, int>> result)
        {
            Character character = Traverse.Create(drop).Field("m_character").GetValue<Character>();
            EliteController? controller = character != null ? character.GetComponent<EliteController>() : null;
            if (controller == null || !controller.Ready || result == null)
            {
                return;
            }
            float multiplier = controller.Rules.Star.DropsAt(controller.Traits.Stars);
            if (multiplier <= 1f)
            {
                return;
            }
            Apply(result, multiplier);
        }

        private static void Apply(List<KeyValuePair<GameObject, int>> result, float multiplier)
        {
            for (int i = 0; i < result.Count; i++)
            {
                int scaled = Mathf.Max(1, Mathf.RoundToInt(result[i].Value * multiplier));
                result[i] = new KeyValuePair<GameObject, int>(result[i].Key, scaled);
            }
        }
    }
}
