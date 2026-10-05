using System.Collections.Generic;
using OpenKeep.Blueprints.Sites;

namespace OpenKeep.Blueprints.Planner
{
    /// <summary>
    /// The pieces of the same type joined to a site piece (<see cref="SmartSelect.SameType"/>: a wall run, a roof
    /// slope, a row of floor tiles) for G + click and the G preview of the hover glow. The last answer is kept, so
    /// hovering asks once per piece; a failing answer stands for the piece alone. Only unbuilt pieces are handed out.
    /// </summary>
    public static class PlannerGroup
    {
        private static Blueprint lastBlueprint;
        private static int lastPiece = -1;
        private static List<int> lastGroup;

        public static List<int> Of(SiteMarker site, int piece)
        {
            Blueprint bp = site.State.Blueprint;
            if (bp != lastBlueprint || piece != lastPiece || lastGroup == null)
            {
                lastGroup = BlueprintSafe.Call("OpenKeep same type select", () => SmartSelect.SameType(bp, piece), null) ?? new List<int> { piece };
                lastBlueprint = bp;
                lastPiece = piece;
            }
            return PlannerPieces.Unbuilt(site.State, lastGroup);
        }
    }
}
