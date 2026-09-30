using OpenKeep.Core;
using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Entry point of the bed feature of section 8: several beds, respawn at the nearest or at the one clicked on the
    /// map after death, a wait that shrinks with the distance, every bed on the map. It binds the settings and the
    /// lines of the countdown on the map (the bed hover uses the game's own "Sleep"); no change handler is needed,
    /// since every patch reads the settings when it runs.
    /// </summary>
    public static class BedFeature
    {
        /// <summary>Under the seconds of the countdown: what happens when they run out.</summary>
        public const string ChoiceNearest = "$ok_bedchoice_nearest";

        /// <summary>The next line: how to choose.</summary>
        public const string ChoiceClick = "$ok_bedchoice_click";

        /// <summary>The last line: the keys that end the choice at once.</summary>
        public const string ChoiceKeys = "$ok_bedchoice_keys";

        public static void Initialize(SyncedConfiguration synced)
        {
            BedSettings.Bind(synced);
            Language.Add("ok_bedchoice_nearest", "seconds until you wake in the nearest bed");
            Language.Add("ok_bedchoice_click", "Click a bed to wake there instead");
            Language.Add("ok_bedchoice_keys", "Map key or Esc: the nearest bed now");
        }
    }
}
