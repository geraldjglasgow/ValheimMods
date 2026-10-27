using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Broadside
{
    /// <summary>
    /// A small JSON reader, and the quoting the blueprint writer uses (this mod references no JSON library): an object
    /// becomes a dictionary of name to value, an array a list, a number a double, true and false a bool, null null.
    /// Throws <see cref="FormatException"/> saying where it stopped.
    /// </summary>
    public sealed class KwBpJson
    {
        private readonly string text;
        private int at;

        private KwBpJson(string text) => this.text = text;

        public static object? Parse(string text)
        {
            KwBpJson reader = new KwBpJson(text ?? "");
            object? value = reader.Value();
            reader.Space();
            if (reader.at < reader.text.Length)
                throw reader.Fail("more text after the end");
            return value;
        }

        /// <summary>A string as a JSON string literal.</summary>
        public static string Quote(string value)
        {
            StringBuilder s = new StringBuilder("\"");
            foreach (char c in value ?? "")
            {
                if (c == '"' || c == '\\')
                    s.Append('\\').Append(c);
                else if (c == '\n')
                    s.Append("\\n");
                else if (c < ' ')
                    s.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                else
                    s.Append(c);
            }
            return s.Append('"').ToString();
        }

        private object? Value()
        {
            Space();
            if (at >= text.Length)
                throw Fail("the text ends early");
            switch (text[at])
            {
                case '{': return ReadObject();
                case '[': return ReadArray();
                case '"': return ReadString();
                case 't': return Word("true", true);
                case 'f': return Word("false", false);
                case 'n': return Word("null", null);
                default: return ReadNumber();
            }
        }

        private Dictionary<string, object?> ReadObject()
        {
            Dictionary<string, object?> map = new Dictionary<string, object?>();
            at++;
            Space();
            if (Take('}'))
                return map;
            do
            {
                Space();
                if (at >= text.Length || text[at] != '"')
                    throw Fail("a name in quotes expected");
                string key = ReadString();
                Space();
                Expect(':');
                map[key] = Value();
                Space();
            }
            while (Take(','));
            Expect('}');
            return map;
        }

        private List<object?> ReadArray()
        {
            List<object?> list = new List<object?>();
            at++;
            Space();
            if (Take(']'))
                return list;
            do
            {
                list.Add(Value());
                Space();
            }
            while (Take(','));
            Expect(']');
            return list;
        }

        private string ReadString()
        {
            StringBuilder s = new StringBuilder();
            at++;
            while (at < text.Length && text[at] != '"')
            {
                char c = text[at++];
                s.Append(c == '\\' ? Escape() : c);
            }
            Expect('"');
            return s.ToString();
        }

        private char Escape()
        {
            if (at >= text.Length)
                throw Fail("the text ends in an escape");
            char c = text[at++];
            switch (c)
            {
                case 'n': return '\n';
                case 't': return '\t';
                case 'r': return '\r';
                case 'b': return '\b';
                case 'f': return '\f';
                case 'u': return Unicode();
                default: return c;
            }
        }

        private char Unicode()
        {
            if (at + 4 > text.Length || !int.TryParse(text.Substring(at, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code))
                throw Fail("a bad \\u escape");
            at += 4;
            return (char)code;
        }

        private double ReadNumber()
        {
            int start = at;
            while (at < text.Length && "+-0123456789.eE".IndexOf(text[at]) >= 0)
                at++;
            if (!double.TryParse(text.Substring(start, at - start), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                throw Fail("a value expected");
            return value;
        }

        private object? Word(string word, object? value)
        {
            if (string.CompareOrdinal(text, at, word, 0, word.Length) != 0)
                throw Fail("a value expected");
            at += word.Length;
            return value;
        }

        private void Space()
        {
            while (at < text.Length && char.IsWhiteSpace(text[at]))
                at++;
        }

        private bool Take(char c)
        {
            if (at >= text.Length || text[at] != c)
                return false;
            at++;
            return true;
        }

        private void Expect(char c)
        {
            if (!Take(c))
                throw Fail($"'{c}' expected");
        }

        private FormatException Fail(string why) => new FormatException($"{why} at character {at}");
    }
}
