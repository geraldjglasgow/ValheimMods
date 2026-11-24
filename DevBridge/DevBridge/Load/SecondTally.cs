using System.Collections.Generic;
using UnityEngine;

namespace DevBridge.Load
{
    /// <summary>
    /// What the fake players were sent during one /load measure, read from each fake's socket counters against its
    /// <see cref="SendMarks"/>. Each second: the bytes per second each fake was sent (all messages, and world data alone:
    /// min, avg, max over the fakes), how many fakes were sent any world data in that second, and ZDOMan's own count of
    /// the ZDOs it sent to everyone in its last second. Over the whole measure: each fake's average per second (all
    /// bytes, world bytes, world messages; world bytes apart again for the fakes that read deltas and those that do
    /// not), min, avg and max over the fakes, and min, avg, max of the per-second count. A
    /// fake that joins during the measure counts from the second after it was first seen, over its own length of time in
    /// the whole-measure figures; one that leaves counts only in the seconds it was there.
    /// </summary>
    internal sealed class SecondTally
    {
        private readonly int session;
        private readonly Stat served = new Stat();
        private float started;

        internal SecondTally(int session) => this.session = session;

        internal List<Dictionary<string, object>> Seconds { get; } = new List<Dictionary<string, object>>();

        internal void Begin()
        {
            started = Time.realtimeSinceStartup;
            foreach (FakePlayer fake in FakeCrowd.All) Mark(fake);
        }

        /// <summary>One second's figures; elapsed is the real time since the last (a long frame makes it more than 1).</summary>
        internal void Second(float elapsed)
        {
            var all = new Stat();
            var world = new Stat();
            int count = 0, sent = 0;
            foreach (FakePlayer fake in FakeCrowd.All)
            {
                SendMarks marks = fake.Marks;
                if (marks.Session != session)
                {
                    Mark(fake);
                    continue;
                }
                all.Add((fake.Socket.Sent - marks.LastSent) / elapsed);
                world.Add((fake.Socket.WorldBytes - marks.LastWorld) / elapsed);
                if (fake.Socket.WorldSends > marks.LastSends) sent++;
                Last(fake);
                count++;
            }
            served.Add(sent);
            Seconds.Add(Record(elapsed, count, all, world, sent));
        }

        /// <summary>Each fake's own average per second over the measure, min, avg and max over the fakes.</summary>
        internal Dictionary<string, object> PerPlayer()
        {
            var all = new Stat();
            var world = new Stat();
            var sends = new Stat();
            var split = new[] { new Stat(), new Stat() };
            float now = Time.realtimeSinceStartup;
            foreach (FakePlayer fake in FakeCrowd.All)
            {
                SendMarks marks = fake.Marks;
                float seconds = now - marks.Since;
                if (marks.Session != session || seconds < 0.5f) continue;
                all.Add((fake.Socket.Sent - marks.Sent) / seconds);
                world.Add((fake.Socket.WorldBytes - marks.World) / seconds);
                split[fake.Deltas ? 1 : 0].Add((fake.Socket.WorldBytes - marks.World) / seconds);
                sends.Add((fake.Socket.WorldSends - marks.Sends) / seconds);
            }
            return new Dictionary<string, object>
            {
                ["players"] = all.Count, ["all_bytes"] = all.Json(), ["world_bytes"] = world.Json(), ["world_messages"] = sends.Json(),
                ["world_bytes_game_format_fakes"] = split[0].Json(), ["world_bytes_delta_fakes"] = split[1].Json(),
            };
        }

        /// <summary>How many fakes were sent world data in a second: min, avg and max over the seconds.</summary>
        internal Dictionary<string, object> Served() => served.Json();

        private Dictionary<string, object> Record(float elapsed, int count, Stat all, Stat world, int sent) => new Dictionary<string, object>
        {
            ["t"] = Stat.R(Time.realtimeSinceStartup - started),
            ["seconds"] = Stat.R(elapsed),
            ["fakes"] = count,
            ["bytes_per_s"] = all.Json(),
            ["world_bytes_per_s"] = world.Json(),
            ["sent_world_data"] = sent,
            ["server_zdos_sent_last_s"] = ZDOMan.instance != null ? ZDOMan.instance.GetSentZDOs() : 0,
        };

        private void Mark(FakePlayer fake)
        {
            SendMarks marks = fake.Marks;
            marks.Session = session;
            marks.Since = Time.realtimeSinceStartup;
            marks.Sent = fake.Socket.Sent;
            marks.World = fake.Socket.WorldBytes;
            marks.Sends = fake.Socket.WorldSends;
            Last(fake);
        }

        private static void Last(FakePlayer fake)
        {
            fake.Marks.LastSent = fake.Socket.Sent;
            fake.Marks.LastWorld = fake.Socket.WorldBytes;
            fake.Marks.LastSends = fake.Socket.WorldSends;
        }
    }
}
