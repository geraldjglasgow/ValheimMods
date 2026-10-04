using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using DevBridge.Events;
using DevBridge.Routes;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Perf
{
    /// <summary>What /perf was asked for.</summary>
    internal sealed class PerfOptions
    {
        internal float Seconds;
        internal string Mod;
        internal int Top;
        internal bool Baseline;
    }

    /// <summary>
    /// One /perf sample from request to reply: plan, probes on (a budget per frame), calibration, frames counted for
    /// the asked seconds, reply, probes off at once (the sample is over, so that hitch costs nothing), event. One runs at
    /// a time. Whatever ends it (the reply, an exception, DevBridge's object going away) takes the probes off.
    /// </summary>
    internal sealed class PerfSession
    {
        private const double BudgetMs = 100;

        internal static PerfSession Current { get; private set; }

        private readonly BridgeRequest request;
        private readonly string stateAtStart = StatusRoute.State();
        private readonly PerfMod calibration;
        private bool stopped;

        internal readonly PerfOptions Options;
        internal readonly PerfPlan Plan;
        internal readonly ProbeSet Probes = new ProbeSet();
        internal readonly FrameSample Frames = new FrameSample();
        internal Calibration.Result Cost;
        internal string CostError;
        internal double ApplySeconds;
        internal int Timed;
        internal int BeatFrame;

        private PerfSession(BridgeRequest request, PerfOptions options)
        {
            this.request = request;
            Options = options;
            if (options.Baseline) return;
            Plan = new PerfPlan(options.Mod);
            calibration = new PerfMod(Plan.Mods.All.Count, "(calibration)");
            PerfProbe.Reset(Thread.CurrentThread.ManagedThreadId, Plan.Mods.All.Count + 1);
        }

        internal static void Start(BridgeRequest request, PerfOptions options)
        {
            if (Current != null)
            {
                request.Fail("a /perf sample is already running; wait for its reply", 409);
                return;
            }
            var session = new PerfSession(request, options);
            session.CheckFilter();
            Current = session;
            session.BeatFrame = Time.frameCount;
            PerfGuard.Watch();
            Async.Start(request, session.Run());
        }

        /// <summary>Stops a running sample from outside its coroutine (see PerfGuard) and fails its request.</summary>
        internal static void Abort(string why)
        {
            PerfSession session = Current;
            if (session == null) return;
            session.Stop();
            Debug.LogWarning($"[DevBridge] perf: {why}; probes removed");
            session.request.Fail("the /perf sample stopped early: " + why, 500);
        }

        private IEnumerator Run()
        {
            try
            {
                foreach (object step in PutOn()) yield return step;
                foreach (object step in Sample()) yield return step;
                Dictionary<string, object> reply = Finish();
                int failedBefore = Probes.Failed.Count;
                // Each unpatch rebuilds a method as patching did, so the probes come off over frames, as they went on.
                Probes.PlanRemoval();
                while (Probes.Step(BudgetMs)) yield return Next();
                Stop();
                Publish(reply, failedBefore);
            }
            finally
            {
                Stop();
            }
        }

        private IEnumerable<object> PutOn()
        {
            if (Plan != null) Probes.Plan(Plan.Targets);
            while (Probes.Step(BudgetMs)) yield return Next();
            if (Plan != null) Calibrate();
            yield return Next();
        }

        private IEnumerable<object> Sample()
        {
            Begin();
            float end = Time.realtimeSinceStartup + Options.Seconds;
            while (Time.realtimeSinceStartup < end)
            {
                yield return Next();
                PerfProbe.SettleMainThread();
                Frames.Frame();
            }
        }

        /// <summary>What every step yields: marks the sample alive for PerfGuard, then waits one frame.</summary>
        private object Next()
        {
            BeatFrame = Time.frameCount;
            return null;
        }

        private void Calibrate()
        {
            try
            {
                Cost = Calibration.Measure(Probes.Harmony, calibration);
            }
            catch (Exception error)
            {
                CostError = $"not measured: {error.GetType().Name}: {error.Message}";
            }
        }

        /// <summary>Zeroes what patching and calibration counted, so the sample holds only its own frames.</summary>
        private void Begin()
        {
            if (Plan != null)
            {
                ApplySeconds = Probes.BusySeconds;
                Timed = Probes.Probed;
                foreach (PerfTarget target in Plan.Targets) target.Counter.Clear();
                foreach (PerfMod mod in Plan.Mods.All) mod.Ticks = 0;
                PerfProbe.SettleMainThread();
            }
            Frames.Begin();
        }

        /// <summary>Replies before the probes come off, so the caller does not wait for that.</summary>
        private Dictionary<string, object> Finish()
        {
            Frames.End();
            Dictionary<string, object> reply = Reply();
            request.Json(reply);
            return reply;
        }

        /// <summary>Once the probes are off: logs any that failed to come off and publishes the event.</summary>
        private void Publish(Dictionary<string, object> reply, int failedBefore)
        {
            for (int i = failedBefore; i < Probes.Failed.Count; i++) Debug.LogWarning("[DevBridge] perf: " + Probes.Failed[i]);
            EventLog.Add("perf", PerfReport.Summary(reply, this));
        }

        private Dictionary<string, object> Reply()
        {
            string state = StatusRoute.State();
            var reply = new Dictionary<string, object>
            {
                ["seconds"] = Options.Seconds,
                ["state"] = state == stateAtStart ? state : $"{stateAtStart} -> {state}",
                ["frames"] = Frames.Frames(),
                ["gc"] = Frames.Gc(),
                ["memory"] = Frames.Memory(),
            };
            if (Plan == null) reply["baseline"] = "frame figures only: nothing was timed";
            else PerfReport.Add(reply, this);
            return reply;
        }

        private void Stop()
        {
            if (stopped) return;
            stopped = true;
            Probes.RemoveAll();
            if (Current == this) Current = null;
        }

        private void CheckFilter()
        {
            if (Plan == null || Options.Mod == null || Plan.Targets.Count > 0 || Plan.Transpilers.Count > 0) return;
            throw new BridgeException($"nothing to time for mod={Options.Mod}; mods with patches or MonoBehaviours: {string.Join(", ", Plan.Seen)}");
        }
    }
}
