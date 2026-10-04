using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevBridge.Scenario
{
    /// <summary>JSON read as written (date-like strings stay strings, comments allowed) and values shown as plain text.</summary>
    internal static class JsonText
    {
        private static readonly JsonLoadSettings SkipComments = new JsonLoadSettings { CommentHandling = CommentHandling.Ignore };

        internal static JToken Parse(string text)
        {
            using (var reader = new JsonTextReader(new StringReader(text)) { DateParseHandling = DateParseHandling.None })
            {
                JToken token = JToken.ReadFrom(reader, SkipComments);
                while (reader.Read())
                    if (reader.TokenType != JsonToken.Comment) throw new JsonReaderException($"more text after the JSON at line {reader.LineNumber}");
                return token;
            }
        }

        /// <summary>The parsed value, or null when the text is not JSON.</summary>
        internal static JToken TryParse(string text)
        {
            try { return string.IsNullOrWhiteSpace(text) ? null : Parse(text); }
            catch (JsonException) { return null; }
        }

        /// <summary>A string as it is, true/false, a number in invariant form, null as "null", objects and lists as compact JSON.</summary>
        internal static string Show(JToken token)
        {
            switch (token?.Type)
            {
                case null: case JTokenType.Null: case JTokenType.Undefined: return "null";
                case JTokenType.String: return (string)token;
                case JTokenType.Boolean: return (bool)token ? "true" : "false";
                case JTokenType.Float: return ((double)token).ToString("R", CultureInfo.InvariantCulture);
                default: return token.ToString(Formatting.None);
            }
        }
    }
}
