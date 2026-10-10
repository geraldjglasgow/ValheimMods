using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using DevBridge.Timing;
using UnityEngine;

namespace DevBridge.Director
{
    /// <summary>
    /// Plays one shot: fires its cues in film time order, records it when the plan says so, and when its length is
    /// reached stops the recording and hands everything back (camera, controls, clock, hidden actors, spawned cast).
    /// </summary>
    internal static class ShotRunner
    {
        private static ShotPlan plan;
        private static float now;
        private static int next, startFrame;
        private static bool ending;
        private static readonly List<Cue> Waiting = new List<Cue>();
        private static readonly List<Dictionary<string, object>> Fired = new List<Dictionary<string, object>>();
        private static readonly List<string> Errors = new List<string>();
        private static Dictionary<string, object> result;

        internal static bool Running => plan != null;

        internal static void Start(ShotPlan shot)
        {
            if (plan != null) throw new BridgeException($"shot {plan.Name} is still running; /shot?stop=1 first");
            (plan, now, next, startFrame, result, ending) = (shot, 0f, 0, Time.frameCount, null, false);
            Errors.Clear();
            Waiting.Clear();
            Fired.Clear();
            Cast.Frame = shot.Frame;
            DirectorDriver.Ensure();
            Clean.Set(shot.Clean);
            if (shot.Record != null) Recorder.Start(shot.Record);
            Fire();
        }

        /// <summary>Once a frame: film time on, the cues that are due, the end of the shot.</summary>
        internal static void Tick()
        {
            if (plan == null || Time.frameCount == startFrame) return;
            now += FilmClock.Delta;
            if (now >= plan.Length) Finish("done");
            else Fire();
            if (ending) Finish("cut");
        }

        /// <summary>An end cue: the shot stops once this frame's cues have run; this frame is not recorded.</summary>
        internal static void EndAfterThisFrame() => ending = plan != null;

        private static void Fire()
        {
            while (plan != null && next < plan.Cues.Count && plan.Cues[next].T <= now + 0.0001f)
            {
                Cue cue = plan.Cues[next++];
                if (cue.When != null) Waiting.Add(cue);
                else Run(cue);
            }
            // Every condition is judged before any cue runs, so one cue (a teleport) cannot change another's answer.
            List<Cue> ready = Waiting.ToList().Where(Due).ToList();
            foreach (Cue cue in ready)
            {
                Waiting.Remove(cue);
                if (plan != null) Run(cue);
            }
        }

        private static void Run(Cue cue)
        {
            if (cue.When != null || cue.Kind == "mark")
                Fired.Add(new Dictionary<string, object> { ["t"] = Fmt.R(now), ["kind"] = cue.Kind, ["mark"] = cue.Spec.Value<string>("mark") });
            try
            {
                Cues.Run(cue, plan.Frame, now);
            }
            catch (System.Exception error)
            {
                Report($"{cue.Kind} cue at {cue.T:0.##} s: {(error is BridgeException ? error.Message : error.ToString())}");
            }
        }

        /// <summary>A waiting cue runs once its condition has held for its delay (judged when it first holds).</summary>
        private static bool Due(Cue cue)
        {
            if (cue.HeldAt < 0f && Holds(cue)) cue.HeldAt = now;
            return cue.HeldAt >= 0f && now >= cue.HeldAt + cue.Delay - 0.0001f;
        }

        private static bool Holds(Cue cue)
        {
            try
            {
                return Conditions.Hold(cue.When);
            }
            catch (BridgeException error) when (error.Message.StartsWith("no actor"))
            {
                return false;   // an actor that comes later (a bolt, a thrown axe): keep waiting for it
            }
            catch (BridgeException error)
            {
                Waiting.Remove(cue);
                Report($"{cue.Kind} cue at {cue.T:0.##} s, when: {error.Message}");
                return false;
            }
        }

        internal static void Report(string message)
        {
            if (Errors.Count < 50) Errors.Add(message);
            Debug.LogWarning("[DevBridge] shot: " + message);
        }

        /// <summary>Ends the shot now (stop=1, or its length reached) and puts the world back.</summary>
        internal static Dictionary<string, object> Finish(string why)
        {
            if (plan == null) return Status();
            ShotPlan ended = plan;
            plan = null;
            Dictionary<string, object> recording = null;
            try
            {
                if (ended.Record != null) recording = Recorder.Stop();
            }
            catch (Exception failure)
            {
                Report("recording: " + failure.Message);
            }
            finally
            {
                Restore(ended.Keep);
                result = Describe(ended, why);
                result["recording"] = recording;
            }
            return result;
        }

        internal static void Restore(bool keepCast)
        {
            FocusPull.Stop();
            CameraRig.Off();
            Puppet.Off();
            Hider.Clear();
            Dresser.StopAll();
            Pin.StopAll();
            PlayerSetup.RestoreWalk();
            Props.Clear();
            SetLights.Restore();
            TitleCard.EndShot();
            Eyelids.EndShot();
            Clean.Set(false);
            TimeRamp.Reset("the shot ended");
            if (!keepCast) Cast.Clear();
        }

        private static Dictionary<string, object> Describe(ShotPlan shot, string state) => new Dictionary<string, object>
        {
            ["shot"] = shot.Name,
            ["state"] = state,
            ["time"] = Fmt.R(now),
            ["length"] = Fmt.R(shot.Length),
            ["cuesFired"] = $"{next}/{shot.Cues.Count}",
            ["errors"] = new List<string>(Errors),
            ["fired"] = new List<Dictionary<string, object>>(Fired),
            ["cast"] = Cast.Describe(),
        };

        internal static Dictionary<string, object> Status()
        {
            if (plan == null) return result ?? new Dictionary<string, object> { ["state"] = "idle" };
            Dictionary<string, object> info = Describe(plan, "running");
            if (plan.Record != null) info["recording"] = Recorder.Status();
            return info;
        }
    }

    /// <summary>Slow motion for shots: the held clock's scale eased from where it is to a target over film seconds.</summary>
    internal static class TimeRamp
    {
        private static float from, to, start, over;
        private static bool running;

        internal static void Start(float target, float duration)
        {
            from = ClockHold.Held ? ClockHold.Scale : 1f;
            (to, start, over, running) = (ClockHold.Clamp(target), CameraRig.Clock, duration, duration > 0f);
            if (!running) ClockHold.SetScale(to);
        }

        internal static void Tick()
        {
            if (!running) return;
            float u = Mathf.Clamp01((CameraRig.Clock - start) / over);
            ClockHold.SetScale(Mathf.Lerp(from, to, CameraMove.Eased(u, "inout")));
            if (u >= 1f) running = false;
        }

        internal static void Reset(string why)
        {
            running = false;
            ClockHold.Release(why);
        }
    }
}
