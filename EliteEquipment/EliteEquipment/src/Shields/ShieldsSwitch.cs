using HarmonyLib;

namespace EliteEquipment.Shields
{
    /// <summary>
    /// Puts the shields in or out together: their recipes (<see cref="ShieldRecipes"/>) and the game shields' remade
    /// icons (<see cref="ShieldIcons"/>). Runs when the item database wakes or is copied, and when the switch changes, a
    /// server's value arriving included. The items themselves always exist (<see cref="ShieldItems"/>).
    /// </summary>
    public static class ShieldsSwitch
    {
        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        private static class AwakePatch
        {
            [HarmonyPostfix]
            private static void Postfix(ObjectDB __instance) => Apply(__instance);
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        private static class CopyPatch
        {
            [HarmonyPostfix]
            private static void Postfix(ObjectDB __instance) => Apply(__instance);
        }

        /// <summary>The switch changed: the live database and every live item follow.</summary>
        public static void Changed() => Apply(ObjectDB.instance);

        private static void Apply(ObjectDB db)
        {
            ShieldRecipes.Install(db);
            ShieldIcons.Apply(ShieldsSettings.On);
        }
    }
}
