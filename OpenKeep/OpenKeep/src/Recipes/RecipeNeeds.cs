namespace OpenKeep.Recipes
{
    /// <summary>
    /// Which of a recipe's materials count, by the game's own rule (InventoryGui.SetupRequirementList): at an upgrader
    /// station only the upgrader materials, anywhere else (another station, or by hand) only the others; and only those
    /// with an amount at the quality made.
    /// </summary>
    public static class RecipeNeeds
    {
        public static bool Counts(Piece.Requirement requirement, int quality, bool upgrader)
        {
            return requirement != null && requirement.m_resItem != null && requirement.m_upgraderResource == upgrader
                && requirement.GetAmount(quality) > 0;
        }

        /// <summary>The local player stands at an upgrader station now.</summary>
        public static bool AtUpgrader
        {
            get
            {
                CraftingStation station = Player.m_localPlayer != null ? Player.m_localPlayer.GetCurrentCraftingStation() : null;
                return station != null && station.m_upgrader;
            }
        }
    }
}
