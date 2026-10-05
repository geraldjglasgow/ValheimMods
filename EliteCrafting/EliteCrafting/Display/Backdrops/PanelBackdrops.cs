using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Valheim.UI;

namespace EliteCrafting.Display.Backdrops
{
    /// <summary>
    /// Backdrops on the icons outside the item cells: the item following the cursor while dragged, the crafting panel
    /// (an upgrade entry in the recipe list and the selected upgrade's large icon, both showing the player's own item)
    /// and the radial menu's items. The split dialog needs none: a magic item never stacks.
    /// </summary>
    internal static class PanelBackdrops
    {
        private static GameObject? _ghost;
        private static Image? _ghostIcon;

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateItemDrag))]
        private static class DragPatch
        {
            [HarmonyPostfix]
            private static void Postfix(InventoryGui __instance)
            {
                GameObject ghost = __instance.m_dragGo;
                if (ghost == null)
                {
                    return;
                }
                if (!ReferenceEquals(ghost, _ghost))
                {
                    _ghost = ghost;
                    Transform icon = ghost.transform.Find("icon");
                    _ghostIcon = icon == null ? null : icon.GetComponent<Image>();
                }
                IconBackdrop.Set(_ghostIcon, __instance.m_dragItem);
            }
        }

        /// <summary>An upgrade entry in the recipe list, on list rebuilds only; its icon is hidden while it cannot be afforded.</summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.AddRecipeToList))]
        private static class RecipeListPatch
        {
            [HarmonyPostfix]
            private static void Postfix(InventoryGui __instance, ItemDrop.ItemData item)
            {
                int count = __instance.m_availableRecipes.Count;
                if (item == null || count == 0)
                {
                    return;
                }
                GameObject element = __instance.m_availableRecipes[count - 1].InterfaceElement;
                Transform? icon = element == null ? null : element.transform.Find("icon");
                if (icon != null)
                {
                    IconBackdrop.Set(icon.GetComponent<Image>(), item);
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
        private static class SelectedRecipePatch
        {
            [HarmonyPostfix]
            private static void Postfix(InventoryGui __instance)
            {
                Recipe? recipe = __instance.m_selectedRecipe.Recipe;
                IconBackdrop.Set(__instance.m_recipeIcon, recipe != null ? __instance.m_selectedRecipe.ItemData : null);
            }
        }

        [HarmonyPatch(typeof(ItemElement), nameof(ItemElement.Init))]
        private static class RadialPatch
        {
            [HarmonyPostfix]
            private static void Postfix(ItemElement __instance, ItemDrop.ItemData item) => IconBackdrop.Set(__instance.m_icon, item);
        }
    }
}
