using System;
using System.Collections;
using System.Collections.Generic;
using DevBridge.Events;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Timing
{
    /// <summary>step= and step_seconds=: runs the paused clock for some frames or game time, and answers once it is frozen again.</summary>
    internal static class ClockStep
    {
        private const int MaxFrames = 1000;
        private const float MaxSeconds = 60f;

        internal static bool Asked(BridgeRequest request) => request.Has("step") || request.Has("step_seconds");

        /// <summary>Throws for a step that cannot run, before the request changes anything; scale is the one it will run at.</summary>
        internal static void Validate(BridgeRequest request, float scale)
        {
            if (ClockHold.Stepping) throw new BridgeException("a step is still running");
            if (GameClock.Paused) throw new BridgeException("the game's own pause (its menu) holds the clock at 0: close it first");
            if (request.Has("step") && request.Has("step_seconds")) throw new BridgeException("step= and step_seconds= together: choose one");
            if (request.Has("step"))
            {
                Frames(request);
                return;
            }
            float real = Seconds(request) / scale;
            if (real > Timeout(request) - 1f)
                throw new BridgeException($"step_seconds={Seconds(request)} at scale {scale} takes about {Fmt.R(real)} real seconds: " +
                                          "raise timeout= (up to 590) or the scale");
        }

        internal static IEnumerator Run(BridgeRequest request)
        {
            double time = Time.timeAsDouble, fixedTime = Time.fixedTimeAsDouble;
            if (request.Has("step")) ClockHold.StepFrames(Frames(request));
            else ClockHold.StepSeconds(Seconds(request));
            float until = Time.realtimeSinceStartup + Timeout(request);
            while (ClockHold.Stepping && Time.realtimeSinceStartup < until) yield return null;
            bool complete = !ClockHold.Stepping && ClockHold.Held && ClockHold.Paused;
            ClockHold.CancelStep();
            Dictionary<string, object> step = Ran(time, fixedTime, complete);
            Dictionary<string, object> reply = ClockReport.Build();
            reply["step"] = step;
            request.Json(reply);
        }

        /// <summary>What the step ran, also published to the event log.</summary>
        private static Dictionary<string, object> Ran(double time, double fixedTime, bool complete)
        {
            double fixedStep = Math.Max(Time.fixedDeltaTime, 1e-6f);
            var step = new Dictionary<string, object>
            {
                ["frames"] = ClockHold.Stepped,
                ["seconds"] = Math.Round(Time.timeAsDouble - time, 5),
                ["fixed_steps"] = (int)Math.Round((Time.fixedTimeAsDouble - fixedTime) / fixedStep),
                ["complete"] = complete,
            };
            Dictionary<string, object> state = ClockHold.Snapshot();
            state["step"] = step;
            EventLog.Add("time", state);
            return step;
        }

        private static int Frames(BridgeRequest request)
        {
            int frames = request.Int("step", 0);
            if (frames < 1 || frames > MaxFrames) throw new BridgeException($"step= takes 1 to {MaxFrames} frames");
            return frames;
        }

        private static float Seconds(BridgeRequest request)
        {
            float seconds = request.Float("step_seconds", 0f);
            if (seconds <= 0f || seconds > MaxSeconds)
                throw new BridgeException($"step_seconds= takes more than 0 and up to {MaxSeconds} game seconds");
            return seconds;
        }

        /// <summary>Real seconds a step may take; below the HTTP thread's patience (timeout= plus 5, at most 600).</summary>
        private static float Timeout(BridgeRequest request) => Mathf.Clamp(request.Float("timeout", 30f), 2f, 590f);
    }
}
