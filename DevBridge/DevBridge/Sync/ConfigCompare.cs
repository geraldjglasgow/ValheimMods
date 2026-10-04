using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DevBridge.Sync
{
    /// <summary>Compares two /config replies for the same plugin: its version, Charter status, and every entry's value.</summary>
    internal static class ConfigCompare
    {
        internal static Dictionary<string, object> Head(PeerAnswer here)
        {
            JObject dump = JObject.Parse(here.Body);
            return new Dictionary<string, object>
            {
                ["plugin"] = (string)dump["name"] + " " + (string)dump["version"],
                ["entries"] = (dump["entries"] as JArray)?.Count ?? 0,
                ["charter"] = Status(dump),
            };
        }

        internal static Dictionary<string, object> Compare(PeerAnswer here, PeerAnswer there)
        {
            JObject mine = JObject.Parse(here.Body), theirs = JObject.Parse(there.Body);
            Differences values = Differences.Between(Values(mine), Values(theirs));
            var row = new Dictionary<string, object>
            {
                ["same"] = values.None,
                ["version"] = Differences.Both(mine["version"], theirs["version"]),
                ["charter"] = Differences.Both(Status(mine), Status(theirs)),
            };
            values.AddTo(row);
            return row;
        }

        private static JToken Status(JObject dump) => (dump["charter"] as JObject)?["status"];

        /// <summary>"[Section] Key" to the value as the .cfg writes it.</summary>
        private static JObject Values(JObject dump)
        {
            var values = new JObject();
            foreach (JObject entry in (dump["entries"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                values[$"[{(string)entry["section"]}] {(string)entry["key"]}"] = entry["value"];
            return values;
        }
    }
}
