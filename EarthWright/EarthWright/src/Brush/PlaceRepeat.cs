using EarthWright.Actions;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// Hold-to-repeat for brush entries: while the place button stays held, after the start delay and then at the repeat
    /// rate, the game's own placement is triggered again (<c>Player.m_placePressedTime</c>), so every repeat is a normal
    /// click with the game's costs and the full edit pipeline. When the rate is faster than the player's place delay,
    /// the delay is lowered while repeating and put back afterwards. Not for ramps and roads (clicks are their points).
    /// Local player only.
    /// </summary>
    public static class PlaceRepeat
    {
        private static float nextAt = float.MaxValue;
        private static Player lowered;
        private static float savedDelay;

        public static void Update(Player player, ToolAction action)
        {
            if (player == null || action == null || action.IsSpecial || !ControlSettings.HoldToRepeat.Value || !InputGate.Open || !Held())
            {
                Stop();
                return;
            }
            float now = Time.time;
            if (PressedNow() || nextAt == float.MaxValue)
            {
                nextAt = now + Paced(ControlSettings.RepeatDelay.Value);
                return;
            }
            if (now < nextAt)
                return;
            LowerDelay(player);
            player.m_placePressedTime = now;
            nextAt = now + Paced(ControlSettings.RepeatRate.Value);
        }

        /// <summary>
        /// Never faster than the server's terrain cooldown (Costs, "Cooldown"): a repeat inside the wait would only be
        /// refused with a "wait" message, several times a second while the button is held.
        /// </summary>
        private static float Paced(float seconds)
        {
            float cooldown = Costs.CostSettings.Cooldown != null ? Costs.CostSettings.Cooldown.Value : 0f;
            return cooldown > 0f ? Mathf.Max(seconds, cooldown + 0.05f) : seconds;
        }

        /// <summary>Ends repeating and gives the player back their own place delay.</summary>
        public static void Stop()
        {
            nextAt = float.MaxValue;
            RestoreDelay();
        }

        private static void LowerDelay(Player player)
        {
            float rate = Paced(ControlSettings.RepeatRate.Value);
            if (lowered == player || player.m_placeDelay <= rate)
                return;
            RestoreDelay();
            lowered = player;
            savedDelay = player.m_placeDelay;
            player.m_placeDelay = rate;
        }

        private static void RestoreDelay()
        {
            if (lowered != null)
                lowered.m_placeDelay = savedDelay;
            lowered = null;
        }

        private static bool Held() => !ZInput.GetButton("JoyAltKeys") && (ZInput.GetButton("Attack") || ZInput.GetButton("JoyPlace"));

        private static bool PressedNow() => ZInput.GetButtonDown("Attack") || ZInput.GetButtonDown("JoyPlace");
    }
}
