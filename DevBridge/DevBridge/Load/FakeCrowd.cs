using System;
using System.Collections.Generic;
using System.Globalization;
using DevBridge.Events;
using DevBridge.Server;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace DevBridge.Load
{
    /// <summary>
    /// The fake players this game runs (/load), on the server: the list, a batch added (placed by <see cref="Spots"/>,
    /// joined by <see cref="FakeJoin"/>, walked by <see cref="FakeTicker"/>), all of them removed (remove=all, a logout,
    /// the game or server closing: <see cref="LoadPatches"/>), and the patches that exist only while there are fakes.
    /// Names run Fake 1, Fake 2... through the session; each fake has a fresh peer uid and a host name of its own: a
    /// number far below every real Steam account's (16 digits from <see cref="HostBase"/>), or DevBridge_ and that number
    /// on a crossplay (PlayFab) server, so its entry in the player list parses there too. A fake's entry in the game's
    /// player list is known by its body's user id (the fake's uid; it keeps it after a lost body), or, with no body, by
    /// its name. Crowd loops lie inside the radius round the centre (their centres within half of it, loops of three
    /// tenths to half of it); spread loops have six tenths to all of the radius. Each fake moves and reports at its own
    /// moment, so they do not all update in one frame. Fakes are only added here: players= never removes any, and
    /// remove=all takes them all.
    /// </summary>
    internal static class FakeCrowd
    {
        internal const int Max = 500;
        private const long HostBase = 1000000000000000L;
        private static readonly List<FakePlayer> fakes = new List<FakePlayer>();
        private static readonly HashSet<long> uids = new HashSet<long>();
        private static readonly HashSet<string> names = new HashSet<string>();
        private static readonly System.Random random = new System.Random();
        private static int nextIndex = 1;

        internal static List<FakePlayer> All => fakes;

        /// <summary>Throws unless this game runs a world as its server (dedicated, hosting, or single player).</summary>
        internal static void RequireServer()
        {
            if (!ZNet.instance || ZDOMan.instance == null || ZRoutedRpc.instance == null || !ZNetScene.instance || !ZoneSystem.instance)
                throw new BridgeException("no world is loaded: fake players join a running world, on a dedicated server or a game you host");
            if (!ZNet.instance.IsServer())
                throw new BridgeException("this game is a client: fake players are added on the server. Call the server's own DevBridge " +
                                          "(a dedicated server on this machine answers on another port: /sync?peers=1 lists them), or host the world");
        }

        /// <summary>Adds fakes until there are args.Players in all.</summary>
        internal static Dictionary<string, object> Add(LoadArgs args)
        {
            RequireServer();
            int wanted = args.Players - fakes.Count;
            if (wanted <= 0) return LoadReport.Added(args, 0, 0, 0, 0, $"already {fakes.Count} fake player(s): nothing added", 0);
            Stopwatch watch = Stopwatch.StartNew();
            List<FakePath> paths = Paths(args, wanted, out int unexplored);
            LoadPatches.Apply();
            int joined = 0;
            string note = null;
            foreach (FakePath path in paths)
            {
                if (JoinOne(args, path, ref note)) joined++;
            }
            if (joined > 0) FakeJoin.Announce();
            FakeTicker.Ensure();
            Announced(joined, args.Mode);
            return LoadReport.Added(args, wanted, paths.Count, joined, unexplored, note, watch.Elapsed.TotalMilliseconds);
        }

        private static List<FakePath> Paths(LoadArgs args, int wanted, out int unexplored)
        {
            unexplored = 0;
            var paths = new List<FakePath>(wanted);
            if (args.Mode == LoadArgs.Crowd)
            {
                Vector3 centre = Spots.CrowdCentre(args);
                for (int i = 0; i < wanted; i++) paths.Add(CrowdPath(centre, args.Radius, args.Speed));
                return paths;
            }
            foreach (Vector3 spot in Spots.Spread(wanted, args.Radius, SpreadCentres(), random, out unexplored))
                paths.Add(NewPath(spot, args.Radius * Range(0.6f, 1f), args.Speed));
            return paths;
        }

        private static FakePath CrowdPath(Vector3 centre, float radius, float speed)
        {
            float half = radius * 0.5f;
            float angle = Range(0f, Mathf.PI * 2f), distance = half * Mathf.Sqrt(Range(0f, 1f));
            var offset = new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
            return NewPath(centre + offset, half * Range(0.6f, 1f), speed);
        }

        private static FakePath NewPath(Vector3 centre, float loop, float speed) =>
            new FakePath(centre, loop, speed, Range(0f, Mathf.PI * 2f), random.Next(2) == 0, Time.time);

        private static List<Vector3> SpreadCentres()
        {
            var centres = new List<Vector3>();
            foreach (FakePlayer fake in fakes)
            {
                if (fake.Mode == LoadArgs.Spread) centres.Add(fake.Path.Centre);
            }
            return centres;
        }

        private static bool JoinOne(LoadArgs args, FakePath path, ref string note)
        {
            FakePlayer fake = Make(args.Mode, path);
            FakeDeltas.Prepare(fake, args.Deltas, args.Verify);
            try
            {
                float now = Time.time;
                FakeJoin.Join(fake, path.Position(now));
                Register(fake);
                if (fake.Deltas) FakeDeltas.Hello(fake);
                if (!FakeCharacter.Create(fake, NewId(), now, random)) note = note ?? $"{fake.Name} has no body: its ZDO id was taken";
                fake.NextMove = now + Range(0f, FakeTicker.MoveInterval);
                fake.NextRefPos = now + Range(0f, FakeTicker.RefPosInterval);
                return true;
            }
            catch (Exception failure)
            {
                note = note ?? $"a fake player could not join: {failure.GetType().Name}: {failure.Message}";
                LeaveSafely(fake);
                Unregister(fake);
                return false;
            }
        }

        private static FakePlayer Make(string mode, FakePath path)
        {
            int index = nextIndex++;
            string number = (HostBase + index).ToString(CultureInfo.InvariantCulture);
            string host = ZNet.m_onlineBackend == OnlineBackendType.PlayFab ? "DevBridge_" + number : number;
            return new FakePlayer(index, NewUid(), host, mode, path);
        }

        private static void Register(FakePlayer fake)
        {
            fakes.Add(fake);
            uids.Add(fake.Uid);
            names.Add(fake.Name);
        }

        private static void Unregister(FakePlayer fake)
        {
            fakes.Remove(fake);
            uids.Remove(fake.Uid);
            names.Remove(fake.Name);
        }

        /// <summary>Every fake leaves through the game's own disconnect, one at a time; how many left.</summary>
        internal static int RemoveAll(string why)
        {
            if (fakes.Count == 0) return 0;
            var leaving = new List<FakePlayer>(fakes);
            foreach (FakePlayer fake in leaving)
            {
                LeaveSafely(fake);
                Unregister(fake);
            }
            Debug.Log($"[DevBridge] load: {leaving.Count} fake player(s) left ({why})");
            EventLog.Add("load", new Dictionary<string, object> { ["what"] = "left", ["count"] = leaving.Count, ["why"] = why, ["total"] = 0 });
            return leaving.Count;
        }

        /// <summary>A fake whose connection the game closed by itself: its disconnect finished, and it is let go.</summary>
        internal static void Drop(FakePlayer fake)
        {
            LeaveSafely(fake);
            Unregister(fake);
            Debug.LogWarning($"[DevBridge] load: {fake.Name} left: the game closed its connection");
        }

        /// <summary>The world the fakes joined is gone with them: only the records go.</summary>
        internal static void Forget(string why)
        {
            if (fakes.Count == 0) return;
            Debug.Log($"[DevBridge] load: {fakes.Count} fake player(s) forgotten ({why})");
            fakes.Clear();
            uids.Clear();
            names.Clear();
        }

        internal static bool IsFake(ZNetPeer peer) => peer != null && peer.m_socket is FakeSocket;

        /// <summary>A fake's entry in the game's player list: by its body's user id, or with no body by its name.</summary>
        internal static bool IsFake(ZNet.PlayerInfo info) =>
            uids.Contains(info.m_characterID.UserID) || info.m_characterID.IsNone() && names.Contains(info.m_name);

        private static void LeaveSafely(FakePlayer fake)
        {
            try
            {
                FakeJoin.Leave(fake);
            }
            catch (Exception error)
            {
                Publish.WarnOnce("load (a fake player leaving)", error);
            }
        }

        private static void Announced(int joined, string mode)
        {
            if (joined == 0) return;
            Debug.Log($"[DevBridge] load: {joined} fake player(s) joined ({mode}), {fakes.Count} in all");
            EventLog.Add("load", new Dictionary<string, object> { ["what"] = "joined", ["count"] = joined, ["mode"] = mode, ["total"] = fakes.Count });
        }

        private static long NewUid()
        {
            long uid;
            do uid = NewId();
            while (uids.Contains(uid) || ZNet.instance.IsConnected(uid));
            return uid;
        }

        private static long NewId() => ((long)random.Next(1, int.MaxValue) << 32) | (uint)random.Next();

        private static float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);
    }
}
