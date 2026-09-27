using System.Collections.Generic;
using EarthWright.Actions;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>
    /// The ramp entry (special action "ramp"). The first click sets the start, the second the end, the third builds.
    /// Until the third click the width follows the brush size, or, with "Width From Cursor", how far the cursor is to
    /// the side of the ramp's middle; holding the one-side modifier puts the whole width on the cursor's side, and
    /// holding the blend modifier blends the ends into the ground. The points are local planning state: nothing
    /// reaches the terrain before the build click, which goes through <see cref="PathCommit"/>.
    /// </summary>
    public sealed class RampTool : ISpecialAction
    {
        public static readonly RampTool Instance = new RampTool();

        private readonly List<Vector3> points = new List<Vector3>();

        /// <summary>0: nothing set, 1: the start is set, 2: start and end are set.</summary>
        public int Count => points.Count;

        public IReadOnlyList<Vector3> Points => points;

        public void OnClick(Player player, ToolAction action, Vector3 ghostPosition)
        {
            if (player != Player.m_localPlayer || !ClickGate.Take())
                return;
            Vector3 cursor = PathSelection.CursorOr(ghostPosition);
            if (points.Count < 2)
                AddPoint(PathSelection.WithHeight(cursor));
            else
                Build(action, cursor);
        }

        /// <summary>What a build click would build now, or null while the ramp has no start (or no cursor for its end).</summary>
        public PathDraft Draft(Vector3? cursor)
        {
            bool blend = Keys.Held(PathSettings.BlendEndsModifier);
            if (points.Count >= 2)
                return FullDraft(cursor, blend);
            if (points.Count == 1 && cursor.HasValue)
                return DraftFactory.Ramp(points[0], PathSelection.WithHeight(cursor.Value), 0, blend, PathSelection.BrushWidth());
            return null;
        }

        /// <summary>Removes the end, or else the start. False when there was nothing to remove.</summary>
        public bool RemoveLast()
        {
            if (points.Count == 0)
                return false;
            points.RemoveAt(points.Count - 1);
            Messages.TopLeft(PathWords.Text("removed"));
            PathSession.MarkDirty();
            return true;
        }

        public void Clear()
        {
            if (points.Count == 0)
                return;
            points.Clear();
            PathSession.MarkDirty();
        }

        private void AddPoint(Vector3 point)
        {
            if (points.Count == 1 && CentreLine.Flat(points[0], point) < PathChecks.MinLength)
            {
                Messages.Center(PathWords.Text("too_short"));
                return;
            }
            points.Add(point);
            Messages.TopLeft(PathWords.Text(points.Count == 1 ? "start_set" : "end_set"));
            PathSession.MarkDirty();
        }

        private void Build(ToolAction action, Vector3 cursor)
        {
            if (!PathCommit.Commit(Draft(cursor), action, PathSelection.RampKey))
                return;
            points.Clear();
            Messages.TopLeft(PathWords.Text("built"));
            PathSession.MarkDirty();
        }

        /// <summary>Start to end; the width and side come from the brush size or the cursor, as the player set it up.</summary>
        private PathDraft FullDraft(Vector3? cursor, bool blend)
        {
            int side = 0;
            float width = PathSelection.BrushWidth();
            if (cursor.HasValue)
            {
                float lateral = CentreLine.SideOf(points[0], points[1], cursor.Value);
                if (Keys.Held(PathSettings.OneSideModifier))
                    side = lateral >= 0f ? 1 : -1;
                if (PathSettings.WidthFromCursor.Value)
                    width = PathSettings.ClampWidth(side == 0 ? 2f * Mathf.Abs(lateral) : Mathf.Abs(lateral));
            }
            return DraftFactory.Ramp(points[0], points[1], side, blend, width);
        }
    }
}
