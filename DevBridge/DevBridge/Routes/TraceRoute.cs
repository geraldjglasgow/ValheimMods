using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevBridge.Server;
using DevBridge.Trace;
using UnityEngine;

namespace DevBridge.Routes
{
    /// <summary>/trace: live tracepoints, a Harmony patch put on any game or mod method while the game runs, recording each call.</summary>
    internal static class TraceRoute
    {
        internal static void Register(Router router) => router.Add("/trace",
            "/trace?method=Type.Method&where=text&max=200&sample=1&stack=1&log=1&events=1&overload=N\n" +
            "                       patch a game or mod method now and record each call: game time, frame, ms, the instance, each\n" +
            "                       argument, the result or exception (stack=1 adds the callers); Type.Method(int,string) or\n" +
            "                       overload=N picks an overload, Type..ctor a constructor, Type.get_X a property getter; where=\n" +
            "                       keeps calls whose instance or arguments contain the text, sample=N every Nth, off after max=;\n" +
            "                       /trace lists them, ?id=N&last=20 its calls, ?off=N|all unpatches, ?clear=1 forgets those off",
            Handle);

        private static void Handle(BridgeRequest request)
        {
            if (request.Get("method") != null) request.Json(Add(request));
            else if (request.Get("off") != null) request.Json(Off(request.Get("off")));
            else if (request.Get("id") != null) request.Json(Calls(request));
            else request.Json(List(request.Flag("clear")));
        }

        private static Dictionary<string, object> Add(BridgeRequest request)
        {
            MethodBase method = MethodFinder.Find(request.Get("method"), request.Int("overload", -1));
            Tracepoint point = Tracer.Add(method, Options(request));
            Dictionary<string, object> reply = point.Summary();
            reply["harmony_id"] = Tracer.HarmonyId;
            List<string> others = Tracer.OtherPatches(method);
            if (others.Count > 0) reply["also_patched_by"] = others;
            return reply;
        }

        private static TraceOptions Options(BridgeRequest request) => new TraceOptions
        {
            Where = request.Get("where"),
            Max = Mathf.Clamp(request.Int("max", 200), 1, 100000),
            Sample = Mathf.Clamp(request.Int("sample", 1), 1, 1000000),
            Stack = request.Flag("stack"),
            Log = request.Flag("log"),
            Events = request.Flag("events"),
        };

        private static Dictionary<string, object> Off(string which) => new Dictionary<string, object>
        {
            ["switched_off"] = Tracer.Off(which),
            ["tracepoints"] = Tracer.List(),
        };

        private static Dictionary<string, object> Calls(BridgeRequest request)
        {
            Tracepoint point = Tracer.Find(request.Int("id", -1));
            int last = Mathf.Clamp(request.Int("last", 20), 1, Tracepoint.Capacity);
            Dictionary<string, object> reply = point.Summary();
            reply["calls"] = point.Recent(last).Select(call => call.ToJson()).ToList();
            return reply;
        }

        private static Dictionary<string, object> List(bool clear)
        {
            var reply = new Dictionary<string, object>();
            if (clear) reply["forgotten"] = Tracer.Forget();
            reply["tracepoints"] = Tracer.List();
            return reply;
        }
    }
}
