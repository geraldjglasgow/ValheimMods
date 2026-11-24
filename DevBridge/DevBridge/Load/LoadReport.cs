using System.Collections.Generic;
using DevBridge.Timing;
using UnityEngine;

namespace DevBridge.Load
{
    /// <summary>The /load replies: the fakes now (status), a batch added, and a measure's figures.</summary>
    internal static class LoadReport
    {
        internal static Dictionary<string, object> Status()
        {
            List<FakePlayer> fakes = FakeCrowd.All;
            var rows = new List<object>(fakes.Count);
            long sent = 0, world = 0;
            int spread = 0;
            foreach (FakePlayer fake in fakes)
            {
                sent += fake.Socket.Sent;
                world += fake.Socket.WorldBytes;
                if (fake.Mode == LoadArgs.Spread) spread++;
                rows.Add(Row(fake));
            }
            Dictionary<string, object> status = World();
            status["fake_players"] = fakes.Count;
            status["modes"] = new Dictionary<string, object> { [LoadArgs.Spread] = spread, [LoadArgs.Crowd] = fakes.Count - spread };
            status["sent_bytes"] = sent;
            status["world_bytes"] = world;
            status["patched"] = LoadPatches.On;
            status["measuring"] = LoadMeasure.Running;
            status["fakes"] = rows;
            return status;
        }

        private static Dictionary<string, object> Row(FakePlayer fake) => new Dictionary<string, object>
        {
            ["name"] = fake.Name,
            ["uid"] = fake.Uid,
            ["mode"] = fake.Mode,
            ["position"] = Fmt.V3(fake.Peer.m_refPos),
            ["loop"] = new Dictionary<string, object>
            {
                ["centre"] = Fmt.V3(fake.Path.Centre), ["radius"] = Fmt.R(fake.Path.Radius), ["speed"] = Fmt.R(fake.Path.Speed),
            },
            ["joined_s_ago"] = Fmt.R(Time.time - fake.Joined),
            ["sent_bytes"] = fake.Socket.Sent,
            ["world_bytes"] = fake.Socket.WorldBytes,
            ["world_messages"] = fake.Socket.WorldSends,
            ["deltas"] = fake.Deltas,
            ["delta_world_bytes"] = fake.Socket.DeltaWorldBytes,
            ["verifies"] = fake.Socket.Shadow != null,
            ["body"] = fake.Body != null ? fake.BodyId.ToString() : null,
        };

        /// <summary>A batch: how many joined out of how many were asked for, and why any are missing.</summary>
        internal static Dictionary<string, object> Added(LoadArgs args, int wanted, int placed, int joined, int unexplored, string note, double ms)
        {
            Dictionary<string, object> reply = Status();
            reply["asked"] = new Dictionary<string, object>
            {
                ["players"] = args.Players, ["mode"] = args.Mode, ["radius"] = args.Radius, ["speed"] = args.Speed, ["new"] = wanted,
                ["deltas"] = args.Deltas, ["verify"] = args.Verify,
            };
            reply["joined"] = joined;
            reply["join_ms"] = Stat.R(ms);
            if (args.Mode == LoadArgs.Spread) reply["spots_outside_explored_land"] = unexplored;
            if (placed < wanted) note = (note == null ? "" : note + "; ") + $"only {placed} of {wanted} spots of land were found";
            if (unexplored > 0) reply["warning"] = $"{unexplored} fake(s) stand where the world has not been generated yet: the server " +
                                                   "generates the zones round them as for any player, and the world save keeps them";
            if (note != null) reply["note"] = note;
            return reply;
        }

        internal static Dictionary<string, object> Measured(int seconds, List<float> frameMs, List<float> syncMs, SecondTally tally,
            HighSeatStats highSeat)
        {
            Dictionary<string, object> reply = World();
            reply["seconds"] = seconds;
            reply["fake_players"] = FakeCrowd.All.Count;
            reply["zdos"] = ZDOMan.instance != null ? ZDOMan.instance.NrOfObjects() : 0;
            reply["frames"] = Frames(frameMs);
            reply["zdoman_update"] = Sync(syncMs, frameMs);
            reply["bytes_per_player_per_s"] = tally.PerPlayer();
            reply["players_sent_world_data_per_s"] = tally.Served();
            reply["per_second"] = tally.Seconds;
            reply["highseat"] = highSeat.Report();
            return reply;
        }

        /// <summary>The event's copy of a measure: the headline figures, not the seconds.</summary>
        internal static Dictionary<string, object> MeasuredSummary(Dictionary<string, object> reply)
        {
            var summary = new Dictionary<string, object> { ["what"] = "measured" };
            foreach (string key in new[] { "seconds", "role", "players", "fake_players", "frames", "zdoman_update", "bytes_per_player_per_s" })
                summary[key] = reply[key];
            return summary;
        }

        private static Dictionary<string, object> World()
        {
            ZNet net = ZNet.instance;
            return new Dictionary<string, object>
            {
                ["role"] = ClockNetwork.Role(),
                ["players"] = net ? net.GetNrOfPlayers() : 0,
                ["peers"] = net ? net.GetPeers().Count : 0,
            };
        }

        private static Dictionary<string, object> Frames(List<float> ms)
        {
            var stat = new Stat();
            foreach (float frame in ms) stat.Add(frame);
            return new Dictionary<string, object>
            {
                ["count"] = ms.Count,
                ["fps"] = stat.Sum > 0 ? Stat.R(ms.Count * 1000.0 / stat.Sum) : 0,
                ["ms"] = Spread(stat, ms),
            };
        }

        private static Dictionary<string, object> Sync(List<float> sync, List<float> frames)
        {
            var stat = new Stat();
            foreach (float ms in sync) stat.Add(ms);
            double frameTotal = 0;
            foreach (float ms in frames) frameTotal += ms;
            return new Dictionary<string, object>
            {
                ["total_ms"] = Stat.R(stat.Sum),
                ["ms_per_frame"] = Spread(stat, sync),
                ["share_of_frame_time"] = frameTotal > 0 ? System.Math.Round(stat.Sum / frameTotal, 3) : 0,
            };
        }

        // avg, p95 and max of a series.
        private static Dictionary<string, object> Spread(Stat stat, List<float> values)
        {
            Dictionary<string, object> json = stat.Json();
            if (json == null) return null;
            json.Remove("min");
            json["p95"] = Stat.Percentile(values, 0.95);
            return json;
        }
    }
}
