using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThreadTimer = System.Threading.Timer;

namespace DevBridge.Reload
{
    /// <summary>
    /// Timers whose callback is the old copy's code, found in Mono's own list of scheduled timers, so a timer nobody
    /// keeps a reference to (ConfigReloader.Setup's return value thrown away) still stops. The list is a private part
    /// of the game's mscorlib (Timer.Scheduler.instance.list); when it is not there the static sweep is all there is.
    /// </summary>
    internal static class TimerSweep
    {
        private const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;

        internal static void Run(Assembly old, Report report)
        {
            Type scheduler = typeof(ThreadTimer).GetNestedType("Scheduler", BindingFlags.NonPublic);
            object instance = scheduler?.GetField("instance", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
            FieldInfo callback = typeof(ThreadTimer).GetField("callback", Hidden);
            FieldInfo disposed = typeof(ThreadTimer).GetField("disposed", Hidden);
            if (!(scheduler?.GetField("list", Hidden)?.GetValue(instance) is List<ThreadTimer> list) || callback == null)
            {
                report.Leave("Mono's timer list was not readable: only timers the old copy's static fields reach were stopped");
                return;
            }
            List<ThreadTimer> timers;
            lock (instance) timers = list.ToList();
            // A disposed timer stays in the list, marked dead, until the scheduler's next pass.
            foreach (ThreadTimer timer in timers.Where(t => disposed?.GetValue(t) as bool? != true && OldCode.Owns(callback.GetValue(t) as Delegate, old)))
            {
                timer.Dispose();
                report.Count("timers");
            }
        }
    }
}
