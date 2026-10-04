using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace DevBridge.Perf
{
    /// <summary>
    /// Makes one tiny probe class per timed method at run time: a static field holding that method's counter, a prefix
    /// that starts its clock and a postfix that stops it. Harmony's __originalMethod would let one shared pair find the
    /// counter, but costs a reflection lookup on every call; a field of its own costs nothing. Classes are made once per
    /// method and reused by later samples with a fresh counter.
    /// </summary>
    internal static class ProbeEmitter
    {
        private static readonly MethodInfo Enter = AccessTools.Method(typeof(PerfProbe), nameof(PerfProbe.Enter));
        private static readonly MethodInfo Leave = AccessTools.Method(typeof(PerfProbe), nameof(PerfProbe.Leave));
        private static readonly Dictionary<MethodInfo, Type> Made = new Dictionary<MethodInfo, Type>();
        private static ModuleBuilder module;

        /// <summary>The prefix and postfix that time this target, its counter set; the prefix runs first, the postfix last.</summary>
        internal static (HarmonyMethod prefix, HarmonyMethod postfix) For(PerfTarget target)
        {
            if (!Made.TryGetValue(target.Method, out Type probe)) Made[target.Method] = probe = Build(Made.Count);
            probe.GetField("Counter").SetValue(null, target.Counter);
            return (new HarmonyMethod(probe.GetMethod("Prefix"), Priority.First), new HarmonyMethod(probe.GetMethod("Postfix"), Priority.Last));
        }

        private static Type Build(int serial)
        {
            TypeBuilder type = Module().DefineType("DevBridgePerf.Probe" + serial,
                TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Class);
            FieldBuilder counter = type.DefineField("Counter", typeof(PerfCounter), FieldAttributes.Public | FieldAttributes.Static);
            DefinePrefix(type, counter);
            DefinePostfix(type);
            return type.CreateType();
        }

        /// <summary>static void Prefix(out PerfMark __state) => PerfProbe.Enter(Counter, out __state);</summary>
        private static void DefinePrefix(TypeBuilder type, FieldInfo counter)
        {
            MethodBuilder prefix = type.DefineMethod("Prefix", MethodAttributes.Public | MethodAttributes.Static,
                typeof(void), new[] { typeof(PerfMark).MakeByRefType() });
            prefix.DefineParameter(1, ParameterAttributes.Out, "__state");
            ILGenerator il = prefix.GetILGenerator();
            il.Emit(OpCodes.Ldsfld, counter);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, Enter);
            il.Emit(OpCodes.Ret);
        }

        /// <summary>static void Postfix(PerfMark __state) => PerfProbe.Leave(__state);</summary>
        private static void DefinePostfix(TypeBuilder type)
        {
            MethodBuilder postfix = type.DefineMethod("Postfix", MethodAttributes.Public | MethodAttributes.Static,
                typeof(void), new[] { typeof(PerfMark) });
            postfix.DefineParameter(1, ParameterAttributes.None, "__state");
            ILGenerator il = postfix.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, Leave);
            il.Emit(OpCodes.Ret);
        }

        private static ModuleBuilder Module()
        {
            if (module != null) return module;
            AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("DevBridgePerfProbes"), AssemblyBuilderAccess.Run);
            return module = assembly.DefineDynamicModule("DevBridgePerfProbes");
        }
    }
}
