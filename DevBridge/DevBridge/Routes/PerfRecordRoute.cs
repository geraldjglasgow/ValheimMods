using System.Collections;
using System.Collections.Generic;
using DevBridge.Perf;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Routes
{
    /// <summary>/perf/record: /perf's timing kept on for minutes or hours, every frame written to disk for reading later.</summary>
    internal static class PerfRecordRoute
    {
        private const string Help =
            "/perf/record?start=1&mod=Name&seconds=0&max_mb=1024&budget=2&min_us=0&name=\n" +
            "                       times what /perf times, every frame, into %LOCALAPPDATA%\\DevBridge\\perf\\<time>[-name]\\ (index.json,\n" +
            "                       frames.bin, entries.bin; read with DevBridge/examples/perf/perfrec.py); replies at once; probes go\n" +
            "                       on and off at budget= ms per frame (0.5-100) so the game barely notices; ends at stop=1, seconds=\n" +
            "                       (0: no limit), max_mb= (max 16384) or quit; min_us= leaves calls under it out of the frames file\n" +
            "/perf/record?stop=1    ends the recording; replies once its files are closed (the probes then come off over frames)\n" +
            "/perf/record           the running or last recording: state, probes on, frames, MB, fps, top mods";

        internal static void Register(Router router) => router.Add("/perf/record", Help, Handle);

        private static void Handle(BridgeRequest request)
        {
            if (request.Flag("start")) Start(request);
            else if (request.Flag("stop")) Stop(request);
            else Status(request);
        }

        private static void Start(BridgeRequest request)
        {
            if (PerfRecording.Current != null || PerfSession.Current != null)
            {
                request.Fail(PerfRecording.Current != null ? "a recording is already running; /perf/record?stop=1 first" : "a /perf sample is running; wait for its reply", 409);
                return;
            }
            PerfRecording recording = PerfRecording.Start(new RecordOptions
            {
                Mod = request.Get("mod"),
                Seconds = Mathf.Max(0f, request.Float("seconds", 0f)),
                MaxMb = Mathf.Clamp(request.Int("max_mb", 1024), 1, 16384),
                BudgetMs = Mathf.Clamp(request.Float("budget", 2f), 0.5f, 100f),
                MinMicros = Mathf.Max(0f, request.Float("min_us", 0f)),
                Name = request.Get("name"),
            });
            request.Json(recording.Summary());
        }

        private static void Stop(BridgeRequest request)
        {
            PerfRecording recording = PerfRecording.Current;
            if (recording == null)
            {
                request.Fail("no recording is running", 409);
                return;
            }
            recording.AskStop();
            Async.Start(request, Closed(request, recording));
        }

        /// <summary>Waits for the files to close (at most a few frames), then replies with the summary.</summary>
        private static IEnumerator Closed(BridgeRequest request, PerfRecording recording)
        {
            while (recording.State != "removing probes" && recording.State != "done") yield return null;
            request.Json(recording.Summary());
        }

        private static void Status(BridgeRequest request)
        {
            PerfRecording recording = PerfRecording.Current ?? PerfRecording.Last;
            if (recording == null) request.Json(new { state = "none", note = "nothing recorded since the game started: /perf/record?start=1" });
            else request.Json(Full(recording));
        }

        private static Dictionary<string, object> Full(PerfRecording recording)
        {
            Dictionary<string, object> reply = recording.Summary();
            reply["probing"] = recording.Probing();
            return reply;
        }
    }
}
