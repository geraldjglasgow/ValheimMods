using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace DevBridge.Perf
{
    /// <summary>
    /// The calls and time of one timed method in one sample. Only the main thread writes the plain fields; calls on other
    /// threads are added atomically to their own pair. Public only because the run-time probe classes hold one.
    /// </summary>
    public sealed class PerfCounter
    {
        internal readonly PerfMod Mod;
        internal long Ticks;
        internal long Calls;
        internal long Max;
        internal long OtherTicks;
        internal long OtherCalls;

        internal PerfCounter(PerfMod mod)
        {
            Mod = mod;
        }

        internal void OnMain(long ticks, bool outer)
        {
            Ticks += ticks;
            Calls++;
            if (ticks > Max) Max = ticks;
            if (outer) Mod.Ticks += ticks;
        }

        internal void OnOther(long ticks)
        {
            Interlocked.Add(ref OtherTicks, ticks);
            Interlocked.Increment(ref OtherCalls);
        }

        internal void Clear()
        {
            Ticks = Calls = Max = 0;
            Interlocked.Exchange(ref OtherTicks, 0);
            Interlocked.Exchange(ref OtherCalls, 0);
        }
    }

    /// <summary>
    /// A mod as /perf reports it: a plugin's name (or an assembly's), and its main-thread time counted once per outermost
    /// call, so one of its timed methods running inside another of its own is not added twice.
    /// </summary>
    internal sealed class PerfMod
    {
        internal readonly int Index;
        internal readonly string Name;
        internal readonly string[] Aliases;
        internal long Ticks;

        internal PerfMod(int index, string name, params string[] aliases)
        {
            Index = index;
            Name = name;
            Aliases = aliases;
        }
    }

    /// <summary>One method /perf times: a mod's prefix, postfix or finalizer, or a MonoBehaviour's per-frame method.</summary>
    internal sealed class PerfTarget
    {
        internal readonly MethodInfo Method;
        internal readonly PerfMod Mod;
        internal readonly PerfCounter Counter;
        internal readonly SortedSet<string> Kinds = new SortedSet<string>();
        internal readonly List<MethodBase> On = new List<MethodBase>();

        internal PerfTarget(MethodInfo method, PerfMod mod)
        {
            Method = method;
            Mod = mod;
            Counter = new PerfCounter(mod);
        }

        /// <summary>
        /// Small enough that Mono may have inlined it into the wrappers Harmony built for the methods it patches, where a
        /// probe on it is never reached: those wrappers are rebuilt once the probe is on. Mono's own limit is 20 bytes of IL.
        /// </summary>
        internal bool MayBeInlined
        {
            get
            {
                if ((Method.MethodImplementationFlags & MethodImplAttributes.AggressiveInlining) != 0) return true;
                byte[] il = Method.GetMethodBody()?.GetILAsByteArray();
                return il != null && il.Length <= 64;
            }
        }
    }
}
