using System.Collections.Generic;
using DevBridge.Events;
using UnityEngine;

namespace DevBridge.Timing
{
    /// <summary>
    /// DevBridge's hold on this machine's game clock: the scale it runs at, a pause, and the frames or game time a step
    /// has left. While held, ClockDriver writes the scale each frame after the game has written its own.
    /// </summary>
    internal static class ClockHold
    {
        internal const float MinScale = 0.01f;
        internal const float MaxScale = 4f;

        /// <summary>A step of game seconds ends once less than this is left (half a millisecond).</summary>
        private const double SeekTolerance = 0.0005;

        private static int framesLeft;
        private static bool seeking;
        private static double seekUntil;
        private static int lastStepFrame;
        private static float gameFixedDelta;

        internal static bool Held { get; private set; }
        internal static bool Paused { get; private set; }

        /// <summary>The scale the held clock runs at when not paused, and the one a step runs at.</summary>
        internal static float Scale { get; private set; } = 1f;

        /// <summary>Frames the current or last step ran.</summary>
        internal static int Stepped { get; private set; }

        /// <summary>True until the last frame of a step has run.</summary>
        internal static bool Stepping => framesLeft > 0 || seeking || Time.frameCount < lastStepFrame;

        /// <summary>The scale a pause would remember: the held one, or the one the game runs at when not paused.</summary>
        internal static float RunScale => Held ? Scale : Clamp(GameClock.Running);

        /// <summary>The game's own fixed time step, the one reset puts back.</summary>
        internal static float GameFixedDelta => Held ? gameFixedDelta : Time.fixedDeltaTime;

        /// <summary>The scale the clock runs at from the next frame: nothing while either the game or DevBridge pauses it.</summary>
        internal static float Current => !Held ? GameClock.Scale : GameClock.Paused || Paused ? 0f : Scale;

        /// <summary>Into 0.01-4; a scale that is not above 0 (the game paused, or its timescale 0) means 1.</summary>
        internal static float Clamp(float scale) => scale > 0f ? Mathf.Clamp(scale, MinScale, MaxScale) : 1f;

        internal static void SetScale(float scale)
        {
            bool changed = !Held || Scale != Clamp(scale);
            Take();
            Scale = Clamp(scale);
            ApplyFixedDelta();
            if (changed) Publish();
        }

        internal static void Pause()
        {
            bool changed = !Paused;
            Take();
            Paused = true;
            if (changed) Publish();
        }

        internal static void Resume()
        {
            if (!Held || !Paused) return;
            CancelStep();
            Paused = false;
            Publish();
        }

        /// <summary>Pauses, then lets the next frames run at the held scale.</summary>
        internal static void StepFrames(int frames)
        {
            Pause();
            Stepped = 0;
            framesLeft = frames;
        }

        /// <summary>Pauses, then lets the clock run at the held scale until that much game time has passed.</summary>
        internal static void StepSeconds(double seconds)
        {
            Pause();
            Stepped = 0;
            seekUntil = Time.timeAsDouble + seconds;
            seeking = true;
        }

        internal static void CancelStep()
        {
            framesLeft = 0;
            seeking = false;
            lastStepFrame = 0;
        }

        /// <summary>Hands the clock back: the game's own scale from the next frame on, and its own fixed time step.</summary>
        internal static void Release(string why)
        {
            if (!Held) return;
            CancelStep();
            Held = false;
            Paused = false;
            Scale = 1f;
            Time.fixedDeltaTime = gameFixedDelta;
            Time.timeScale = GameClock.Scale;
            Publish(why + ", the clock is the game's again");
        }

        /// <summary>Called once a frame by ClockDriver: the scale the next frame runs at, counting a step down.</summary>
        internal static float NextFrame()
        {
            if (!Paused) return Scale;
            if (framesLeft <= 0) return seeking ? SeekShare() : 0f;
            framesLeft--;
            return StepFrame(Scale);
        }

        /// <summary>The scale that brings the clock to the step's end in the next frame, judged by this frame's length.</summary>
        private static float SeekShare()
        {
            double left = seekUntil - Time.timeAsDouble;
            if (left < SeekTolerance)
            {
                seeking = false;
                return 0f;
            }
            float frame = Mathf.Max(Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime), 0.001f);
            return StepFrame(Mathf.Min(Scale, (float)(left / frame)));
        }

        private static float StepFrame(float scale)
        {
            Stepped++;
            lastStepFrame = Time.frameCount + 1;
            return scale;
        }

        /// <summary>Starts holding at the scale the game runs at, keeping the game's fixed time step to put back.</summary>
        private static void Take()
        {
            if (Held) return;
            Scale = RunScale;
            gameFixedDelta = Time.fixedDeltaTime;
            Held = true;
            ApplyFixedDelta();
            ClockDriver.Ensure();
        }

        /// <summary>
        /// Physics steps shrink with slow motion, so bodies move a little every frame instead of jumping every few frames;
        /// they never grow past the game's own, so speed-up stays as stable as normal play.
        /// </summary>
        private static void ApplyFixedDelta() => Time.fixedDeltaTime = gameFixedDelta * Mathf.Min(1f, Scale);

        /// <summary>What a "time" event carries: the scale the clock runs at when not paused, the pause, the hold.</summary>
        internal static Dictionary<string, object> Snapshot() => new Dictionary<string, object>
        {
            ["scale"] = Fmt.R(Held ? Scale : GameClock.Scale),
            ["paused"] = Paused,
            ["held"] = Held,
        };

        private static void Publish(string why = null)
        {
            Dictionary<string, object> state = Snapshot();
            EventLog.Add("time", state);
            Debug.Log($"[DevBridge] time: {why ?? "held"}, scale {state["scale"]}{(Paused ? ", paused" : "")}");
        }
    }
}
