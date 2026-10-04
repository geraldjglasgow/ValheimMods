using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DevBridge.Sync
{
    /// <summary>
    /// What differs between two JSON values, leaf by leaf: paths only here, only there, and with other values. Objects
    /// are walked into (paths joined with dots); arrays and plain values are leaves; null leaves count as absent.
    /// </summary>
    internal sealed class Differences
    {
        private const int Shown = 300;

        internal readonly SortedDictionary<string, JToken> OnlyHere = new SortedDictionary<string, JToken>(StringComparer.Ordinal);
        internal readonly SortedDictionary<string, JToken> OnlyThere = new SortedDictionary<string, JToken>(StringComparer.Ordinal);
        internal readonly SortedDictionary<string, object> Different = new SortedDictionary<string, object>(StringComparer.Ordinal);

        internal bool None => OnlyHere.Count == 0 && OnlyThere.Count == 0 && Different.Count == 0;

        internal static Differences Between(JToken here, JToken there)
        {
            Dictionary<string, JToken> mine = Leaves(here), theirs = Leaves(there);
            var found = new Differences();
            foreach (KeyValuePair<string, JToken> leaf in mine)
            {
                if (!theirs.TryGetValue(leaf.Key, out JToken other)) found.OnlyHere[leaf.Key] = Show(leaf.Value);
                else if (!JToken.DeepEquals(leaf.Value, other)) found.Different[leaf.Key] = Both(leaf.Value, other);
            }
            foreach (KeyValuePair<string, JToken> leaf in theirs)
                if (!mine.ContainsKey(leaf.Key)) found.OnlyThere[leaf.Key] = Show(leaf.Value);
            return found;
        }

        /// <summary>Adds the non-empty lists to a reply row.</summary>
        internal void AddTo(Dictionary<string, object> row)
        {
            if (OnlyHere.Count > 0) row["onlyHere"] = OnlyHere;
            if (OnlyThere.Count > 0) row["onlyThere"] = OnlyThere;
            if (Different.Count > 0) row["different"] = Different;
        }

        /// <summary>One value when both sides agree, {here, there} when not; long strings get where they part.</summary>
        internal static object Both(JToken here, JToken there)
        {
            if (JToken.DeepEquals(here, there)) return Show(here);
            var both = new Dictionary<string, object> { ["here"] = Show(here), ["there"] = Show(there) };
            if (here?.Type == JTokenType.String && there?.Type == JTokenType.String && Math.Max(Text(here).Length, Text(there).Length) > Shown)
                both["from"] = FirstDifference(Text(here), Text(there));
            return both;
        }

        private static JToken Show(JToken value) =>
            value?.Type == JTokenType.String && Text(value).Length > Shown
                ? new JValue(Text(value).Substring(0, Shown) + $"... ({Text(value).Length} characters)")
                : value;

        private static string Text(JToken value) => (string)value ?? "";

        private static int FirstDifference(string a, string b)
        {
            int length = Math.Min(a.Length, b.Length);
            for (int i = 0; i < length; i++)
                if (a[i] != b[i]) return i;
            return length;
        }

        private static Dictionary<string, JToken> Leaves(JToken root)
        {
            var leaves = new Dictionary<string, JToken>(StringComparer.Ordinal);
            Walk(root, "", leaves);
            return leaves;
        }

        private static void Walk(JToken token, string path, Dictionary<string, JToken> leaves)
        {
            if (token is JObject item)
            {
                foreach (JProperty property in item.Properties())
                    Walk(property.Value, path.Length == 0 ? property.Name : path + "." + property.Name, leaves);
            }
            else if (token != null && token.Type != JTokenType.Null)
            {
                leaves[path] = token;
            }
        }
    }
}
