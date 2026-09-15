using System;
using System.Collections.Generic;

namespace OpenKeep.Core
{
    /// <summary>
    /// Per character state in <c>Player.m_customData</c>, which the game saves with the character, under keys
    /// <c>OpenKeep.&lt;key&gt;</c>. Sets are comma separated; a flag is "1". Without a local player reads return
    /// empty and writes are dropped.
    /// </summary>
    public static class CharacterData
    {
        private const string Prefix = "OpenKeep.";

        public static HashSet<string> GetSet(string key)
        {
            HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
            string value = Get(key);
            if (string.IsNullOrEmpty(value))
                return result;
            foreach (string part in value.Split(','))
            {
                string item = part.Trim();
                if (item.Length > 0)
                    result.Add(item);
            }
            return result;
        }

        public static void SetSet(string key, IEnumerable<string> values)
        {
            List<string> parts = new List<string>();
            if (values != null)
            {
                foreach (string value in values)
                {
                    string item = (value ?? "").Trim().Replace(",", "");
                    if (item.Length > 0 && !parts.Contains(item))
                        parts.Add(item);
                }
            }
            Set(key, parts.Count > 0 ? string.Join(",", parts) : null);
        }

        public static bool GetFlag(string key)
        {
            string value = Get(key);
            return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        public static void SetFlag(string key, bool value) => Set(key, value ? "1" : null);

        private static Dictionary<string, string> Store => Player.m_localPlayer != null ? Player.m_localPlayer.m_customData : null;

        private static string Get(string key)
        {
            Dictionary<string, string> store = Store;
            return store != null && store.TryGetValue(Prefix + key, out string value) ? value : null;
        }

        private static void Set(string key, string value)
        {
            Dictionary<string, string> store = Store;
            if (store == null)
                return;
            if (value == null)
                store.Remove(Prefix + key);
            else
                store[Prefix + key] = value;
        }
    }
}
