namespace GrindstoneSkills
{
    /// <summary>
    /// What the Crafting perks (section 42) share: the module switch and the stations they work at. A station trains
    /// the skill in its m_craftingSkill; the workbench, the forge and the game's other crafting stations train Crafting,
    /// the kitchens (cauldron, mead cauldron, prep table) train Cooking and are left to the Kitchen code. The switch only
    /// decides new crafts and pieces: items and pieces marked earlier keep their bonus whatever it says, so every machine
    /// agrees on them even before the synced settings arrive. Read on whichever machine asks; the switch is synced, and
    /// the station test is for the local player, whose client is the only one that crafts for them.
    /// </summary>
    public static class CraftingPerks
    {
        /// <summary>Whether the Crafting perks are on (GameSkillSettings.Crafting, synced).</summary>
        public static bool Enabled => GameSkillSettings.Crafting != null && GameSkillSettings.Crafting.Value;

        /// <summary>The station the player is crafting at when it trains Crafting; null at a kitchen or away from stations.</summary>
        public static CraftingStation Station(Player player)
        {
            CraftingStation station = player != null ? player.GetCurrentCraftingStation() : null;
            return station != null && station.m_craftingSkill == Skills.SkillType.Crafting ? station : null;
        }
    }
}
