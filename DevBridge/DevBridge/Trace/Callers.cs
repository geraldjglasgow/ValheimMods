using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace DevBridge.Trace
{
    /// <summary>The methods that called a traced one, read from the stack with the patch machinery's frames left out.</summary>
    internal static class Callers
    {
        internal const int Frames = 5;

        internal static string[] Of(MethodBase traced)
        {
            var callers = new List<string>(Frames);
            foreach (StackFrame frame in new StackTrace(1, false).GetFrames() ?? new StackFrame[0])
            {
                MethodBase method = frame.GetMethod();
                if (Hidden(method)) continue;
                // the patched method's own frame, should the runtime report the replacement under the original's name
                if (callers.Count == 0 && method.Name == traced.Name && method.DeclaringType == traced.DeclaringType) continue;
                callers.Add(TraceText.Path(method.DeclaringType) + "." + method.Name);
                if (callers.Count == Frames) break;
            }
            return callers.ToArray();
        }

        /// <summary>DevBridge, Harmony and MonoMod frames, and the generated replacement (DMD) the patch runs in.</summary>
        private static bool Hidden(MethodBase method)
        {
            if (method?.DeclaringType == null) return true;
            Assembly assembly = method.DeclaringType.Assembly;
            if (assembly == typeof(Callers).Assembly) return true;
            string where = method.DeclaringType.FullName + "." + method.Name;
            if (where.Contains("DMD<") || where.Contains("Trampoline")) return true;
            string name = assembly.GetName().Name;
            return name.StartsWith("0Harmony") || name.StartsWith("MonoMod") || name.StartsWith("HarmonySharedState");
        }
    }
}
