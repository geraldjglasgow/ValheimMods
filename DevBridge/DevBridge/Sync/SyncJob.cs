using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DevBridge.Server;
using Newtonsoft.Json;

namespace DevBridge.Sync
{
    /// <summary>
    /// Runs one /sync on a thread of its own, never the main thread: finds the DevBridges on this machine, asks each
    /// the same question (this one through its own HTTP, so both sides are read and formatted alike), compares this
    /// instance's answer with each other's, and finishes the request. BridgeRequest.Finish is safe from any thread:
    /// the HTTP thread only waits on its event and reads the reply after it is set.
    /// </summary>
    internal static class SyncJob
    {
        internal static void Start(BridgeRequest request, SyncScope scope, SyncQuery query) =>
            new Thread(() => Run(request, scope, query)) { IsBackground = true, Name = "DevBridge sync" }.Start();

        private static void Run(BridgeRequest request, SyncScope scope, SyncQuery query)
        {
            try
            {
                request.Json(Build(scope, query));
            }
            catch (Exception error)
            {
                request.Finish(Reply.FromException(error));
            }
        }

        private static Dictionary<string, object> Build(SyncScope scope, SyncQuery query)
        {
            List<Instance> found = Census.Take(scope);
            Instance here = found.FirstOrDefault(instance => instance.Here);
            if (here == null || here.Problem != null)
                throw new BridgeException($"this DevBridge (port {scope.OwnPort}) did not answer its own /status: {here?.Problem ?? "not listening"}");
            return query == null ? Census.Report(found, scope) : Ask(scope, query, here, found.Where(instance => !instance.Here && !instance.Foreign).ToList());
        }

        private static Dictionary<string, object> Ask(SyncScope scope, SyncQuery query, Instance here, List<Instance> peers)
        {
            IEnumerable<int> ports = new[] { here.Port }.Concat(peers.Where(peer => peer.Problem == null).Select(peer => peer.Port));
            // Each instance's own Router waits only its default 30 s unless told otherwise; give it the time the caller allowed.
            string path = query.Path + "&timeout=" + Math.Max(1, scope.QueryMs / 1000 - 6);
            Dictionary<int, PeerAnswer> answers = PeerClient.GetAll(ports, path, scope.QueryMs).ToDictionary(answer => answer.Port);
            PeerAnswer mine = answers[here.Port];
            if (!query.Usable(mine)) throw new BridgeException("here: " + mine.Problem);
            List<Dictionary<string, object>> rows = peers.Select(peer => Row(query, here, peer, mine, answers)).ToList();
            var reply = new Dictionary<string, object>
            {
                ["same"] = rows.Count > 0 && rows.All(row => (bool)row["same"]),
                ["compared"] = query.What,
                ["here"] = here.Brief(),
            };
            foreach (KeyValuePair<string, object> pair in query.Head(mine)) reply[pair.Key] = pair.Value;
            reply["peers"] = rows;
            if (rows.Count == 0) reply["note"] = Census.NoPeers(scope);
            return reply;
        }

        /// <summary>One peer's line: who it is, then the comparison, or why there is none.</summary>
        private static Dictionary<string, object> Row(SyncQuery query, Instance here, Instance peer, PeerAnswer mine, Dictionary<int, PeerAnswer> answers)
        {
            Dictionary<string, object> row = peer.Brief();
            PeerAnswer theirs = peer.Problem == null ? answers[peer.Port] : null;
            string problem = peer.Problem ?? (query.Usable(theirs) ? null : theirs.Problem);
            if (problem == null && peer.World != here.World) row["note"] = "in another world";
            try
            {
                if (problem == null) foreach (KeyValuePair<string, object> pair in query.Compare(mine, theirs)) row[pair.Key] = pair.Value;
            }
            catch (JsonException error)
            {
                problem = "unreadable answer: " + error.Message;
            }
            if (problem == null) return row;
            row["same"] = false;
            row["problem"] = problem;
            return row;
        }
    }
}
