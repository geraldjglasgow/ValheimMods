using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevBridge.Timing
{
    /// <summary>The clock as /time replies with it. Times keep more decimals than Fmt.R, so a single frame shows.</summary>
    internal static class ClockReport
    {
        internal static Dictionary<string, object> Build()
        {
            var reply = new Dictionary<string, object>
            {
                ["held"] = ClockHold.Held,
                ["scale"] = Fmt.R(ClockHold.Current),
                ["paused"] = ClockHold.Paused,
                ["resume_scale"] = Fmt.R(ClockHold.RunScale),
                ["game_paused"] = GameClock.Paused,
                ["game_scale"] = Fmt.R(GameClock.Scale),
                ["frame"] = Time.frameCount,
                ["time"] = Math.Round(Time.timeAsDouble, 4),
                ["fixed_delta_time"] = Math.Round(Time.fixedDeltaTime, 5),
                ["game_fixed_delta_time"] = Math.Round(ClockHold.GameFixedDelta, 5),
                ["network"] = ClockNetwork.Role(),
                ["peers"] = ClockNetwork.Peers,
            };
            bool drifting = ClockHold.Held && (ClockHold.Paused || ClockHold.Scale != 1f);
            string warning = drifting ? ClockNetwork.Warning() : null;
            if (warning != null) reply["warning"] = warning;
            return reply;
        }
    }
}
