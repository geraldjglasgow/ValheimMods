using BepInEx.Configuration;
using HarmonyLib;

namespace FeastMaster
{
    /// <summary>
    /// World rate overrides. The world modifiers Game.m_foodRate, m_staminaRate, m_moveStaminaRate and
    /// m_staminaRegenRate are static fields set from the world's global keys in Game.UpdateWorldRates; an override
    /// above 0 replaces the value after every such update. A setting change re-runs the game's own update through
    /// ZoneSystem.UpdateWorldRates (which reads the current keys), so switching an override back to 0 restores
    /// the world's value.
    /// </summary>
    public static class WorldRates
    {
        public static void HookConfig(ConfigFile config)
        {
            config.SettingChanged += (_, args) =>
            {
                if (args.ChangedSetting.Definition.Section == Settings.WorldRatesSection)
                    Refresh();
            };
            config.ConfigReloaded += (_, __) => Refresh();
        }

        /// <summary>Runs the game's world rate update (the postfix then applies the overrides).</summary>
        public static void Refresh()
        {
            if (ZoneSystem.instance != null && Game.instance != null)
                ZoneSystem.instance.UpdateWorldRates();
        }

        public static void ApplyOverrides()
        {
            Override(ref Game.m_foodRate, Settings.FoodRate.Value);
            Override(ref Game.m_staminaRate, Settings.StaminaRate.Value);
            Override(ref Game.m_moveStaminaRate, Settings.MoveStaminaRate.Value);
            Override(ref Game.m_staminaRegenRate, Settings.StaminaRegenRate.Value);
        }

        private static void Override(ref float rate, float value)
        {
            if (value > 0f)
                rate = value;
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.UpdateWorldRates))]
    public static class WorldRatesPatch
    {
        [HarmonyPostfix]
        public static void Postfix() => WorldRates.ApplyOverrides();
    }
}
