using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DevBridge.Sync
{
    /// <summary>Compares two /zdo replies for the same ZDO: owner, revisions, position, and every value it stores.</summary>
    internal static class ZdoCompare
    {
        /// <summary>Within this many metres two copies stand in the same place (positions come rounded to 0.01).</summary>
        private const float SamePlace = 0.05f;

        /// <summary>Compared on their own, not as values: the owner reads "me" on its owner, revisions and position get their own lines.</summary>
        private static readonly string[] OwnLines = { "id", "owner", "ownerId", "revision", "ownerRevision", "position" };

        internal static Dictionary<string, object> Head(PeerAnswer here)
        {
            JObject zdo = JObject.Parse(here.Body);
            return new Dictionary<string, object> { ["zdo"] = (string)zdo["id"], ["prefab"] = (string)zdo["prefab"] };
        }

        internal static Dictionary<string, object> Compare(PeerAnswer here, PeerAnswer there)
        {
            JObject mine = JObject.Parse(here.Body), theirs = JObject.Parse(there.Body);
            ZdoKeys.Align(mine, theirs);
            float apart = Apart(mine["position"], theirs["position"]);
            Differences values = Differences.Between(Values(mine), Values(theirs));
            bool sameOwner = JToken.DeepEquals(OwnerId(mine), OwnerId(theirs));
            var row = new Dictionary<string, object>
            {
                ["same"] = values.None && sameOwner && apart <= SamePlace && JToken.DeepEquals(mine["revision"], theirs["revision"]),
                ["owner"] = sameOwner
                    ? Owner(mine, here.Port) ?? Owner(theirs, there.Port) ?? (string)OwnerId(mine)
                    : Owners(mine, here.Port, theirs, there.Port),
                ["revision"] = Differences.Both(mine["revision"], theirs["revision"]),
                ["ownerRevision"] = Differences.Both(mine["ownerRevision"], theirs["ownerRevision"]),
                ["apart"] = apart,
            };
            values.AddTo(row);
            return row;
        }

        private static JObject Values(JObject zdo)
        {
            var values = (JObject)zdo.DeepClone();
            foreach (string name in OwnLines) values.Remove(name);
            return values;
        }

        /// <summary>The owner's id; older bridges only say "me" or the id as text.</summary>
        private static JToken OwnerId(JObject zdo) => zdo["ownerId"] ?? zdo["owner"];

        /// <summary>"none" when nobody owns it, "id (port N)" when that instance says it is the owner, else null.</summary>
        private static string Owner(JObject zdo, int port)
        {
            string id = (string)OwnerId(zdo);
            if (id == "0") return "none";
            return (string)zdo["owner"] == "me" ? $"{id} (port {port})" : null;
        }

        private static object Owners(JObject mine, int myPort, JObject theirs, int theirPort) => new Dictionary<string, object>
        {
            ["here"] = Owner(mine, myPort) ?? (string)OwnerId(mine),
            ["there"] = Owner(theirs, theirPort) ?? (string)OwnerId(theirs),
        };

        /// <summary>Metres between two [x, y, z] positions; -1 when either is missing.</summary>
        private static float Apart(JToken a, JToken b)
        {
            if (!(a is JArray p) || !(b is JArray q) || p.Count != 3 || q.Count != 3) return -1f;
            double sum = Enumerable.Range(0, 3).Sum(i => Math.Pow((double)p[i] - (double)q[i], 2));
            return Fmt.R((float)Math.Sqrt(sum));
        }
    }

    /// <summary>
    /// A ZDO key one side could name and the other knows only by its hash ("#123") is the same key: the name scan
    /// depends on the plugins each process loaded. Both sides get the name before they are compared.
    /// </summary>
    internal static class ZdoKeys
    {
        internal static void Align(JObject here, JObject there)
        {
            foreach (JProperty section in here.Properties().ToList())
                if (section.Value is JObject mine && there[section.Name] is JObject theirs) Align(mine, theirs, Names(mine, theirs));
        }

        private static Dictionary<int, string> Names(JObject a, JObject b)
        {
            var names = new Dictionary<int, string>();
            foreach (JProperty property in a.Properties().Concat(b.Properties()))
                if (!property.Name.StartsWith("#", StringComparison.Ordinal)) names[property.Name.GetStableHashCode()] = property.Name;
            return names;
        }

        private static void Align(JObject a, JObject b, Dictionary<int, string> names)
        {
            foreach (JObject section in new[] { a, b })
                foreach (JProperty property in section.Properties().ToList())
                {
                    if (!property.Name.StartsWith("#", StringComparison.Ordinal) || !int.TryParse(property.Name.Substring(1), out int hash)) continue;
                    if (names.TryGetValue(hash, out string name) && section[name] == null) property.Replace(new JProperty(name, property.Value));
                }
        }
    }
}
