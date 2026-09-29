namespace OpenKeep.Batch
{
    /// <summary>
    /// The stepper drives the game's own multi-craft instead of crafting by itself. <c>m_multiCraftAmount</c> is set to
    /// the amount, and <c>m_touchMultiCrafting</c> (the flag a long press sets on touch screens) makes the game treat the
    /// craft as a multi-craft while the amount is above 1. So the game's requirement rows, recipe name, affordability
    /// check, room check, payment, skill gain, bonus rolls and statistics all count the amount, and every mod reading
    /// those fields sees it. When the stepper stops applying, the game's own amount (5, for Shift + Craft) comes back.
    /// The amount of a started craft is kept apart and put back just before <c>DoCrafting</c>, so selecting another
    /// recipe or tab while the bar fills cannot change what the craft makes.
    /// </summary>
    public static class BatchDrive
    {
        private static int gameAmount = 5;
        private static bool driving;
        private static int started;

        /// <summary>At InventoryGui.Awake: the game's own amount, before anything drives it.</summary>
        public static void Remember(InventoryGui gui)
        {
            gameAmount = gui.m_multiCraftAmount;
            driving = false;
            started = 0;
        }

        public static void Drive(InventoryGui gui, int amount)
        {
            gui.m_multiCraftAmount = amount;
            gui.m_touchMultiCrafting = amount > 1;
            driving = true;
        }

        public static void Release(InventoryGui gui)
        {
            if (!driving)
                return;
            driving = false;
            gui.m_multiCraftAmount = gameAmount;
            gui.m_touchMultiCrafting = false;
        }

        /// <summary>
        /// After OnCraftPressed started a craft the stepper drove. The game also turned multi-crafting on for a held Shift
        /// or stick, so it is set from the amount alone: 1 is a single craft, with the single craft's time.
        /// </summary>
        public static void Started(InventoryGui gui, int amount)
        {
            started = amount;
            gui.m_multiCrafting = amount > 1;
        }

        public static void Forget() => started = 0;

        /// <summary>Before DoCrafting: the started craft's amount, whatever the stepper shows now.</summary>
        public static void Restore(InventoryGui gui)
        {
            if (started <= 0)
                return;
            gui.m_multiCraftAmount = started;
            gui.m_multiCrafting = started > 1;
        }
    }
}
