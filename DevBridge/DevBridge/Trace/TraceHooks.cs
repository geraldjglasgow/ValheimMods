using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace DevBridge.Trace
{
    /// <summary>
    /// The Harmony patch methods every traced method shares. The prefix runs before every other prefix: Harmony writes the
    /// argument array back into ref and out arguments after it, and an array read first still holds what the caller passed,
    /// so no other mod's change to them is undone. The postfix runs after every other void postfix (Harmony runs pass-through
    /// postfixes, which return the result, after all void ones, so a result one of those replaces is not seen). Recording never throws into the game, and a call made while recording is not recorded.
    /// </summary>
    internal static class TraceHooks
    {
        private const int MaxWarnings = 20;

        [ThreadStatic] private static bool busy;
        private static int warnings;

        internal static HarmonyMethod Prefix(MethodBase method)
        {
            bool args = MethodRules.ReadsArgs(method);
            if (MethodRules.ReadsInstance(method)) return Hook(args ? nameof(Enter) : nameof(EnterBlind), int.MaxValue);
            return Hook(args ? nameof(EnterStruct) : nameof(EnterStructBlind), int.MaxValue);
        }

        internal static HarmonyMethod Postfix(MethodBase method) =>
            Hook(MethodRules.ReadsResult(method) ? nameof(Leave) : nameof(LeaveUnread), int.MinValue);

        internal static HarmonyMethod Finalizer() => Hook(nameof(Fail), int.MinValue);

        private static HarmonyMethod Hook(string name, int priority) =>
            new HarmonyMethod(AccessTools.Method(typeof(TraceHooks), name)) { priority = priority };

        private static void Enter(object __instance, object[] __args, MethodBase __originalMethod, out TraceCall __state) =>
            __state = Begin(__originalMethod, __instance, __args);

        private static void EnterStruct(object[] __args, MethodBase __originalMethod, out TraceCall __state) =>
            __state = Begin(__originalMethod, null, __args);

        // Without __args: Harmony then builds no argument array, which it would copy at the wrong width for some parameters.
        private static void EnterBlind(object __instance, MethodBase __originalMethod, out TraceCall __state) =>
            __state = Begin(__originalMethod, __instance, null);

        private static void EnterStructBlind(MethodBase __originalMethod, out TraceCall __state) =>
            __state = Begin(__originalMethod, null, null);

        private static void Leave(object __result, bool __runOriginal, TraceCall __state)
        {
            if (__state != null) End(__state, __result, true, __runOriginal, null);
        }

        private static void LeaveUnread(bool __runOriginal, TraceCall __state)
        {
            if (__state != null) End(__state, null, false, __runOriginal, null);
        }

        /// <summary>Runs after every call and after every other finalizer; records a thrown call, or one a later postfix
        /// threw out of after the trace's postfix had already ended it.</summary>
        private static void Fail(Exception __exception, bool __runOriginal, TraceCall __state)
        {
            if (__state != null && (__exception != null || !__state.Ended)) End(__state, null, false, __runOriginal, __exception);
        }

        private static TraceCall Begin(MethodBase method, object instance, object[] args)
        {
            if (busy) return null;
            busy = true;
            try
            {
                return TraceRecorder.Begin(method, instance, args);
            }
            catch (Exception error)
            {
                Warn(method, error);
                return null;
            }
            finally
            {
                busy = false;
            }
        }

        private static void End(TraceCall call, object result, bool read, bool ran, Exception thrown)
        {
            bool outer = !busy;
            busy = true;
            try
            {
                TraceRecorder.End(call, result, read, ran, thrown);
            }
            catch (Exception error)
            {
                Warn(call.Point.Method, error);
            }
            finally
            {
                if (outer) busy = false;
            }
        }

        private static void Warn(MethodBase method, Exception error)
        {
            if (Interlocked.Increment(ref warnings) > MaxWarnings) return;
            try
            {
                Debug.LogWarning($"[DevBridge] trace {method?.Name}: recording failed, {error.GetType().Name}: {error.Message}");
            }
            catch (Exception)
            {
                // the warning is called from inside the traced method: not even logging may throw into the game
            }
        }
    }
}
