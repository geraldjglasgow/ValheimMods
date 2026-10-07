using System;
using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Fills <see cref="YieldCatalog"/>, forgets the cached <see cref="ProduceTables"/> and registers the eggs
    /// (<see cref="YieldStarItems"/>), on every machine. Runs last after both ZNetScene.Awake and ObjectDB.Awake, like
    /// <see cref="KitchenDiscovery"/>: whichever comes second finds both, so prefabs another mod registers in its own
    /// Awake postfix count too; running twice is harmless. The first run also subscribes to "Husbandry Enabled", so
    /// turning it on (in the .cfg or by the server's sync) registers the eggs at once.
    /// </summary>
    public static class YieldDiscovery
    {
        private static bool subscribed;

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class SceneAwake
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix() => HookGuard.Run("husbandry yield discovery", Discover);
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        private static class DatabaseAwake
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix() => HookGuard.Run("husbandry yield discovery", Discover);
        }

        private static void Discover()
        {
            YieldCatalog.Discover();
            ProduceTables.Clear();
            YieldStarItems.Register();
            Subscribe();
        }

        private static void Subscribe()
        {
            if (subscribed)
                return;
            subscribed = true;
            HusbandrySettings.Enabled.SettingChanged += OnSwitchChanged;
        }

        private static void OnSwitchChanged(object sender, EventArgs args) =>
            HookGuard.Run("husbandry eggs", YieldStarItems.Register);
    }
}
