using UnityEngine;

namespace OpenKeep.Batch
{
    /// <summary>
    /// Craft Speed: the game's craft bar durations (a craft, a multi-craft, an upgrader's base and per level time) are
    /// remembered at InventoryGui.Awake and divided by the setting before every UpdateRecipe, the one place the game
    /// reads them, so the bar, the craft and every mod reading the fields see the same time. The game's skill
    /// reduction is applied by the game on top.
    /// </summary>
    public static class CraftSpeed
    {
        private static float craft = 2f;
        private static float multi = 6f;
        private static float upgrader = 8f;
        private static float perLevel = 1f;

        public static void Remember(InventoryGui gui)
        {
            craft = gui.m_craftDuration;
            multi = gui.m_multiCraftDuration;
            upgrader = gui.m_upgraderDuration;
            perLevel = gui.m_upgraderDurationPerLevel;
        }

        public static void Apply(InventoryGui gui)
        {
            float speed = Mathf.Clamp(BatchSettings.CraftSpeed.Value, 0.1f, 10f);
            gui.m_craftDuration = craft / speed;
            gui.m_multiCraftDuration = multi / speed;
            gui.m_upgraderDuration = upgrader / speed;
            gui.m_upgraderDurationPerLevel = perLevel / speed;
        }
    }
}
