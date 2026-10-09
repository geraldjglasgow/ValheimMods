using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenKeep.Blueprints.Bench
{
    /// <summary>
    /// A player's machine: the shared pool as the server last listed it. Asked for when the bench window opens and again
    /// (at most once a second) while it is open and the server announced a change.
    /// </summary>
    public static class BenchPool
    {
        private const float AskInterval = 1f;
        private const float AnswerWait = 5f;

        private static bool stale;
        private static float nextAsk;
        private static float openedAt;

        public static List<BenchOwner> Owners { get; private set; } = new List<BenchOwner>();

        /// <summary>A listing arrived since the window opened.</summary>
        public static bool Loaded { get; private set; }

        /// <summary>No listing came within a few seconds of opening: the server does not answer bench requests.</summary>
        public static bool Silent => !Loaded && Time.unscaledTime - openedAt > AnswerWait;

        /// <summary>Goes up with every listing.</summary>
        public static int Version { get; private set; }

        public static BenchOwner Find(long id) => Owners.FirstOrDefault(o => o.Id == id);

        /// <summary>The window opened: forget the old listing and ask for the pool.</summary>
        public static void Open()
        {
            Loaded = false;
            openedAt = Time.unscaledTime;
            Ask();
        }

        private static void Ask()
        {
            stale = false;
            nextAsk = Time.unscaledTime + AskInterval;
            ZRoutedRpc.instance?.InvokeRoutedRPC(BenchRpc.List);
        }

        public static void MarkStale() => stale = true;

        public static void Tick()
        {
            if (stale && BenchWindow.IsOpen && Time.unscaledTime >= nextAsk)
                Ask();
        }

        public static void OnListing(long sender, ZPackage package)
        {
            if (!BenchRpc.FromServer(sender))
                return;
            Owners = BenchListing.Read(package);
            Loaded = true;
            Version++;
        }
    }
}
