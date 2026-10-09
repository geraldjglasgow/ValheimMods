using HarmonyLib;

namespace EliteEquipment.Boots
{
    /// <summary>
    /// Puts the whole split in or out together: the leggings' stats and set sizes (<see cref="BootsStats"/>), the recipes
    /// (<see cref="BootsRecipes"/>), the leggings' trousers icon (<see cref="LegsIcons"/>) and the leggings' look
    /// (<see cref="LegsLook"/>, attached again; the body paint follows); their weight follows the switch by itself
    /// (<see cref="BootsWeight"/>), so only the local inventory's total is counted again. Runs when the item database wakes
    /// and when the switch changes, a server's value arriving included; switching off takes the local player's pair off.
    /// </summary>
    public static class BootsSwitch
    {
        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        private static class AwakePatch
        {
            [HarmonyPostfix]
            private static void Postfix(ObjectDB __instance) => OnDatabase(__instance);
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        private static class CopyPatch
        {
            [HarmonyPostfix]
            private static void Postfix(ObjectDB __instance) => OnDatabase(__instance);
        }

        private static void OnDatabase(ObjectDB db)
        {
            BootsRecipes.Install(db);
            Apply(db);
            BootsLevels.Apply();
        }

        /// <summary>The switch changed: everything follows, live items included.</summary>
        public static void Changed()
        {
            Apply(ObjectDB.instance);
            Player player = Player.m_localPlayer;
            if (player == null)
                return;
            if (!BootsSettings.On)
                WornBoots.TakeOffAll(player);
            player.GetInventory().UpdateTotalWeight();
        }

        private static void Apply(ObjectDB db)
        {
            bool on = BootsSettings.On;
            BootsStats.Apply(db, on);
            BootsRecipes.Apply(db, on);
            LegsIcons.Apply(on);
            LegsLook.Apply();
        }
    }
}
