using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DevBridge.Server;

namespace DevBridge.Eval
{
    internal enum TokenKind { Ident, Number, String, Symbol, End }

    internal struct Token
    {
        internal TokenKind Kind;
        internal string Text;
        internal int Position;
    }

    /// <summary>Splits an expression into identifiers ($name included), numbers, quoted strings and . ( ) [ ] , =</summary>
    internal static class Lexer
    {
        internal static List<Token> Read(string source)
        {
            var tokens = new List<Token>();
            for (int i = 0; i < source.Length;)
            {
                if (char.IsWhiteSpace(source[i])) { i++; continue; }
                int start = i;
                TokenKind kind = Scan(source, ref i);
                tokens.Add(new Token { Kind = kind, Text = source.Substring(start, i - start), Position = start });
            }
            tokens.Add(new Token { Kind = TokenKind.End, Text = "end", Position = source.Length });
            return tokens;
        }

        private static TokenKind Scan(string s, ref int i)
        {
            char c = s[i];
            if (c == '"' || c == '\'') { SkipString(s, ref i); return TokenKind.String; }
            if (char.IsDigit(c) || c == '-' && i + 1 < s.Length && char.IsDigit(s[i + 1])) { SkipNumber(s, ref i); return TokenKind.Number; }
            if (char.IsLetter(c) || c == '_' || c == '$') { i++; while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_' || s[i] == '`')) i++; return TokenKind.Ident; }
            if ("().[],=".IndexOf(c) >= 0) { i++; return TokenKind.Symbol; }
            throw new BridgeException($"unexpected '{c}' at {i}");
        }

        private static void SkipString(string s, ref int i)
        {
            char quote = s[i++];
            while (i < s.Length && s[i] != quote) i += s[i] == '\\' ? 2 : 1;
            if (i >= s.Length) throw new BridgeException("unterminated string");
            i++;
        }

        private static void SkipNumber(string s, ref int i)
        {
            i++;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' && i + 1 < s.Length && char.IsDigit(s[i + 1]))) i++;
            if (i < s.Length && "fFdDlL".IndexOf(s[i]) >= 0) i++;
        }
    }

    internal static class Literals
    {
        internal static object Number(string text)
        {
            string digits = text.TrimEnd('f', 'F', 'd', 'D', 'l', 'L');
            if (digits.Contains(".") || text.EndsWith("f") || text.EndsWith("F") || text.EndsWith("d") || text.EndsWith("D"))
                return double.Parse(digits, CultureInfo.InvariantCulture);
            return long.Parse(digits, CultureInfo.InvariantCulture);
        }

        internal static string Unquote(string text)
        {
            var result = new StringBuilder();
            for (int i = 1; i < text.Length - 1; i++)
            {
                char c = text[i];
                if (c == '\\' && i + 1 < text.Length - 1) c = Escape(text[++i]);
                result.Append(c);
            }
            return result.ToString();
        }

        private static char Escape(char c) => c == 'n' ? '\n' : c == 't' ? '\t' : c;
    }
}
