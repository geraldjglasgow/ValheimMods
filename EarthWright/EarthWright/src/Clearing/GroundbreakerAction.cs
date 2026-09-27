using EarthWright.Actions;
using EarthWright.Brush;
using EarthWright.Core;
using EarthWright.Costs;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Clearing
{
    /// <summary>
    /// The Groundbreaker entry (special "groundbreaker"): one swing clears the objects in the brush (while clearing is
    /// switched on), then levels the ground to the target height and paves it. The level is an ordinary brush stroke
    /// of the entry's own action with paved paint (or the paint the player picked); it is checked by every sender guard
    /// first, then charged once, then the clearing runs and the stroke is sent, so wards, limits and undo treat it like
    /// any other stroke and a refused swing costs nothing.
    /// </summary>
    public sealed class GroundbreakerAction : ISpecialAction
    {
        public void OnClick(Player player, ToolAction action, Vector3 ghostPosition)
        {
            TerrainEdit edit = Build(player, action, ghostPosition);
            string reason = Dispatcher.Check(edit);
            if (!string.IsNullOrEmpty(reason))
            {
                Messages.Center(reason);
                return;
            }
            ClearPlan plan = PlanClearing(player, edit.Stroke);
            if (!CostApi.TryCharge(player, action, Engine.Estimate(edit)))
                return;
            if (plan != null && (!plan.Empty || plan.Refusal != null))
            {
                int cleared = plan.Empty ? 0 : ClearJob.Execute(plan, player);
                ClearJob.Report(plan, cleared, Messages.TopLeft, Messages.TopLeft);
            }
            Dispatcher.SendChecked(edit);
        }

        /// <summary>The level-and-pave stroke: the entry's own brush stroke, levelling, with paved paint unless the player picked a paint.</summary>
        private static TerrainEdit Build(Player player, ToolAction action, Vector3 ghostPosition)
        {
            TerrainEdit edit = EditFactory.Build(action);
            BrushStroke stroke = edit.Stroke;
            if (!BrushAim.Tracking)
            {
                stroke.Center = ghostPosition;
                stroke.Target = player.transform.position.y;
            }
            if (stroke.Height == HeightOp.None)
                stroke.Height = HeightOp.Level;
            if (BrushState.PaintOverride == PaintOp.None && !BrushState.KeepPaint)
                stroke.Paint = PaintOp.Paved;
            return edit;
        }

        /// <summary>The clearing part, only while clearing is switched on.</summary>
        private static ClearPlan PlanClearing(Player player, BrushStroke stroke)
        {
            if (!ClearingSettings.Enabled)
                return null;
            return ClearPlanner.Prepare(player, ClearArea.ForEntry(stroke), ClearingSettings.EntryMask, false, LocalTool.RightItemName);
        }
    }
}
