using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Single-ingredient recipes (Recipe.m_requireOnlyOneIngredient) pay with one stack picked by
    /// Player.GetFirstRequiredItem: the first requirement with a quality (0 to m_maxQuality) holding enough units.
    /// DoCrafting then removes that many of the stack's quality, and Recipe.GetAmount adds output for the stack's
    /// quality times m_qualityResultAmountMultiplier (meant for fish). For a recipe with an ingredient that carries
    /// stars the prefix picks the stack itself: for that ingredient, the first star count in the player's Ingredient
    /// Order that alone holds enough, matching <see cref="IngredientCounting"/>. Stars are not quality, so the postfix
    /// on GetAmount takes back the extra output a starred stack would give. The game's only such recipe today cleans
    /// fish, which never carry stars; this keeps modded recipes consistent.
    /// </summary>
    public static class SingleIngredient
    {
        [HarmonyPatch(typeof(Player), nameof(Player.GetFirstRequiredItem))]
        private static class FirstRequired
        {
            [HarmonyPrefix]
            private static bool Prefix(Player __instance, Inventory inventory, Recipe recipe, int qualityLevel, ref int amount,
                ref int extraAmount, int craftMultiplier, ref ItemDrop.ItemData __result, bool __runOriginal)
            {
                if (!__runOriginal)
                    return false;
                if (recipe == null || inventory == null || !HasStarredRequirement(recipe))
                    return true;
                amount = 0;
                extraAmount = 0;
                __result = null;
                CraftingStation station = __instance.GetCurrentCraftingStation();
                foreach (Piece.Requirement requirement in recipe.m_resources)
                {
                    if (Skipped(station, requirement) || !Covered(__instance.GetInventory(), requirement, qualityLevel, craftMultiplier, out int quality))
                        continue;
                    amount = requirement.GetAmount(qualityLevel) * craftMultiplier;
                    extraAmount = requirement.m_extraAmountOnlyOneIngredient;
                    __result = inventory.GetItem(requirement.m_resItem.m_itemData.m_shared.m_name, quality);
                    break;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(Recipe), nameof(Recipe.GetAmount))]
        private static class StarsAreNotQuality
        {
            [HarmonyPostfix]
            private static void Postfix(Recipe __instance, ref ItemDrop.ItemData singleReqItem, int craftMultiplier, ref int __result)
            {
                if (!__instance.m_requireOnlyOneIngredient || singleReqItem == null || singleReqItem.m_quality <= 1 || !Kitchen.IsKitchenItem(singleReqItem))
                    return;
                float extra = (singleReqItem.m_quality - 1) * __instance.m_amount * __instance.m_qualityResultAmountMultiplier;
                __result -= Mathf.CeilToInt(extra) * craftMultiplier;
            }
        }

        private static bool HasStarredRequirement(Recipe recipe) =>
            recipe.m_resources.Any(requirement => requirement.m_resItem != null && Kitchen.IsKitchenName(requirement.m_resItem.m_itemData.m_shared.m_name));

        /// <summary>The game's rule for requirements that do not apply at this station.</summary>
        private static bool Skipped(CraftingStation station, Piece.Requirement requirement) =>
            (station != null && station.m_upgrader != requirement.m_upgraderResource)
            || (station == null && requirement.m_upgraderResource)
            || !requirement.m_resItem;

        /// <summary>Whether one quality of the requirement holds enough units, and which, in the order to try them.</summary>
        private static bool Covered(Inventory counted, Piece.Requirement requirement, int qualityLevel, int craftMultiplier, out int quality)
        {
            ItemDrop.ItemData.SharedData shared = requirement.m_resItem.m_itemData.m_shared;
            int need = requirement.GetAmount(qualityLevel) * craftMultiplier;
            foreach (int candidate in Qualities(shared))
            {
                quality = candidate;
                if (IngredientCounting.Exact(counted, shared.m_name, candidate, true) >= need)
                    return true;
            }
            quality = 0;
            return false;
        }

        /// <summary>Star counts in the player's order for items that carry stars; the game's 0..m_maxQuality otherwise.</summary>
        private static IEnumerable<int> Qualities(ItemDrop.ItemData.SharedData shared) =>
            Kitchen.IsKitchenName(shared.m_name) ? IngredientTakeOrder.Qualities() : Enumerable.Range(0, shared.m_maxQuality + 1);
    }
}
