using System;
using System.Linq;
using System.Reflection;
using DevBridge.Server;

namespace DevBridge.Trace
{
    /// <summary>Which methods a trace may patch, and which hook variants a method needs.</summary>
    internal static class MethodRules
    {
        // the tracer and Harmony run on these: patching them could recurse into the patcher or break it mid-patch
        private static readonly string[] Runtime = { "mscorlib", "netstandard", "System", "Mono", "BepInEx.Harmony" };
        private static readonly string[] Patcher = { "0Harmony", "MonoMod", "HarmonyX" };

        internal static void Check(MethodBase method)
        {
            string why = Shape(method) ?? Body(method) ?? Home(method) ?? Parameters(method);
            if (why != null) throw new BridgeException($"{TraceText.Signature(method)} cannot be traced: {why}");
        }

        /// <summary>Harmony hands a struct to an object __instance unboxed (it crashes), so a struct's own methods get none.</summary>
        internal static bool ReadsInstance(MethodBase method) => method.IsStatic || !method.DeclaringType.IsValueType;

        /// <summary>An object __result fails to patch for void and ref returns, and passes a pointer or ref struct as garbage.</summary>
        internal static bool ReadsResult(MethodBase method) =>
            method is MethodInfo info && info.ReturnType != typeof(void) && !info.ReturnType.IsByRef && !Unboxable(info.ReturnType);

        /// <summary>What a call's result says when the hook cannot read it; null for void methods and constructors.</summary>
        internal static string UnreadResult(MethodBase method)
        {
            if (!(method is MethodInfo info) || info.ReturnType == typeof(void)) return null;
            return info.ReturnType.IsByRef ? "(ref return, not read)" : "(pointer or ref struct, not read)";
        }

        private static string Shape(MethodBase method)
        {
            Type type = method.DeclaringType;
            if (type == null) return "it belongs to no type";
            if (type.IsGenericType || type.ContainsGenericParameters) return "its type is generic (code shared between instantiations)";
            if (method.IsGenericMethod) return "it is a generic method";
            if (method.IsConstructor && method.IsStatic) return "a static constructor has already run";
            if ((method.CallingConvention & CallingConventions.VarArgs) != 0) return "it takes __arglist";
            return null;
        }

        private static string Body(MethodBase method)
        {
            MethodImplAttributes impl = method.GetMethodImplementationFlags();
            if (method.IsAbstract) return "it is abstract (trace an implementation of it)";
            if ((impl & MethodImplAttributes.InternalCall) != 0 || (method.Attributes & MethodAttributes.PinvokeImpl) != 0)
                return "it is extern (native code, no IL to patch)";
            if ((impl & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.Runtime) return "the runtime implements it";
            return method.GetMethodBody() == null ? "it has no IL body" : null;
        }

        private static string Home(MethodBase method)
        {
            Assembly assembly = method.DeclaringType.Assembly;
            if (assembly == typeof(MethodRules).Assembly) return "it is DevBridge's own";
            string name = assembly.GetName().Name;
            return Runtime.Any(root => name == root || name.StartsWith(root + ".")) || Patcher.Any(name.StartsWith)
                ? $"it is in {name}: the runtime and Harmony are not traced (the tracer runs on them)"
                : null;
        }

        private static string Parameters(MethodBase method) =>
            method.GetParameters().Any(p => Unboxable(Element(p.ParameterType)))
                ? "a parameter is a pointer or a ref struct, which Harmony cannot put in the argument array"
                : null;

        /// <summary>False when HarmonyX 2.9's argument array would copy a parameter at the wrong width (a by-value [Out], or a
        /// ref or out bool, char, IntPtr or enum not 4 bytes wide): such calls are traced without their arguments.</summary>
        internal static bool ReadsArgs(MethodBase method) =>
            !method.GetParameters().Any(p => p.ParameterType.IsByRef ? !CopiedBack(p.ParameterType.GetElementType()) : p.IsOut);

        private static bool CopiedBack(Type type)
        {
            if (type.IsEnum) return Type.GetTypeCode(type) == TypeCode.Int32 || Type.GetTypeCode(type) == TypeCode.UInt32;
            return type != typeof(bool) && type != typeof(char) && type != typeof(IntPtr) && type != typeof(UIntPtr);
        }

        private static Type Element(Type type) => type.IsByRef ? type.GetElementType() : type;

        /// <summary>Types that cannot be boxed into an object: pointers and ref structs (Span, TypedReference).</summary>
        internal static bool Unboxable(Type type) =>
            type.IsPointer || type == typeof(TypedReference) || type == typeof(ArgIterator) || type == typeof(RuntimeArgumentHandle)
            || type.IsValueType && type.CustomAttributes.Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute");
    }
}
