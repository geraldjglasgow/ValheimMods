using System.Collections.Generic;
using EarthWright.Actions;
using EarthWright.Core;
using EarthWright.Costs;
using EarthWright.Gear;
using UnityEngine;

namespace EarthWright.Extras
{
    /// <summary>
    /// The cultivator's Uproot entry (special action "uproot"): one click removes the wild pickables inside the brush
    /// footprint - berry bushes, mushrooms, thistle, branches, stones - and leaves crops alone (<see cref="NaturalPickables"/>).
    /// Refused as a whole where the cultivator is refused (dungeons, no-build locations) and when the tool's level does
    /// not unlock it; plants under someone else's ward or refused by the protection rules are skipped
    /// (<see cref="UprootTargets"/>). The costs module charges the click once something will be uprooted. Each plant is
    /// (with "Uproot Picks Items") picked by its owner so its items drop once, then destroyed (<see cref="OwnedPick"/>);
    /// drops and removal replicate to everyone through the game's own objects.
    /// </summary>
    public sealed class UprootAction : ISpecialAction
    {
        public void OnClick(Player player, ToolAction action, Vector3 ghostPosition)
        {
            if (!GeneralSettings.Active)
                return;
            Footprint area = Footprint.Of(action, ghostPosition);
            string refusal = AreaRefusal(player, action, area);
            UprootTargets targets = refusal == null ? UprootTargets.Find(player, area) : null;
            if (targets != null && targets.Allowed.Count == 0)
                refusal = targets.Refusal ?? ExtrasWords.NothingToUproot;
            if (refusal != null)
            {
                Messages.Center(refusal);
                return;
            }
            if (!CostApi.TryCharge(player, action, null))
                return;
            Messages.TopLeft(ExtrasWords.Uprooted + " " + RemoveAll(targets.Allowed));
        }

        /// <summary>The refusals for the whole click: tool level, dungeon, no-build location.</summary>
        private static string AreaRefusal(Player player, ToolAction action, Footprint area)
        {
            string locked = LevelCaps.Refusal(action.Id, area.Shape, null);
            if (locked != null)
                return locked;
            if (player.InInterior())
                return "$msg_notindungeon";
            return Location.IsInsideNoBuildLocation(area.Center) ? "$msg_nobuildzone" : null;
        }

        private static int RemoveAll(List<Pickable> found)
        {
            bool picks = ExtrasSettings.UprootPicksItems.Value;
            int removed = 0;
            foreach (Pickable pickable in found)
            {
                if (pickable != null && Remove(pickable, picks))
                    removed++;
            }
            return removed;
        }

        /// <summary>Optionally picks (through the owner, who drops the items), then destroys the object (<see cref="OwnedPick"/>).</summary>
        private static bool Remove(Pickable pickable, bool picks)
        {
            ZNetView view = pickable.m_nview != null ? pickable.m_nview : pickable.GetComponent<ZNetView>();
            return OwnedPick.PickThenRemove(view, picks && pickable.CanBePicked());
        }
    }
}
