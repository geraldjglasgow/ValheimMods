using System;
using System.Collections;
using System.Collections.Concurrent;

namespace DevBridge.Server
{
    /// <summary>Work posted from HTTP threads, run from the plugin's Update.</summary>
    internal static class MainThread
    {
        private static readonly ConcurrentQueue<Action> Queue = new ConcurrentQueue<Action>();

        internal static void Post(Action action) => Queue.Enqueue(action);

        internal static void Drain()
        {
            while (Queue.TryDequeue(out Action action)) action();
        }
    }

    /// <summary>Runs a handler as a coroutine (frames, end of frame, real-time waits) and turns its exceptions into replies.</summary>
    internal static class Async
    {
        internal static void Start(BridgeRequest request, IEnumerator work) =>
            DevBridgePlugin.Instance.StartCoroutine(Guard(request, work));

        private static IEnumerator Guard(BridgeRequest request, IEnumerator work)
        {
            while (Step(request, work)) yield return work.Current;
            if (!request.Answered) request.Fail("the handler ended without an answer", 500);
        }

        private static bool Step(BridgeRequest request, IEnumerator work)
        {
            try
            {
                return work.MoveNext();
            }
            catch (Exception error)
            {
                request.Finish(Reply.FromException(error));
                return false;
            }
        }
    }
}
