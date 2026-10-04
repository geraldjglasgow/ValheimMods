using System;
using System.Collections.Generic;
using System.Reflection;
using DevBridge.Server;

namespace DevBridge.Trace
{
    /// <summary>How a tracepoint chooses and reports calls.</summary>
    internal sealed class TraceOptions
    {
        internal string Where;
        internal int Max = 200;
        internal int Sample = 1;
        internal bool Stack;
        internal bool Log;
        internal bool Events;
    }

    /// <summary>
    /// One tracepoint: its method, its options, its counts and the calls it kept. Calls may arrive on any thread, so
    /// counting and keeping happen under a lock; after Max kept calls it switches itself off and asks the main thread to
    /// take its patch off.
    /// </summary>
    internal sealed class Tracepoint
    {
        internal const string On = "on";
        internal const int Capacity = 500;

        internal readonly int Id;
        internal readonly MethodBase Method;
        internal readonly IntPtr Key;
        internal readonly string Label;
        internal readonly ParameterInfo[] Parameters;
        internal readonly bool ReadsInstance;
        internal readonly string UnreadResult;
        internal readonly TraceOptions Options;

        private readonly object gate = new object();
        private readonly TraceCall[] ring = new TraceCall[Capacity];
        private volatile string state = On;
        private long calls, matched;
        private int kept;

        internal Tracepoint(int id, MethodBase method, TraceOptions options)
        {
            Id = id;
            Method = method;
            Key = method.MethodHandle.Value;
            Label = TraceText.Signature(method);
            Parameters = method.GetParameters();
            ReadsInstance = MethodRules.ReadsInstance(method);
            UnreadResult = MethodRules.UnreadResult(method);
            Options = options;
        }

        internal bool IsOn => state == On;

        internal string State => state;

        /// <summary>Counts a call and gives it a number when it is kept, or 0. subject is only needed for where=.</summary>
        internal int Admit(string subject)
        {
            lock (gate)
            {
                if (state != On) return 0;
                calls++;
                if (Options.Where != null && subject.IndexOf(Options.Where, StringComparison.OrdinalIgnoreCase) < 0) return 0;
                if (matched++ % Options.Sample != 0) return 0;
                if (++kept < Options.Max) return kept;
                state = "max reached";
                MainThread.Post(() => Tracer.Retire(this));
                return kept;
            }
        }

        /// <summary>Stops keeping calls at once; Tracer.Retire takes the patch off.</summary>
        internal void SwitchOff()
        {
            lock (gate) if (state == On) state = "off";
        }

        internal void Keep(TraceCall call)
        {
            lock (gate) ring[(call.N - 1) % Capacity] = call;
        }

        /// <summary>The newest finished calls, oldest first; calls still running are not in it yet.</summary>
        internal List<TraceCall> Recent(int last)
        {
            var recent = new List<TraceCall>();
            lock (gate)
            {
                for (int n = Math.Max(1, kept - Math.Min(last, Capacity) + 1); n <= kept; n++)
                {
                    TraceCall call = ring[(n - 1) % Capacity];
                    if (call != null && call.N == n) recent.Add(call);
                }
            }
            return recent;
        }

        internal Dictionary<string, object> Summary()
        {
            var summary = new Dictionary<string, object> { ["id"] = Id, ["method"] = Label, ["state"] = state };
            lock (gate)
            {
                summary["calls"] = calls;
                summary["matched"] = matched;
                summary["kept"] = kept;
            }
            summary["max"] = Options.Max;
            if (Options.Where != null) summary["where"] = Options.Where;
            if (Options.Sample > 1) summary["sample"] = Options.Sample;
            if (Options.Stack) summary["stack"] = true;
            if (Options.Log) summary["log"] = true;
            if (Options.Events) summary["events"] = true;
            return summary;
        }
    }
}
