using UnityEngine;

namespace EliteCreaturesReborn.Rules
{
    /// <summary>
    /// The boss table: star chances and star power for bosses, entirely separate from the creature lines so a server
    /// can make the world brutal and leave bosses alone, or the reverse. Bosses roll their stars independently of the
    /// world's pressure - a boss is a set-piece you choose to walk into, not something you stumble across - so this is
    /// one table for every boss rather than a per-biome block. Bosses take no mutations.
    /// </summary>
    public sealed class BossRules
    {
        /// <summary>Off leaves every boss exactly as the game ships it, with the rest of the mod still working.</summary>
        public bool Enabled = true;

        /// <summary>Weights per star count, index 0 being no stars. Conservative by default: most bosses stay plain.</summary>
        public float[] StarChances = { 100f };

        /// <summary>The boss lines. Health, attack, size and drops scale; speed and swing speed are left alone.</summary>
        public StarPower Star = new StarPower();

        /// <summary>The highest star count the boss distribution can produce.</summary>
        public int StarCeiling => Mathf.Max(0, StarChances.Length - 1);

        public BossRules Clone()
        {
            return new BossRules
            {
                Enabled = Enabled, StarChances = (float[])StarChances.Clone(), Star = Star.Clone(),
            };
        }
    }
}
