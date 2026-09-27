using EarthWright.Actions;
using EarthWright.Core;
using EarthWright.Costs;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Clearing
{
    /// <summary>
    /// The Clear entry (special "clear"): one click clears the objects of the enabled kinds inside the brush (or the
    /// Clearing Radius circle) under the server's clearing mode and the protection rules (<see cref="ClearRules"/>).
    /// Nothing is charged when there is nothing to clear; otherwise the Costs module charges the entry once per click
    /// (which also keeps its cooldown).
    /// </summary>
    public sealed class ClearAction : ISpecialAction
    {
        public void OnClick(Player player, ToolAction action, Vector3 ghostPosition)
        {
            if (!ClearingSettings.Enabled)
            {
                Messages.Center(ClearingWords.Disabled);
                return;
            }
            ClearArea area = ClearArea.ForEntry(EntryStroke(action, ghostPosition));
            ClearPlan plan = ClearPlanner.Prepare(player, area, ClearingSettings.EntryMask, false, LocalTool.RightItemName);
            if (!plan.Empty && !CostApi.TryCharge(player, action, new EditEstimate()))
                return;
            int cleared = plan.Empty ? 0 : ClearJob.Execute(plan, player);
            ClearJob.Report(plan, cleared, Messages.Center, Messages.TopLeft);
        }

        /// <summary>The brush stroke the entry's click stands for; at the game's ghost when the brush is not tracking the aim.</summary>
        internal static BrushStroke EntryStroke(ToolAction action, Vector3 ghostPosition)
        {
            BrushStroke stroke = EditFactory.Build(action).Stroke;
            if (!BrushAim.Tracking)
                stroke.Center = ghostPosition;
            return stroke;
        }
    }
}
