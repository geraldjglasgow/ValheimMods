using OpenKeep.Core;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// A click with an entry of the hammer's Blueprints tab selected (a Player.TryPlacePiece prefix): nothing is placed and
    /// nothing charged by the game. The first click pins the blueprint where it stands, so the player can
    /// walk around it; the next click checks it again on the spot (everything but the materials, which the site
    /// collects later) and places a construction site there (<see cref="Sites.SitePlacement"/>).
    /// </summary>
    public static class BlueprintTool
    {
        /// <summary>False (skip the game's placement) when the local player clicked with an entry of the tab.</summary>
        public static bool BeforePlace(Player player, Piece piece, ref bool result)
        {
            if (player != Player.m_localPlayer || !BlueprintMenu.IsOurs(piece))
                return true;
            result = false;
            if (BlueprintMenu.IsFix(piece))
                BlueprintSafe.Run("OpenKeep fix ground click", () => GroundFixSession.OnClick(player));
            else if (BlueprintMenu.IsPlanner(piece))
                BlueprintSafe.Run("OpenKeep site planner click", () => Sites.SiteHooks.PlannerClick(player));
            else if (BlueprintMenu.IsCopy(piece))
                BlueprintSafe.Run("OpenKeep copy click", () => Sites.SiteHooks.CopyClick(player));
            else if (BlueprintMenu.Owns(piece))
                BlueprintSafe.Run("OpenKeep blueprint click", () => OnClick(player));
            return false;
        }

        private static void OnClick(Player player)
        {
            if (!BlueprintSettings.Enabled)
                Messages.Center(BlueprintWords.Disabled);
            else if (!BlueprintSession.Pinned)
                BlueprintSession.Pin();
            else
                Build(player);
        }

        private static void Build(Player player)
        {
            SitePlan plan = BlueprintSession.PlanNow(player);
            string problem = plan == null ? Unreadable() : plan.Problem;
            if (problem != null)
            {
                Messages.Center(problem);
                return;
            }
            if (Sites.SitePlacement.Place(player, plan))
                BlueprintSession.Release(quiet: true);
        }

        private static string Unreadable()
        {
            string error = BlueprintLibrary.Error;
            return error != null ? BlueprintWords.Format(BlueprintWords.Unreadable, error) : BlueprintWords.NoBlueprints;
        }
    }
}
