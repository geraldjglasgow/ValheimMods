using System;
using System.Collections.Generic;
using System.Threading;
using DevBridge.Server;

namespace DevBridge.Scenario
{
    /// <summary>
    /// Runs a scenario on a thread of its own, one at a time, and answers the HTTP request that started it. Each step's
    /// endpoint call goes through the router, so it runs on the main thread exactly as an HTTP call does; the scenario
    /// thread only waits, which the main thread must never do.
    /// </summary>
    internal static class ScenarioRunner
    {
        private static int busy;
        private static string current;

        /// <summary>Set when the game quits: waits end and no further step starts.</summary>
        internal static volatile bool Quitting;

        internal static void Start(ScenarioPlan plan, TimeSpan budget, Dispatcher dispatch, BridgeRequest request)
        {
            if (Interlocked.CompareExchange(ref busy, 1, 0) != 0)
                throw new BridgeException($"scenario '{current}' is still running; scenarios run one at a time");
            current = plan.Name;
            try
            {
                new Thread(() => Run(plan, budget, dispatch, request)) { IsBackground = true, Name = "DevBridge scenario" }.Start();
            }
            catch
            {
                Interlocked.Exchange(ref busy, 0);
                throw;
            }
        }

        /// <summary>The slot is freed before the answer goes out, so a caller may start the next scenario as soon as it has it.</summary>
        private static void Run(ScenarioPlan plan, TimeSpan budget, Dispatcher dispatch, BridgeRequest request)
        {
            Dictionary<string, object> report = null;
            Reply failure = null;
            try { report = new ScenarioRun(plan, budget, dispatch).Execute(); }
            catch (Exception error) { failure = Reply.FromException(error); }
            finally { Interlocked.Exchange(ref busy, 0); }
            if (report != null) request.Json(report);
            else request.Finish(failure);
        }
    }
}
