using EarthWright.Actions;
using EarthWright.Core;
using EarthWright.Costs;
using EarthWright.Protection;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>
    /// The module's per-frame driver: follows which entry is selected (forgetting points when it changes, if set, and
    /// always on death and logout), hands the keys to <see cref="PathInput"/>, and while the ramp or road entry is
    /// selected re-plans ten times a second (at once after a click or key) for the preview, the HUD and the preview
    /// status. Everything shown is hidden again when the entry is deselected or the tool put away.
    /// </summary>
    public static class PathSession
    {
        private const float PlanInterval = 0.1f;
        private const string StatusKey = "paths";

        private static string lastSpecial;
        private static float nextPlan;
        private static bool dirty = true;
        private static bool shown;

        /// <summary>Re-plan on the next frame (points or settings changed).</summary>
        public static void MarkDirty() => dirty = true;

        internal static void Tick()
        {
            ClickGate.Track();
            Player player = Player.m_localPlayer;
            if (player == null || player.IsDead())
            {
                Forget();
                return;
            }
            TrackSelection();
            ToolAction current = PathSelection.Current;
            PathInput.Handle(current);
            if (!PathSelection.IsRamp(current) && !PathSelection.IsRoad(current))
            {
                Hide();
                return;
            }
            if (dirty || Time.time >= nextPlan)
                Refresh(current);
        }

        /// <summary>The undo hook: removes the selected tool's last point. True when one was removed.</summary>
        public static bool RemoveLastPoint()
        {
            string special = PathSelection.SelectedSpecial;
            if (special == PathSelection.RampKey)
                return RampTool.Instance.RemoveLast();
            if (special == PathSelection.RoadKey)
                return RoadTool.Instance.RemoveLast();
            return false;
        }

        private static void Refresh(ToolAction current)
        {
            dirty = false;
            nextPlan = Time.time + PlanInterval;
            PathView view = PathSelection.IsRamp(current) ? PathViews.Ramp() : PathViews.Road();
            string work = WorkProblem(view, current);
            PathPreview.Show(view);
            PreviewStatus.Report(StatusKey, view.Ghostly ? null : PathHud.ShownProblem(view.Plan) ?? work);
            PathHud.Show(view, work);
            shown = true;
        }

        /// <summary>
        /// Why building the planned ramp or road now would be refused although the plan itself is fine: a protection
        /// rule over its ground (a ward, a no-build place, an admin zone) or a cost the player cannot pay; else null.
        /// </summary>
        private static string WorkProblem(PathView view, ToolAction action)
        {
            PathPlan plan = view.Plan;
            if (plan == null || view.Ghostly || !plan.Valid || plan.Vertices.Count == 0)
                return null;
            TerrainEdit edit = PathCommit.EditFor(plan, action, view.Source);
            return ProtectionGuards.SenderReason(edit) ?? CostApi.CannotPay(Player.m_localPlayer, action, Engine.Estimate(edit));
        }

        private static void TrackSelection()
        {
            string special = PathSelection.SelectedSpecial;
            if (special == lastSpecial)
                return;
            if (PathSettings.ClearWhenDeselected.Value)
                ClearTool(lastSpecial);
            lastSpecial = special;
            dirty = true;
        }

        private static void ClearTool(string special)
        {
            if (special == PathSelection.RampKey)
                RampTool.Instance.Clear();
            else if (special == PathSelection.RoadKey)
                RoadTool.Instance.Clear();
        }

        /// <summary>Death or logout: every point is forgotten, whatever the setting says.</summary>
        private static void Forget()
        {
            RampTool.Instance.Clear();
            RoadTool.Instance.Clear();
            lastSpecial = null;
            Hide();
        }

        private static void Hide()
        {
            if (!shown)
                return;
            shown = false;
            dirty = true;
            PathPreview.Hide();
            PathHud.Clear();
            PreviewStatus.Report(StatusKey, null);
        }
    }
}
