using System;
using System.Globalization;
using System.Linq;
using DevBridge.Eval;
using UnityEngine;

namespace DevBridge.Tune
{
    /// <summary>Values as the replies show them (floats rounded) and as C# literals to paste into a mod (code=1).</summary>
    internal static class CodeText
    {
        internal static object Show(object value)
        {
            switch (value)
            {
                case null: return null;
                case float number: return Fmt.R(number);
                case double number: return Math.Round(number, 2);
                case Vector3 vector: return Fmt.V3(vector);
                case Enum named: return named.ToString();
                case string _: return value;
                default: return value.GetType().IsPrimitive ? value : Describe.Value(value);
            }
        }

        internal static string Literal(object value)
        {
            switch (value)
            {
                case null: return "null";
                case float number: return Float(number);
                case double number: return number.ToString("R", CultureInfo.InvariantCulture);
                case bool flag: return flag ? "true" : "false";
                case string text: return "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
                case Enum named: return EnumLiteral(named);
                case Vector3 v: return $"new Vector3({Float(v.x)}, {Float(v.y)}, {Float(v.z)})";
                case Vector2 v: return $"new Vector2({Float(v.x)}, {Float(v.y)})";
                case Color c: return $"new Color({Float(c.r)}, {Float(c.g)}, {Float(c.b)}, {Float(c.a)})";
                case IFormattable number when value.GetType().IsPrimitive: return number.ToString(null, CultureInfo.InvariantCulture);
                default: return "/* " + Describe.Value(value) + " */";
            }
        }

        private static string Float(float number) => number.ToString("G6", CultureInfo.InvariantCulture) + "f";

        /// <summary>Attack.AttackType.Horizontal; flags joined with |; a value with no name cast from its number.</summary>
        private static string EnumLiteral(Enum value)
        {
            string type = value.GetType().FullName.Replace('+', '.');
            string text = value.ToString();
            if (text.Length > 0 && (char.IsDigit(text[0]) || text[0] == '-')) return $"({type}){text}";
            return string.Join(" | ", text.Split(new[] { ", " }, StringSplitOptions.None).Select(name => type + "." + name));
        }
    }
}
