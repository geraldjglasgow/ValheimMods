using System;
using System.Collections.Generic;
using System.Globalization;
using DevBridge.Eval;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Tune
{
    /// <summary>
    /// value= read for a member's type: a number, true/false, an enum name, text, x,y,z or r,g,b,a; or *x, /x and +x,
    /// worked out from each copy's own current value.
    /// </summary>
    internal static class TuneValue
    {
        private static readonly HashSet<Type> Numbers = new HashSet<Type>
        {
            typeof(float), typeof(double), typeof(int), typeof(long), typeof(short), typeof(byte),
            typeof(sbyte), typeof(uint), typeof(ulong), typeof(ushort), typeof(decimal),
        };

        /// <summary>The value to write over current.</summary>
        internal static object Next(string text, object current, Type type)
        {
            char op = Operator(text);
            if (op == '=') return Parse(text.Trim(), type);
            if (!Numbers.Contains(type)) throw new BridgeException($"*, / and + work on numbers; {Describe.TypeName(type)} is not one");
            double operand = Number(text.Substring(1).Trim());
            if (op == '/' && operand == 0) throw new BridgeException("cannot divide by 0");
            double now = System.Convert.ToDouble(current, CultureInfo.InvariantCulture);
            return Coerce.To(op == '*' ? now * operand : op == '/' ? now / operand : now + operand, type);
        }

        /// <summary>*, / or +, else '='. A + left unencoded in a query string arrives as a space, so a leading space is + too.</summary>
        private static char Operator(string text)
        {
            char first = text.Length > 0 ? text[0] : '=';
            if (first == ' ') return '+';
            return first == '*' || first == '/' || first == '+' ? first : '=';
        }

        internal static object Parse(string text, Type type)
        {
            if (text == "null" && !type.IsValueType) return null;
            if (type == typeof(string)) return text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"' ? text.Substring(1, text.Length - 2) : text;
            if (type == typeof(bool)) return Bool(text);
            if (Numbers.Contains(type)) return Coerce.To(Number(text), type);
            if (type == typeof(Color)) return Colour(text);
            return Coerce.To(text, type);
        }

        private static double Number(string text) =>
            double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : throw new BridgeException($"'{text}' is not a number");

        private static bool Bool(string text)
        {
            switch (text.ToLowerInvariant())
            {
                case "1": case "true": case "yes": case "on": return true;
                case "0": case "false": case "no": case "off": return false;
                default: throw new BridgeException($"'{text}' is not true or false");
            }
        }

        private static Color Colour(string text)
        {
            float[] n = Fmt.Numbers(text, text.Split(',').Length == 4 ? 4 : 3, "a colour (r,g,b or r,g,b,a)");
            return new Color(n[0], n[1], n[2], n.Length == 4 ? n[3] : 1f);
        }
    }
}
