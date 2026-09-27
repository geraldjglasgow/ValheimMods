using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Starred ingredients count for recipes. Player.HaveRequirementItems, behind every "can craft" check (the recipe
    /// list, the craft button, multi-craft and DoCrafting itself), counts each requirement at every quality from 1 to
    /// the item's m_maxQuality with Inventory.CountItems(name, quality) and keeps the largest. Kitchen items have a
    /// maximum quality of 1, so only 0-star units would count. While that method runs, CountItems(name, 1) for an item
    /// that carries stars counts past the stars: every unit for a normal recipe (paying takes any quality), the largest
    /// single star count for a single-ingredient recipe (it pays from one quality, see <see cref="SingleIngredient"/>).
    /// The requirement display (InventoryGui.SetupRequirement) already counts every quality. Any inventory counts this
    /// way inside the scope, so a mod that adds chest contents through CountItems counts their stars too.
    /// </summary>
    public static class IngredientCounting
    {
        private enum Mode
        {
            Off,
            AllStars,
            BestStar,
        }

        private static Mode mode;

        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirementItems))]
        private static class RequirementScope
        {
            [HarmonyPrefix]
            private static void Prefix(Recipe piece) =>
                mode = piece != null && piece.m_requireOnlyOneIngredient ? Mode.BestStar : Mode.AllStars;

            [HarmonyFinalizer]
            private static void Finalizer() => mode = Mode.Off;
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.CountItems))]
        private static class Count
        {
            [HarmonyPrefix]
            private static bool Prefix(Inventory __instance, string name, int quality, bool matchWorldLevel, ref int __result, bool __runOriginal)
            {
                if (!__runOriginal)
                    return false;
                if (mode == Mode.Off || quality != 1 || !Kitchen.IsKitchenName(name))
                    return true;
                __result = mode == Mode.AllStars ? __instance.CountItems(name, -1, matchWorldLevel) : BestStar(__instance, name, matchWorldLevel);
                return false;
            }
        }

        /// <summary>The most units that share one star count.</summary>
        private static int BestStar(Inventory inventory, string name, bool matchWorldLevel)
        {
            int best = 0;
            foreach (int quality in IngredientTakeOrder.Qualities())
                best = Mathf.Max(best, Exact(inventory, name, quality, matchWorldLevel));
            return best;
        }

        /// <summary>The game's own count of one quality, never widened by this scope.</summary>
        public static int Exact(Inventory inventory, string name, int quality, bool matchWorldLevel)
        {
            int count = 0;
            foreach (ItemDrop.ItemData item in inventory.m_inventory)
            {
                if (item.m_shared.m_name == name && item.m_quality == quality && (!matchWorldLevel || item.m_worldLevel >= Game.m_worldLevel))
                    count += item.m_stack;
            }
            return count;
        }
    }
}
