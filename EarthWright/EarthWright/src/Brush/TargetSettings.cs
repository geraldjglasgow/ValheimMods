using BepInEx.Configuration;
using EarthWright.Core;
using SyncedConfig;

namespace EarthWright.Brush
{
    /// <summary>The live target modes a player can start in or cycle through (the fixed ones are set by keys).</summary>
    public enum StartTarget
    {
        Feet = 0,
        Aimed = 1,
        Continued = 4,
    }

    /// <summary>
    /// Section "2. Target Height": how the level target is found and how the height keys step. Every key here is a
    /// personal preference of how the player aims (the edit is the same whichever way the height was picked), so all
    /// are local.
    /// </summary>
    public static class TargetSettings
    {
        public static ConfigEntry<StartTarget> StartMode { get; private set; }
        public static ConfigEntry<float> ContinueTolerance { get; private set; }
        public static ConfigEntry<float> HeightStep { get; private set; }
        public static ConfigEntry<float> ExactStep { get; private set; }
        public static ConfigEntry<float> ExactFastStep { get; private set; }
        public static ConfigEntry<bool> LockMessage { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            StartMode = synced.Bind(Sections.Target, "Default Target", StartTarget.Feet,
                "Where levelling takes its height from when you log in and after the back-to-feet key. Feet: the ground under you (as the unmodded game). Aimed: the ground under the crosshair. Continued: the height of earlier flat edits next to the crosshair.",
                synced: false);
            ContinueTolerance = synced.Bind(Sections.Target, "Continue Tolerance", 0.25f,
                "Continue the flat: earlier edits near the crosshair count as one flat when their heights differ by at most this many metres. When two flats disagree the crosshair height is used instead.",
                synced: false, acceptableValues: new AcceptableValueRange<float>(0.01f, 5f));
            HeightStep = synced.Bind(Sections.Target, "Height Key Step", 0.1f,
                "Metres the locked height moves per press of the height up/down keys (Shift: times the fast step multiplier).",
                synced: false, acceptableValues: new AcceptableValueRange<float>(0.01f, 10f));
            ExactStep = synced.Bind(Sections.Target, "Exact Height Step", 0.25f,
                "Metres the exact target height moves per step when it is the selected brush value.",
                synced: false, acceptableValues: new AcceptableValueRange<float>(0.01f, 10f));
            ExactFastStep = synced.Bind(Sections.Target, "Exact Height Fast Step", 2f,
                "Metres the exact target height moves per step while the fast modifier (Ctrl) is held.",
                synced: false, acceptableValues: new AcceptableValueRange<float>(0.01f, 50f));
            LockMessage = synced.Bind(Sections.Target, "Show Target Messages", true,
                "Shows a message in the middle of the screen when the target height is locked, copied from a floor or released.",
                synced: false);
        }
    }
}
