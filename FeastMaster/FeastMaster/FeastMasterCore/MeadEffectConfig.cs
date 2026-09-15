using BepInEx.Configuration;

namespace FeastMaster
{
    /// <summary>The nine entries of a mead: the four over-time values and the five bonus modifiers of its status effect.</summary>
    public class MeadEffectConfig
    {
        public ConfigEntry<float> Duration { get; set; }
        public ConfigEntry<float> HealthOverTime { get; set; }
        public ConfigEntry<float> StaminaOverTime { get; set; }
        public ConfigEntry<float> EitrOverTime { get; set; }
        public ConfigEntry<float> HealthRegenMultiplier { get; set; }
        public ConfigEntry<float> StaminaRegenMultiplier { get; set; }
        public ConfigEntry<float> EitrRegenMultiplier { get; set; }
        public ConfigEntry<float> RunStaminaModifier { get; set; }
        public ConfigEntry<float> JumpStaminaModifier { get; set; }
    }
}
