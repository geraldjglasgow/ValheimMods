using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace HighSeat
{
    /// <summary>
    /// A game method's fingerprint for the player list patches: <see cref="GameCode"/>'s text of the method, followed by
    /// the text of every method the compiler made for it (its lambdas and closures, in ZNet+&lt;&gt;c and the
    /// &lt;&gt;c__DisplayClass types, named after it), so a change inside a lambda counts as well. The numbers the
    /// compiler puts in those names (b__130_0, DisplayClass140_0: where the method sits in its class) are left out: a
    /// build with a method more or less before it (the dedicated server's game is built apart) numbers them differently
    /// without any change to what the code does. Reflection only, run once per method per process.
    /// </summary>
    internal static class ListCode
    {
        private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
            BindingFlags.Static | BindingFlags.DeclaredOnly;
        private static readonly Regex Ordinal = new Regex(@"(?<=b__|<>9__|DisplayClass)\d+(?=_)");

        public static uint Fingerprint(MethodBase method) => GameCode.Hash(Text(method));

        /// <summary>The text the fingerprint is taken of.</summary>
        public static string Text(MethodBase method)
        {
            StringBuilder text = new StringBuilder(Normal(GameCode.Describe(method)));
            foreach (MethodInfo part in Parts(method))
            {
                text.Append('|').Append(Normal(part.Name)).Append(':').Append(Normal(GameCode.Describe(part)));
            }
            return text.ToString();
        }

        private static string Normal(string text) => Ordinal.Replace(text, "#");

        // The compiler's methods for this one ("<Name>b__0", "<Name>g__Local|1_0"), in a fixed order.
        private static List<MethodInfo> Parts(MethodBase method)
        {
            List<MethodInfo> parts = new List<MethodInfo>();
            string prefix = "<" + method.Name + ">";
            foreach (Type nested in method.DeclaringType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                foreach (MethodInfo m in nested.GetMethods(Declared))
                {
                    if (m.Name.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        parts.Add(m);
                    }
                }
            }
            parts.Sort((a, b) => string.CompareOrdinal(Normal(a.Name), Normal(b.Name)));
            return parts;
        }
    }
}
