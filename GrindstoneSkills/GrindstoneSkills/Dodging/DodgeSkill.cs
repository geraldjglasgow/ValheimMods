namespace GrindstoneSkills
{
    /// <summary>
    /// The Dodging module's switch (section 40) and the local player's Dodge level, for its three perks:
    /// <see cref="LongRoll"/>, <see cref="DodgeCounter"/> and <see cref="HeavyRoll"/>. A player's dodge, their skills and
    /// their perfect dodges all live on their own client (Player.UpdateDodge and RPC_HitWhileDodging run only on the
    /// player's owner), so every perk runs there and nothing is sent. Off, or below a perk's level, the dodge is the game's.
    /// </summary>
    public static class DodgeSkill
    {
        /// <summary>Whether the server's Dodging switch is on.</summary>
        public static bool Active => GameSkillSettings.Dodging != null && GameSkillSettings.Dodging.Value;

        /// <summary>The local player's Dodge level as a share of 100, 0..1.</summary>
        public static float Factor => LocalSkill.Factor(Skills.SkillType.Dodge);

        /// <summary>Whether the switch is on and the local player has reached a perk's level.</summary>
        public static bool Reached(float level) => Active && LocalSkill.Reached(Skills.SkillType.Dodge, level);
    }
}
