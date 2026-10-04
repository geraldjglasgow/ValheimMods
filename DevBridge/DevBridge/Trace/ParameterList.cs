using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevBridge.Server;

namespace DevBridge.Trace
{
    /// <summary>A parameter list as method= gives it, (int, ref float, List&lt;string&gt;), matched against a method's parameters.</summary>
    internal static class ParameterList
    {
        private static readonly string[] RefWords = { "ref ", "out ", "in " };

        /// <summary>The parameter types of "(a, b)": split at the commas outside of &lt;&gt; and [].</summary>
        internal static string[] Split(string list, string text)
        {
            list = list.Trim();
            if (!list.EndsWith(")")) throw new BridgeException($"method= has an unclosed parameter list: {text}");
            string inner = list.Substring(1, list.Length - 2);
            if (inner.Trim().Length == 0) return new string[0];
            var parts = new List<string>();
            int depth = 0, start = 0;
            for (int i = 0; i < inner.Length; i++)
            {
                char c = inner[i];
                if (c == '<' || c == '[') depth++;
                else if (c == '>' || c == ']') depth--;
                else if (c == ',' && depth == 0) { parts.Add(inner.Substring(start, i - start).Trim()); start = i + 1; }
            }
            parts.Add(inner.Substring(start).Trim());
            return parts.ToArray();
        }

        internal static bool Matches(MethodBase method, string[] wanted)
        {
            ParameterInfo[] parameters = method.GetParameters();
            return parameters.Length == wanted.Length && parameters.Select((p, i) => Matches(p.ParameterType, wanted[i])).All(match => match);
        }

        /// <summary>By C# name (int, List&lt;string&gt;), type name (Int32) or full name; ref, out, in or a trailing &amp;
        /// asks for a by-reference parameter, and without one a by-reference parameter still matches its type.</summary>
        private static bool Matches(Type type, string given)
        {
            given = given.Trim();
            bool byRef = given.EndsWith("&");
            given = given.TrimEnd('&');
            foreach (string word in RefWords.Where(word => given.StartsWith(word)))
            {
                given = given.Substring(word.Length);
                byRef = true;
            }
            if (byRef && !type.IsByRef) return false;
            if (type.IsByRef) type = type.GetElementType();
            return Same(given, TraceText.CsName(type)) || Same(given, type.Name) || Same(given, TraceText.Path(type))
                   || Same(given, type.FullName?.Replace('+', '.'));
        }

        private static bool Same(string given, string name) =>
            name != null && string.Equals(given.Replace(" ", ""), name.Replace(" ", ""), StringComparison.OrdinalIgnoreCase);
    }
}
