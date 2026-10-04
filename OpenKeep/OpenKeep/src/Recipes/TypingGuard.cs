using OpenKeep.Batch;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// Typing in the crafting panel's fields (the recipe search, the batch amount) must not work the inventory's own
    /// keys: the game closes the inventory on its Use key (E), its Inventory key (Tab) and Escape, with no regard for a
    /// focused field. While a field is focused, or was at the end of the last frame (the field may already have handled
    /// Escape and let go this frame), the Use and Inventory presses are dropped and a frame with Escape skips the
    /// game's inventory update, so Escape only leaves the field. <see cref="TypingWatch"/> records the focus late in the frame.
    /// </summary>
    public static class TypingGuard
    {
        public static bool TypingNow => SearchBar.Focused || BatchField.Focused;

        public static bool Typing => TypingWatch.WasTyping || TypingNow;

        /// <summary>
        /// Before InventoryGui.Update: false when the game's update must skip this frame. A gamepad's B (or Y) leaves
        /// the field instead of closing the inventory: a pad without Steam's keyboard cannot type, and would otherwise
        /// be stuck in a field the right stick focused.
        /// </summary>
        public static bool BeforeInventoryUpdate()
        {
            if (!Typing)
                return true;
            ZInput.ResetButtonStatus("Inventory");
            ZInput.ResetButtonStatus("Use");
            if (ZInput.GetButtonDown("JoyButtonB") || ZInput.GetButtonDown("JoyButtonY"))
            {
                ZInput.ResetButtonStatus("JoyButtonB");
                ZInput.ResetButtonStatus("JoyButtonY");
                Leave();
                return false;
            }
            return !ZInput.GetKeyDown(KeyCode.Escape);
        }

        /// <summary>Lets go of the focused field (its text is kept).</summary>
        private static void Leave()
        {
            EventSystem system = EventSystem.current;
            if (system != null && TypingNow && !system.alreadySelecting)
                system.SetSelectedGameObject(null);
        }

        /// <summary>At InventoryGui.Awake, on the inventory's own object, which stays active while the panel is hidden.</summary>
        public static void Install(InventoryGui gui)
        {
            if (gui.GetComponent<TypingWatch>() == null)
                gui.gameObject.AddComponent<TypingWatch>();
        }
    }
}
