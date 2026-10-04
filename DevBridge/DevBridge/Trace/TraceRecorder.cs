using System;
using System.Reflection;
using System.Threading;
using DevBridge.Events;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace DevBridge.Trace
{
    /// <summary>
    /// What the hooks do with a call: Begin finds its tracepoint, writes the call down and starts the clock when the
    /// tracepoint keeps it; End stops the clock, adds the result or exception and keeps the call.
    /// </summary>
    internal static class TraceRecorder
    {
        /// <summary>Set on the main thread when a tracepoint is added; Unity is only read on that thread.</summary>
        internal static int MainThreadId = -1;

        internal static TraceCall Begin(MethodBase method, object instance, object[] args)
        {
            Tracepoint point = Tracer.Active(method);
            if (point == null) return null;
            var call = new TraceCall { Point = point, OnMain = Thread.CurrentThread.ManagedThreadId == MainThreadId };
            if (point.Options.Where != null) Describe(call, instance, args);
            call.N = point.Admit(point.Options.Where != null ? call.Subject : null);
            if (call.N == 0) return null;
            if (call.Args == null) Describe(call, instance, args);
            Stamp(call);
            call.Started = Stopwatch.GetTimestamp();
            return call;
        }

        /// <summary>result is only meaningful when read is true (the hook could read the method's return value).</summary>
        internal static void End(TraceCall call, object result, bool read, bool ran, Exception error)
        {
            if (call.Ended) return;
            call.Ended = true;
            call.Ms = (Stopwatch.GetTimestamp() - call.Started) * 1000.0 / Stopwatch.Frequency;
            call.Skipped = !ran;
            Tracepoint point = call.Point;
            if (error != null) call.Error = Fmt.Clip(error.GetType().Name + ": " + error.Message, TraceText.Width);
            else call.Result = read ? TraceText.Brief(result, call.OnMain) : point.UnreadResult;
            point.Keep(call);
            if (point.Options.Log) Debug.Log($"[DevBridge] trace {point.Id} {call.ToLine()}");
            if (point.Options.Events) Publish(call);
        }

        private static void Describe(TraceCall call, object instance, object[] args)
        {
            Tracepoint point = call.Point;
            if (!point.Method.IsStatic)
                call.Instance = point.ReadsInstance ? TraceText.Brief(instance, call.OnMain) : "(a struct, not read)";
            ParameterInfo[] parameters = point.Parameters;
            call.Args = new string[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                // Harmony reads the array as the call starts, before an out argument has a value
                bool unset = parameters[i].IsOut && parameters[i].ParameterType.IsByRef;
                string value = unset ? "(out)" : args == null ? "(not read)" : i < args.Length ? TraceText.Brief(args[i], call.OnMain) : "?";
                call.Args[i] = Fmt.Clip(parameters[i].Name + "=" + value, TraceText.Width);
            }
        }

        private static void Stamp(TraceCall call)
        {
            if (call.OnMain)
            {
                call.GameTime = Time.time;
                call.Frame = Time.frameCount;
            }
            else
            {
                call.ThreadName = Thread.CurrentThread.Name ?? "#" + Thread.CurrentThread.ManagedThreadId;
            }
            if (call.Point.Options.Stack) call.Stack = Callers.Of(call.Point.Method);
        }

        private static void Publish(TraceCall call)
        {
            var data = call.ToJson();
            data["id"] = call.Point.Id;
            data["method"] = call.Point.Label;
            EventLog.Add("trace", data);
        }
    }
}
