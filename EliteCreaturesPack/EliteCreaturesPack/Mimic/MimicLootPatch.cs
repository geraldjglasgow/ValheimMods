using System.Collections.Generic;
using EliteCreaturesPack.Core;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesPack.Mimic
{
    /// <summary>
    /// Adds a mimic's chest loot to its drop list, after every other change to the list (Elite Creatures Reborn's loot rules included, when it is installed), so
    /// the chest's rows arrive exactly as the chest would have held them.
    /// </summary>
    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    public static class MimicLootPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(CharacterDrop __instance, List<KeyValuePair<GameObject, int>> __result) =>
            SafeCall.Run("CharacterDrop.GenerateDropList mimic", () => MimicLoot.AddChestLoot(__instance, __result));
    }
}
