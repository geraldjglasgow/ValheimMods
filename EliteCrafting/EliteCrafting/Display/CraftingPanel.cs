using EliteCrafting.Affixes;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace EliteCrafting.Display
{
    /// <summary>
    /// The crafting panel's upgrade tab (display.md section 2). The game describes the selected recipe from the recipe
    /// prefab's data, not the player's item, so an upgrade target's rarity and affixes would never show there.
    /// <list type="bullet">
    /// <item>The selected recipe's description: while <c>InventoryGui.UpdateRecipe</c> runs, the upgrade target is
    /// remembered, and <see cref="TooltipPatch"/> appends the target's cached affix block to the prefab tooltip the
    /// game asks for. The description text then comes out identical frame to frame, so the label is not rebuilt.</item>
    /// <item>The selected recipe's name label, and each upgrade entry in the recipe list: colored through the label's
    /// <c>color</c> property, not a tag. The game rewrites the name text every frame; a tag would change the text twice
    /// per frame and rebuild the label, while an unchanged color is a compare and nothing else.</item>
    /// </list>
    /// Runs on the local player's client only, every frame while the panel is open: two field reads, one cached state
    /// lookup, no allocation.
    /// </summary>
    internal static class CraftingPanel
    {
        private static ItemDrop.ItemData? _recipeItem;
        private static ItemDrop.ItemData? _target;
        private static TMP_Text? _nameLabel;
        private static Color _nameBase;

        /// <summary>
        /// The item whose affix block belongs under a crafting tooltip: the upgrade target when the game is describing
        /// the selected recipe's prefab data, otherwise the item itself.
        /// </summary>
        public static ItemDrop.ItemData Shown(ItemDrop.ItemData item)
        {
            return _target != null && ReferenceEquals(item, _recipeItem) ? _target : item;
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
        private static class UpdateRecipePatch
        {
            [HarmonyPrefix]
            private static void Prefix(InventoryGui __instance)
            {
                Recipe? recipe = __instance.m_selectedRecipe.Recipe;
                ItemDrop? prefab = recipe == null ? null : recipe.m_item;
                ItemDrop.ItemData? target = __instance.m_selectedRecipe.ItemData;
                bool upgrading = prefab != null && target != null;
                _recipeItem = upgrading ? prefab!.m_itemData : null;
                _target = upgrading ? target : null;
            }

            [HarmonyPostfix]
            private static void Postfix(InventoryGui __instance)
            {
                ColorName(__instance.m_recipeName, _target);
                _recipeItem = null;
                _target = null;
            }
        }

        /// <summary>An upgrade entry in the recipe list, built on list rebuilds only (tab, station, inventory change).</summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.AddRecipeToList))]
        private static class RecipeListPatch
        {
            [HarmonyPostfix]
            private static void Postfix(InventoryGui __instance, ItemDrop.ItemData item, bool canCraft)
            {
                int count = __instance.m_availableRecipes.Count;
                if (item == null || count == 0 || !RarityPalette.TryNameColor(ItemState.Read(item).Rarity, out Color color))
                {
                    return;
                }
                GameObject element = __instance.m_availableRecipes[count - 1].InterfaceElement;
                Transform? name = element == null ? null : element.transform.Find("name");
                TMP_Text? label = name == null ? null : name.GetComponent<TMP_Text>();
                if (label != null)
                {
                    // The game greys an entry the player cannot afford; keep that cue by dimming the rarity color alike.
                    label.color = canCraft ? color : new Color(color.r * 0.66f, color.g * 0.66f, color.b * 0.66f, 1f);
                }
            }
        }

        // The label's own color is remembered the first time it is seen, and restored for plain and new-craft recipes.
        private static void ColorName(TMP_Text? label, ItemDrop.ItemData? target)
        {
            if (label == null)
            {
                return;
            }
            if (!ReferenceEquals(label, _nameLabel))
            {
                _nameLabel = label;
                _nameBase = label.color;
            }
            Color wanted = _nameBase;
            if (target != null && RarityPalette.TryNameColor(ItemState.Read(target).Rarity, out Color rarity))
            {
                wanted = rarity;
            }
            if (label.color != wanted)
            {
                label.color = wanted;
            }
        }
    }
}
