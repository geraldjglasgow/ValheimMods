using System.Collections.Generic;
using BepInEx.Configuration;
using EarthWright.Brush;
using UnityEngine;

namespace EarthWright.Paths
{
    /// <summary>What the preview and the HUD show for the selected ramp or road entry at one moment.</summary>
    public sealed class PathView
    {
        /// <summary>The planned ramp or road, or null while there is nothing to plan yet.</summary>
        public PathPlan Plan;

        /// <summary>The plan is only a hint (the quick ramp's preview) and is drawn faintly.</summary>
        public bool Ghostly;

        /// <summary>"ramp" or "road": the edit source, for the cost check.</summary>
        public string Source;

        public string Title;

        /// <summary>What the next click or key does.</summary>
        public string Next;

        /// <summary>The placed points (ramp start and end, road waypoints), drawn as posts.</summary>
        public readonly List<Vector3> Markers = new List<Vector3>();

        /// <summary>A thin line from the last waypoint to the cursor, where the next waypoint would go.</summary>
        public bool HasTentative;
        public Vector3 TentativeFrom;
        public Vector3 TentativeTo;
    }

    /// <summary>Builds the <see cref="PathView"/> of the ramp or road entry from its tool's points and the cursor.</summary>
    public static class PathViews
    {
        public static PathView Ramp()
        {
            RampTool tool = RampTool.Instance;
            PathView view = new PathView { Source = PathSelection.RampKey };
            view.Markers.AddRange(tool.Points);
            PathDraft draft = tool.Draft(PathSelection.TryCursor(out Vector3 cursor) ? cursor : (Vector3?)null);
            if (draft == null && tool.Count == 0 && PathSettings.QuickRampPreview.Value && QuickRamp.Allowed)
            {
                draft = QuickRamp.Draft();
                view.Ghostly = draft != null;
            }
            view.Plan = draft != null ? PathPlanner.Plan(draft) : null;
            string profile = PathWords.ProfileName(PathSettings.Profile.Value);
            view.Title = PathWords.Format(view.Ghostly ? "title_quick" : "title_ramp", profile);
            view.Next = RampNext(tool.Count);
            return view;
        }

        public static PathView Road()
        {
            RoadTool tool = RoadTool.Instance;
            PathView view = new PathView { Source = PathSelection.RoadKey };
            view.Markers.AddRange(tool.Waypoints);
            PathDraft draft = tool.Draft(false);
            view.Plan = draft != null ? PathPlanner.Plan(draft) : null;
            if (tool.Count > 0 && PathSelection.TryCursor(out Vector3 cursor))
            {
                view.HasTentative = true;
                view.TentativeFrom = tool.Waypoints[tool.Count - 1];
                view.TentativeTo = PathSelection.WithHeight(cursor);
            }
            view.Title = PathWords.Format("title_road", tool.Count);
            view.Next = PathWords.Format("next_road", KeyName(PathSettings.CarveKey), KeyName(PathSettings.CarvePavedKey), KeyName(PathSettings.RemoveKey));
            return view;
        }

        private static string RampNext(int count)
        {
            if (count == 0)
                return QuickRamp.Allowed ? PathWords.Format("next_start", KeyName(PathSettings.QuickRampKey)) : PathWords.Text("next_start_plain");
            if (count == 1)
                return PathWords.Format("next_end", KeyName(PathSettings.RemoveKey));
            return PathWords.Format("next_build", KeyName(PathSettings.OneSideModifier), KeyName(PathSettings.BlendEndsModifier),
                KeyName(ControlSettings.ShapeKey), KeyName(PathSettings.RemoveKey));
        }

        private static string KeyName(ConfigEntry<KeyboardShortcut> key) => key.Value.ToString();
    }
}
