using OpenKeep.Reach;
using OpenKeep.Recipes;

namespace OpenKeep.Tracker
{
    /// <summary>
    /// What a tracked material row counts. Need: the game's requirement amount at the tracked quality times the tracked
    /// amount. Have: what the player carries (the game's own count, any quality, current world level), plus, with Count
    /// Nearby Chests, what Reach would craft from in the containers within reach now (Reach's own count and rules).
    /// </summary>
    public static class TrackerCounts
    {
        public static int Need(Piece.Requirement requirement, int quality, int amount) => requirement.GetAmount(quality) * amount;

        public static int Have(Player player, Piece.Requirement requirement, int quality)
        {
            string name = requirement.m_resItem.m_itemData.m_shared.m_name;
            int carried = player.GetInventory().CountItems(name);
            if (!TrackerSettings.CountNearbyChests.Value || !ReachRules.Active(ReachRules.FromQuality(quality)))
                return carried;
            return carried + ReachCount.InContainers(name, -1, true);
        }

        /// <summary>The rows a tracked recipe shows: the materials the crafting panel lists for it where it was tracked.</summary>
        public static bool Shows(Piece.Requirement requirement, TrackedRecipe tracked)
        {
            return RecipeNeeds.Counts(requirement, tracked.Quality, tracked.Upgrader);
        }
    }
}
