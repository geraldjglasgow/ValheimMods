using System.Collections.Generic;
using System.Diagnostics;
using DevBridge.Events;
using DevBridge.Load;
using DevBridge.Server;

namespace DevBridge.Routes
{
    /// <summary>/load: fake players on the server for load tests (HighSeatPlus/PLAN.md, designs 12 and 13), and measuring.</summary>
    internal static class LoadRoute
    {
        private const string Help =
            "/load?players=N&mode=spread|crowd&x=&z=&radius=20&speed=4\n" +
            "                       on the server (dedicated or host): fake players until there are N in all (max 500): peers whose\n" +
            "                       socket counts and drops what it is sent, joined the way the game joins one, each with a Player\n" +
            "                       body walking a loop of radius= metres at speed= m/s; spread: each on its own spot of land\n" +
            "                       (explored zones first), crowd: round x,z, else the host, else the first player connected\n" +
            "                       &deltas=1: the new fakes read HighSeat's smaller updates (as with HighSeatPlus); &verify=1: they\n" +
            "                       keep a copy of the world from what they are sent, both formats, and ask for whole objects\n" +
            "/load?verify=1         for each verifying fake, every object the server has it booked at: its copy at those revisions\n" +
            "                       with the server's record's values (passed, counts, samples of any mismatch)\n" +
            "/load?remove=all       every fake leaves through the game's own disconnect (they also leave on logout and shutdown)\n" +
            "/load?measure=10       sample seconds (1-120) on this game: frame ms avg/p95/max, ZDOMan.Update ms per frame, bytes per\n" +
            "                       fake player per second (all and world data, min/avg/max; apart for fakes with and without\n" +
            "                       deltas), fakes sent world data each second, HighSeat.SyncStats' and DeltaStats' public fields\n" +
            "/load, /load?status=1  the fakes: how many, modes, bytes sent, positions";

        internal static void Register(Router router)
        {
            EventKinds.Describe("load", "published by /load: what = joined (count, mode, total), left (count, why, total),\n" +
                                        "measured (seconds, role, players, fake_players, frames, zdoman_update, bytes_per_player_per_s)");
            router.Add("/load", Help, Handle);
        }

        private static void Handle(BridgeRequest request)
        {
            LoadArgs args = LoadArgs.Read(request);
            switch (args.Action)
            {
                case LoadAction.Add:
                    request.Json(FakeCrowd.Add(args));
                    break;
                case LoadAction.Remove:
                    request.Json(Remove());
                    break;
                case LoadAction.Measure:
                    LoadMeasure.Start(request, args.Seconds);
                    break;
                case LoadAction.Verify:
                    request.Json(ShadowVerify.Report());
                    break;
                default:
                    request.Json(LoadReport.Status());
                    break;
            }
        }

        private static Dictionary<string, object> Remove()
        {
            Stopwatch watch = Stopwatch.StartNew();
            int removed = FakeCrowd.RemoveAll("remove=all");
            LoadPatches.Remove();
            Dictionary<string, object> reply = LoadReport.Status();
            reply["removed"] = removed;
            reply["leave_ms"] = Stat.R(watch.Elapsed.TotalMilliseconds);
            return reply;
        }
    }
}
