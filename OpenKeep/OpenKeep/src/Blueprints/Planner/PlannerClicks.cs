using System.Collections.Generic;
using OpenKeep.Blueprints.Sites;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints.Planner
{
    /// <summary>
    /// A click with the Site planner entry (<see cref="SiteHooks.PlannerClick"/>, routed by <see cref="BlueprintTool"/>,
    /// which already kept the game from placing anything): the ghost piece under the crosshair is selected, or let go
    /// when it was; with Shift, the house around it (smart select) is added, or let go when all of it was selected; with
    /// G, the same for the joined pieces of its type.
    /// A pick on another site starts a new selection there.
    /// </summary>
    public static class PlannerClicks
    {
        public static void OnClick(Player player)
        {
            if (!BlueprintSettings.Enabled)
            {
                Messages.Center(BlueprintWords.Disabled);
                return;
            }
            if (!PlannerAim.Pick(out SiteMarker site, out int piece))
            {
                Messages.Center(PlannerWords.AimHint);
                return;
            }
            if (PlannerKeys.SameType)
                PickSameType(site, piece);
            else if (PlannerKeys.Shift)
                PickHouse(site, piece);
            else
                PlannerSelection.Toggle(site, piece);
        }

        private static void PickSameType(SiteMarker site, int piece)
        {
            List<int> group = PlannerGroup.Of(site, piece);
            if (group.Count == 0)
            {
                PlannerSelection.Toggle(site, piece);
                return;
            }
            int changed = PlannerSelection.ToggleGroup(site, group, smart: false);
            Messages.Center(BlueprintWords.Format(changed > 0 ? PlannerWords.SameTypeAdded : PlannerWords.SameTypeRemoved, Mathf.Abs(changed)));
        }

        private static void PickHouse(SiteMarker site, int piece)
        {
            List<int> house = PlannerHouse.Of(site, piece);
            if (house.Count == 0)
            {
                PlannerSelection.Toggle(site, piece);
                return;
            }
            int changed = PlannerSelection.ToggleHouse(site, house);
            Messages.Center(BlueprintWords.Format(changed > 0 ? PlannerWords.HouseAdded : PlannerWords.HouseRemoved, Mathf.Abs(changed)));
        }
    }
}
