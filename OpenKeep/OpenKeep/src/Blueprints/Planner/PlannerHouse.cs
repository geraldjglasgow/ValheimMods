using System.Collections.Generic;
using OpenKeep.Blueprints.Sites;

namespace OpenKeep.Blueprints.Planner
{
    /// <summary>
    /// Smart select's house around a site piece (<see cref="SmartSelect.Building"/>, a pure function of the blueprint)
    /// for Shift + click and the Shift preview of the hover glow. The last answer is kept, so hovering asks once per
    /// piece; a failing answer is logged once and stands for the piece alone. Only the house's unbuilt pieces are
    /// handed out.
    /// </summary>
    public static class PlannerHouse
    {
        private static Blueprint lastBlueprint;
        private static int lastPiece = -1;
        private static List<int> lastHouse;

        public static List<int> Of(SiteMarker site, int piece)
        {
            Blueprint bp = site.State.Blueprint;
            if (bp != lastBlueprint || piece != lastPiece || lastHouse == null)
            {
                lastHouse = BlueprintSafe.Call("OpenKeep smart select", () => SmartSelect.Building(bp, piece), null) ?? new List<int> { piece };
                lastBlueprint = bp;
                lastPiece = piece;
            }
            return PlannerPieces.Unbuilt(site.State, lastHouse);
        }
    }
}
