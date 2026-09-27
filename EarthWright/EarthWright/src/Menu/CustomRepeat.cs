using EarthWright.Actions;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Menu
{
    /// <summary>
    /// One run per press, and "repeat while held". After a custom entry ran, further clicks are ignored until the place
    /// button is let go (so a click repeated by the brush's own hold-to-repeat does not run a non-repeating command
    /// again). A repeating entry runs again every "Custom Entry Repeat Interval" seconds while the button stays down and
    /// the entry stays selected with the menu closed.
    /// </summary>
    public static class CustomRepeat
    {
        private static CustomEntry repeating;
        private static ToolAction repeatingAction;
        private static float nextRun;

        /// <summary>A custom entry ran and the place button has not been let go since.</summary>
        public static bool HeldSinceRun { get; private set; }

        public static void Initialize() => Ticker.OnUpdate("EarthWright custom entry repeat", Tick);

        public static void Started(CustomEntry entry, ToolAction action)
        {
            HeldSinceRun = true;
            repeating = entry.Repeat ? entry : null;
            repeatingAction = action;
            nextRun = Time.time + MenuSettings.CustomRepeatInterval.Value;
        }

        private static void Tick()
        {
            if (!HeldSinceRun)
                return;
            if (!PlaceHeld)
            {
                HeldSinceRun = false;
                repeating = null;
                return;
            }
            if (repeating == null || Time.time < nextRun)
                return;
            // Inside the terrain cooldown a run would be refused and the repeat would stop; wait for it instead.
            if (Costs.CostApi.CooldownRemaining > 0f)
                return;
            nextRun = Time.time + MenuSettings.CustomRepeatInterval.Value;
            if (ActionCatalog.Current != repeatingAction || !RunAgain())
                repeating = null;
        }

        private static bool RunAgain()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
                return false;
            GameObject ghost = LocalTool.Ghost;
            Vector3 position = ghost != null ? ghost.transform.position : player.transform.position;
            return CustomCommand.Run(player, repeatingAction, repeating, position);
        }

        private static bool PlaceHeld => ZInput.GetButton("Attack") || ZInput.GetButton("JoyPlace");
    }
}
