using System.Collections;
using System.Collections.Generic;
using DevBridge.Events;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Load
{
    /// <summary>
    /// One /load?measure= sample on this game (the server, or a player's game to see its own frames beside a crowd):
    /// <see cref="SyncTimer"/> on, then until the asked seconds have passed, every frame's time (Unity's unscaled frame
    /// time) and the time ZDOMan.Update took in it, and at each second the fakes' sends (<see cref="SecondTally"/>) and
    /// HighSeat's SyncStats and DeltaStats (<see cref="HighSeatStats"/>); a second is at least one second of real time,
    /// so a long frame makes one second longer rather than several empty ones. Then the reply, the timer off and a
    /// "load" event. One runs at a time; whatever ends it (the reply, an exception, DevBridge going away) takes the timer
    /// off.
    /// </summary>
    internal sealed class LoadMeasure
    {
        private static LoadMeasure current;
        private static int sessions;
        private readonly BridgeRequest request;
        private readonly int seconds;
        private readonly List<float> frameMs = new List<float>(8192);
        private readonly List<float> syncMs = new List<float>(8192);
        private readonly SecondTally tally = new SecondTally(++sessions);
        private readonly HighSeatStats highSeat = new HighSeatStats();
        private readonly HighSeatStats deltas = new HighSeatStats(HighSeatStats.DeltaStats);

        private LoadMeasure(BridgeRequest request, int seconds)
        {
            this.request = request;
            this.seconds = seconds;
        }

        internal static bool Running => current != null;

        internal static void Start(BridgeRequest request, int seconds)
        {
            if (!ZNet.instance) throw new BridgeException("no world is loaded: /load measures a running world");
            if (current != null)
            {
                request.Fail("a /load measure is already running; wait for its reply", 409);
                return;
            }
            current = new LoadMeasure(request, seconds);
            Async.Start(request, current.Run());
        }

        private IEnumerator Run()
        {
            try
            {
                SyncTimer.Start();
                tally.Begin();
                foreach (object frame in Sample()) yield return frame;
                Finish();
            }
            finally
            {
                SyncTimer.Stop();
                current = null;
            }
        }

        // Frame by frame until the asked seconds have been taken.
        private IEnumerable<object> Sample()
        {
            float last = Time.realtimeSinceStartup;
            for (int taken = 0; taken < seconds;)
            {
                yield return null;
                frameMs.Add(Time.unscaledDeltaTime * 1000f);
                syncMs.Add(SyncTimer.TakeFrameMs());
                float now = Time.realtimeSinceStartup;
                if (now - last < 1f) continue;
                tally.Second(now - last);
                highSeat.Sample();
                deltas.Sample();
                last = now;
                taken++;
            }
        }

        private void Finish()
        {
            Dictionary<string, object> reply = LoadReport.Measured(seconds, frameMs, syncMs, tally, highSeat);
            reply["highseat_deltas"] = deltas.Report();
            request.Json(reply);
            EventLog.Add("load", LoadReport.MeasuredSummary(reply));
        }
    }
}
