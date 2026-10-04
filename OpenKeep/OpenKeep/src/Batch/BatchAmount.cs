using UnityEngine;

namespace OpenKeep.Batch
{
    /// <summary>
    /// The amount the stepper shows, counted in crafts: a recipe that makes 20 arrows makes 20 per step, and the game's
    /// recipe name above shows the total. It goes back to 1 when another recipe is selected and never stays above what
    /// can be made now (<see cref="CanMake"/>), so once a batch has used up the materials it drops by itself.
    /// </summary>
    public static class BatchAmount
    {
        private const float RepeatWindow = 0.35f;
        private const int FastAfter = 8;

        private static Recipe recipe;
        private static int streak;
        private static int streakDirection;
        private static float lastStep = -10f;

        public static int Value { get; private set; } = 1;

        /// <summary>The recipe the amount belongs to (the one selected on the Craft tab when the stepper last showed).</summary>
        public static Recipe Current => recipe;

        /// <summary>The stepper belongs to the Craft tab (not Upgrade, not Salvage) of every station and of crafting by hand, except an upgrader.</summary>
        public static bool Applies(InventoryGui gui, Player player)
        {
            if (!BatchSettings.Enabled.Value || gui == null || player == null || !gui.InCraftTab())
                return false;
            CraftingStation station = player.GetCurrentCraftingStation();
            return station == null || !station.m_upgrader;
        }

        /// <summary>Every frame while the stepper applies: back to 1 on another recipe, down to what can be made.</summary>
        public static void Follow(Player player, Recipe selected)
        {
            if (selected != recipe)
            {
                recipe = selected;
                Value = 1;
            }
            if (recipe != null && Value > 1 && !CanMake(player, recipe, Value))
                Value = Limit(player, recipe);
        }

        public static bool CanStepUp(Player player) => recipe != null && CanMake(player, recipe, Value + 1);

        /// <summary>How many crafts the next press of Craft makes on the Craft tab: the stepper's amount, or without it the game's Shift + Craft rule.</summary>
        public static int NextCraft(InventoryGui gui, Player player)
        {
            if (Applies(gui, player))
                return Value;
            bool multi = ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyLStick") || gui.m_touchMultiCrafting;
            return multi ? gui.m_multiCraftAmount : 1;
        }

        /// <summary>
        /// A click on - (direction -1) or + (1), or the wheel: one step; with Shift to the next ten; with Ctrl to 1 or
        /// to the most that can be made. A gamepad's held D-pad, which the game repeats, steps by tens after 8 repeats.
        /// </summary>
        public static void Step(int direction)
        {
            Player player = Player.m_localPlayer;
            if (player == null || recipe == null)
                return;
            bool fast = FastRepeat(direction);
            int limit = Limit(player, recipe);
            Value = Mathf.Clamp(Target(direction, limit, fast), 1, limit);
        }

        /// <summary>Counts the gamepad's quick repeats in one direction; true once there have been enough.</summary>
        private static bool FastRepeat(int direction)
        {
            float now = Time.unscaledTime;
            bool repeat = ZInput.IsGamepadActive() && direction == streakDirection && now - lastStep < RepeatWindow;
            streak = repeat ? streak + 1 : 0;
            streakDirection = direction;
            lastStep = now;
            return streak >= FastAfter;
        }

        /// <summary>An amount typed into the field, kept within 1 and the most that can be made.</summary>
        public static void Set(int typed)
        {
            Player player = Player.m_localPlayer;
            if (player == null || recipe == null)
                return;
            Value = Mathf.Clamp(typed, 1, Limit(player, recipe));
        }

        private static int Target(int direction, int limit, bool fast)
        {
            if (Held(KeyCode.LeftControl, KeyCode.RightControl))
                return direction > 0 ? limit : 1;
            if (!fast && !Held(KeyCode.LeftShift, KeyCode.RightShift))
                return Value + direction;
            return direction > 0 ? (Value / 10 + 1) * 10 : (Value - 1) / 10 * 10;
        }

        private static bool Held(KeyCode left, KeyCode right) => Input.GetKey(left) || Input.GetKey(right);

        /// <summary>
        /// Whether that many crafts can be made now: within Max Amount, the materials (the game's HaveRequirements, so the
        /// station counts and, with Reach, the containers; skipped without a craft cost) and room for what is made.
        /// One craft always passes; the game checks it as usual.
        /// </summary>
        public static bool CanMake(Player player, Recipe made, int amount)
        {
            if (amount <= 1)
                return true;
            if (amount > BatchSettings.MaxAmount.Value)
                return false;
            bool free = player.NoCostCheat() || (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoCraftCost));
            if (!free && !player.HaveRequirements(made, false, 1, amount))
                return false;
            return player.GetInventory().CanAddItem(made.m_item.gameObject, made.m_amount * amount);
        }

        /// <summary>The most crafts that can be made now, at least 1. CanMake only turns false as the amount grows, so a binary search finds it.</summary>
        public static int Limit(Player player, Recipe made)
        {
            int low = 1;
            int high = BatchSettings.MaxAmount.Value;
            while (low < high)
            {
                int middle = (low + high + 1) / 2;
                if (CanMake(player, made, middle))
                    low = middle;
                else
                    high = middle - 1;
            }
            return low;
        }
    }
}
