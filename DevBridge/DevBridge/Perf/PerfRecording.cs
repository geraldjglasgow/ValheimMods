using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DevBridge.Events;
using UnityEngine;

namespace DevBridge.Perf
{
    /// <summary>What /perf/record was asked for.</summary>
    internal sealed class RecordOptions
    {
        internal string Mod;
        internal float Seconds;
        internal int MaxMb;
        internal double BudgetMs;
        internal double MinMicros;
        internal string Name;
    }

    /// <summary>
    /// A long /perf: every frame's timed calls to disk until stopped, for reading afterwards (examples/perf/perfrec.py).
    /// The plan is made on a worker thread; the probes go on and come off at a small budget per frame, so the game barely
    /// notices either; each frame only copies counters into a chunk the writer thread puts on disk. One recording at a
    /// time, never beside a /perf sample (they share the probes). Quitting the game closes the files and leaves the probes.
    /// </summary>
    internal sealed class PerfRecording
    {
        internal static PerfRecording Current { get; private set; }
        internal static PerfRecording Last { get; private set; }

        private readonly ModDirectory mods = new ModDirectory();
        private readonly ProbeSet probes = new ProbeSet();
        private volatile bool planned;
        private string planError;
        private RecordWriter writer;
        private RecordStream frames;
        private RecordStream entries;
        private RecordFrames recorder;
        private Calibration.Result cost;
        private string costError;
        private bool stopAsked;
        private bool filesClosed;
        private bool closed;
        private float flushAt;

        internal readonly RecordOptions Options;
        internal readonly string Folder;
        internal readonly DateTime Started = DateTime.Now;
        internal DateTime Ended;
        internal PerfPlan Plan { get; private set; }
        internal string State { get; private set; } = "planning";
        internal string Status { get; private set; } = "recording";
        internal int BeatFrame;

        private PerfRecording(RecordOptions options)
        {
            Options = options;
            Folder = RecordIndex.NewFolder(options.Name);
        }

        internal static PerfRecording Start(RecordOptions options)
        {
            var recording = new PerfRecording(options);
            Current = recording;
            recording.BeatFrame = Time.frameCount;
            PerfGuard.Watch();
            DevBridgePlugin.Instance.StartCoroutine(recording.Run());
            return recording;
        }

        internal void AskStop() => stopAsked = true;

        internal bool Silent(int frames) => Time.frameCount - BeatFrame > frames;

        /// <summary>Ends it from outside its coroutine (see PerfGuard): files closed, probes off at once.</summary>
        internal void Abort(string why)
        {
            Debug.LogWarning($"[DevBridge] perf record: {why}; probes removed");
            Close("aborted: " + why, true);
        }

        /// <summary>The game is quitting: the files are closed so they stay readable; the probes go with the process.</summary>
        internal void AtQuit() => Close("stopped at quit", false);

        private IEnumerator Run()
        {
            try
            {
                foreach (object step in Planned()) yield return step;
                if (Status == "recording") foreach (object step in PutOn()) yield return step;
                if (Status == "recording") foreach (object step in Record()) yield return step;
                foreach (object step in TakeOff()) yield return step;
            }
            finally
            {
                Close(Status == "recording" ? "stopped" : Status, true);
            }
        }

        private IEnumerable<object> Planned()
        {
            ThreadPool.QueueUserWorkItem(_ => MakePlan());
            while (!planned) yield return Next();
            string problem = planError ?? Problem();
            if (problem == null) problem = Open();
            if (problem != null) Status = "failed: " + problem;
        }

        /// <summary>On a worker: reflection only. The mods were found on the main thread, which asks Unity which plugins live.</summary>
        private void MakePlan()
        {
            try
            {
                var plan = new PerfPlan(Options.Mod, mods);
                foreach (PerfTarget target in plan.Targets) _ = target.MayBeInlined;
                Plan = plan;
            }
            catch (Exception error)
            {
                planError = $"planning: {error.GetType().Name}: {error.Message}";
            }
            finally
            {
                planned = true;
            }
        }

        private string Problem()
        {
            if (Plan.Targets.Count == 0) return $"nothing to time for mod={Options.Mod}; mods seen: {string.Join(", ", Plan.Seen)}";
            if (Plan.Targets.Count > ushort.MaxValue) return $"{Plan.Targets.Count} methods, more than a recording can name";
            return null;
        }

        private string Open()
        {
            try
            {
                PerfProbe.Reset(Thread.CurrentThread.ManagedThreadId, Plan.Mods.All.Count + 1);
                writer = new RecordWriter();
                frames = new RecordStream(writer, Path.Combine(Folder, RecordIndex.FramesFile), 1 << 16);
                entries = new RecordStream(writer, Path.Combine(Folder, RecordIndex.EntriesFile), 1 << 20);
                recorder = new RecordFrames(Plan, Options.MinMicros);
                RecordIndex.Write(this);
                return null;
            }
            catch (Exception error)
            {
                return $"opening the files: {error.GetType().Name}: {error.Message}";
            }
        }

        private IEnumerable<object> PutOn()
        {
            State = "probing";
            probes.Plan(Plan.Targets);
            while (!stopAsked && probes.Step(Options.BudgetMs)) yield return Next();
            if (stopAsked) Status = "stopped while probing";
            else Calibrate();
            yield return Next();
        }

        private void Calibrate()
        {
            try
            {
                cost = Calibration.Measure(probes.Harmony, new PerfMod(Plan.Mods.All.Count, "(calibration)"));
            }
            catch (Exception error)
            {
                costError = $"not measured: {error.GetType().Name}: {error.Message}";
            }
        }

        private IEnumerable<object> Record()
        {
            State = "recording";
            recorder.Begin();
            RecordIndex.Write(this);
            float end = Options.Seconds > 0 ? Time.realtimeSinceStartup + Options.Seconds : float.MaxValue;
            flushAt = Time.realtimeSinceStartup + 1f;
            while (Status == "recording")
            {
                yield return Next();
                Step(end);
            }
        }

        private void Step(float end)
        {
            try
            {
                recorder.Write(frames, entries);
                if (Time.realtimeSinceStartup >= flushAt) Flush();
                Status = Over(end) ?? Status;
            }
            catch (Exception error)
            {
                Status = $"failed: {error.GetType().Name}: {error.Message}";
            }
        }

        private string Over(float end)
        {
            if (stopAsked) return "stopped";
            if (Time.realtimeSinceStartup >= end) return $"seconds={Options.Seconds} reached";
            if (writer.Error != null) return "failed writing: " + writer.Error;
            return Bytes >= Options.MaxMb * 1048576L ? $"max_mb={Options.MaxMb} reached" : null;
        }

        private void Flush()
        {
            frames.Flush();
            entries.Flush();
            flushAt = Time.realtimeSinceStartup + 1f;
        }

        /// <summary>The files are finished before the probes come off, so they can be read while that runs.</summary>
        private IEnumerable<object> TakeOff()
        {
            CloseFiles(Status);
            State = "removing probes";
            probes.PlanRemoval();
            while (probes.Step(Options.BudgetMs)) yield return Next();
        }

        private void Close(string status, bool unprobe)
        {
            if (closed) return;
            closed = true;
            // after the slow take-off none is left, and RemoveAll's sweep over every patched method would hitch by itself
            if (unprobe && probes.Probed > 0) probes.RemoveAll();
            CloseFiles(status);
            State = "done";
            if (Current == this) Current = null;
            Last = this;
            for (int i = 0; i < probes.Failed.Count && i < 20; i++) Debug.LogWarning("[DevBridge] perf record: " + probes.Failed[i]);
            EventLog.Add("perf", Summary());
        }

        private void CloseFiles(string status)
        {
            if (filesClosed) return;
            filesClosed = true;
            Status = status;
            Ended = DateTime.Now;
            if (writer != null) Finish();
            try
            {
                RecordIndex.Write(this);
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[DevBridge] perf record: index not written: {error.Message}");
            }
        }

        private void Finish()
        {
            frames.Flush();
            entries.Flush();
            writer.Close();
            frames.File.Dispose();
            entries.File.Dispose();
        }

        /// <summary>What every step yields: marks the recording alive for PerfGuard, then waits one frame.</summary>
        private object Next()
        {
            BeatFrame = Time.frameCount;
            return null;
        }

        private long Bytes => (frames?.Bytes ?? 0) + (entries?.Bytes ?? 0);

        internal Dictionary<string, object> Probing() => new Dictionary<string, object>
        {
            ["methods"] = Plan?.Targets.Count ?? 0,
            ["on"] = probes.Probed,
            ["queued"] = probes.Pending,
            ["seconds"] = Math.Round(probes.BusySeconds, 2),
            ["wrappers_rebuilt"] = probes.Rebuilt,
            ["failed"] = probes.Failed.Take(50).ToList(),
            ["extra_us_per_call"] = cost == null ? null : (object)Math.Round(cost.ExtraMicroseconds, 3),
            ["empty_reads_us"] = cost == null ? null : (object)Math.Round(cost.FloorMicroseconds, 3),
            ["calibration"] = costError,
        };

        internal Dictionary<string, object> Summary() => RecordSummary.Of(this, recorder, Bytes);
    }
}
