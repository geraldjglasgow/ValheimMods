using EarthWright.Actions;
using EarthWright.Brush;
using EarthWright.Core;

namespace EarthWright.Paths
{
    /// <summary>
    /// The module's keys, read once a frame while a terrain entry is selected and the player takes input: the quick
    /// ramp with any terrain entry; the brush's shape key (the ramp profile) and the remove key with the ramp entry; the carve keys and the remove
    /// key with the road entry. Nothing fires while text is being typed.
    /// </summary>
    public static class PathInput
    {
        public static void Handle(ToolAction current)
        {
            if (current == null || !PathSelection.InputAllowed)
                return;
            if (Keys.Pressed(PathSettings.QuickRampKey))
                QuickRamp.Build();
            if (PathSelection.IsRamp(current))
                HandleRamp();
            else if (PathSelection.IsRoad(current))
                HandleRoad(current);
        }

        private static void HandleRamp()
        {
            if (Keys.Pressed(ControlSettings.ShapeKey))
                CycleProfile();
            if (Keys.Pressed(PathSettings.RemoveKey))
                RampTool.Instance.RemoveLast();
        }

        private static void HandleRoad(ToolAction current)
        {
            if (Keys.Pressed(PathSettings.CarvePavedKey))
                RoadTool.Instance.Carve(current, true);
            else if (Keys.Pressed(PathSettings.CarveKey))
                RoadTool.Instance.Carve(current, false);
            if (Keys.Pressed(PathSettings.RemoveKey))
                RoadTool.Instance.RemoveLast();
        }

        /// <summary>The next profile; kept in the player's own config so it survives a restart.</summary>
        private static void CycleProfile()
        {
            RampProfile next = ProfileCurve.Next(PathSettings.Profile.Value);
            PathSettings.Profile.Value = next;
            Messages.TopLeft(PathWords.Format("profile", PathWords.ProfileName(next)));
            PathSession.MarkDirty();
        }
    }
}
