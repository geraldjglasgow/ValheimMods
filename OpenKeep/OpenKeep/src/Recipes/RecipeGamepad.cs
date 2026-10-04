using System.Collections.Generic;
using OpenKeep.Batch;
using OpenKeep.Salvage;
using OpenKeep.Tracker;
using UnityEngine;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// The gamepad in the crafting panel, from the game's own recipe stepping (it runs only while the crafting panel is
    /// the selected part of the inventory). Right stick: up searches, down tracks or untracks the selected recipe, left
    /// makes it a favourite or not, right switches favourites only; each acts once per push, never on the game's
    /// repeat. In a grid the stick and D-pad step a whole row up and down and the left stick one tile left and right
    /// (the D-pad's left and right too while the batch stepper, which owns them, is hidden). The Salvage tab keeps its own.
    /// </summary>
    public static class RecipeGamepad
    {
        private static readonly HashSet<string> latched = new HashSet<string>();

        /// <summary>The prefix: false when the game's own list stepping must not run.</summary>
        public static bool Handle(InventoryGui gui)
        {
            if (SalvageTab.Active)
                return true;
            if (TypingGuard.Typing)
                return false;
            if (RecipeListSettings.GamepadControls.Value)
                Shortcuts(gui);
            if (!RecipeLayout.IsGrid)
                return true;
            StepGrid(gui);
            return false;
        }

        private static void Shortcuts(InventoryGui gui)
        {
            Recipe recipe = gui.m_selectedRecipe.Recipe;
            int quality = RecipeFilter.Quality(gui.m_selectedRecipe.ItemData);
            if (Once("JoyRStickUp"))
                SearchBar.Focus();
            if (Once("JoyRStickDown") && recipe != null)
                TrackerList.Toggle(recipe, quality);
            if (Once("JoyRStickLeft") && recipe != null)
                RecipeFavourites.Toggle(recipe);
            if (Once("JoyRStickRight"))
                RecipeFavourites.ToggleOnly();
        }

        /// <summary>A press of the button that is not the game's repeat of a held one.</summary>
        private static bool Once(string button)
        {
            if (!ZInput.GetButton(button))
            {
                latched.Remove(button);
                return false;
            }
            return ZInput.GetButtonDown(button) && latched.Add(button);
        }

        private static void StepGrid(InventoryGui gui)
        {
            int count = gui.m_availableRecipes.Count;
            int step = GridStep(RecipeLayout.Columns);
            if (count == 0 || step == 0)
                return;
            int index = Mathf.Clamp(gui.GetSelectedRecipeIndex() + step, 0, count - 1);
            gui.SetRecipe(index, true);
        }

        private static int GridStep(int columns)
        {
            bool pad = !BatchStepper.Shown;
            if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
                return columns;
            if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
                return -columns;
            if (ZInput.GetButtonDown("JoyLStickRight") || (pad && ZInput.GetButtonDown("JoyDPadRight")))
                return 1;
            if (ZInput.GetButtonDown("JoyLStickLeft") || (pad && ZInput.GetButtonDown("JoyDPadLeft")))
                return -1;
            return 0;
        }
    }
}
