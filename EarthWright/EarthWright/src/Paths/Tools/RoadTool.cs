using System.Collections.Generic;
using EarthWright.Actions;
using EarthWright.Core;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>
    /// The road entry (special action "road"). Each click adds a waypoint; the road follows a smooth curve through
    /// them, as wide as the brush. The carve key builds it with the current paint, the paved carve key paves it. The
    /// waypoints are local planning state until carved, and are forgotten once the road is carved.
    /// </summary>
    public sealed class RoadTool : ISpecialAction
    {
        public static readonly RoadTool Instance = new RoadTool();

        /// <summary>A waypoint closer than this to the last one is refused (it would kink the curve).</summary>
        public const float MinSpacing = 1f;

        private readonly List<Vector3> waypoints = new List<Vector3>();

        public int Count => waypoints.Count;

        public IReadOnlyList<Vector3> Waypoints => waypoints;

        public void OnClick(Player player, ToolAction action, Vector3 ghostPosition)
        {
            if (player != Player.m_localPlayer || !ClickGate.Take())
                return;
            Vector3 point = PathSelection.WithHeight(PathSelection.CursorOr(ghostPosition));
            if (waypoints.Count > 0 && CentreLine.Flat(waypoints[waypoints.Count - 1], point) < MinSpacing)
            {
                Messages.Center(PathWords.Text("waypoint_close"));
                return;
            }
            waypoints.Add(point);
            Messages.TopLeft(PathWords.Format("waypoint", waypoints.Count));
            PathSession.MarkDirty();
        }

        /// <summary>The road the carve key would build, or null with fewer than two waypoints.</summary>
        public PathDraft Draft(bool paved)
        {
            if (waypoints.Count < 2)
                return null;
            PaintOp paint = paved ? PaintOp.Paved : PathSelection.Paint(PathSettings.RoadPaint.Value);
            return DraftFactory.Road(waypoints, paint);
        }

        public void Carve(ToolAction action, bool paved)
        {
            if (waypoints.Count < 2)
            {
                Messages.Center(PathWords.Text("need_two"));
                return;
            }
            if (!PathCommit.Commit(Draft(paved), action, PathSelection.RoadKey))
                return;
            waypoints.Clear();
            Messages.TopLeft(PathWords.Text("carved"));
            PathSession.MarkDirty();
        }

        /// <summary>Removes the last waypoint. False when there was none.</summary>
        public bool RemoveLast()
        {
            if (waypoints.Count == 0)
                return false;
            waypoints.RemoveAt(waypoints.Count - 1);
            Messages.TopLeft(PathWords.Text("removed"));
            PathSession.MarkDirty();
            return true;
        }

        public void Clear()
        {
            if (waypoints.Count == 0)
                return;
            waypoints.Clear();
            PathSession.MarkDirty();
        }
    }
}
