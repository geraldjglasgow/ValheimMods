using System.Collections.Generic;
using UnityEngine;
using Wayfare.Portals;

namespace Wayfare.Targeting
{
    /// <summary>Per-player favourite portals, persisted client-side in the character save
    /// (<c>Player.m_customData["Wayfare.favourites"]</c>, a comma-separated set) so they travel with the character
    /// rather than the installation. A favourite is keyed by the portal's position rounded to the metre, not by its
    /// <c>ZDOID</c>: the game renumbers every ZDOID on each world load (<c>ZDO.Load</c>), so an id key would point at
    /// another portal, or none, after a server restart. Portals never move and two cannot stand within a metre of
    /// each other, and a rename keeps the key. Callers still pass the session's ZDOIDs; the key is looked up in the
    /// current portal snapshot.</summary>
    public static class PlayerFavourites
    {
        private const string Key = "Wayfare.favourites";

        private static Player cachedFor;
        private static HashSet<string> cache;
        private static readonly Dictionary<ZDOID, string> keys = new Dictionary<ZDOID, string>();
        private static int keysVersion = -1;
        private static int version;

        /// <summary>Changes whenever the favourite set does (a toggle, another character's set loaded), so a list
        /// built from it knows when to refill.</summary>
        public static int Version
        {
            get
            {
                Set();
                return version;
            }
        }

        public static bool IsFavourite(ZDOID id) => TryKey(id, out string key) && Set().Contains(key);

        /// <summary>Adds or removes the portal; returns whether it is a favourite afterwards.</summary>
        public static bool Toggle(ZDOID id)
        {
            if (!TryKey(id, out string key))
                return false;
            HashSet<string> set = Set();
            bool on = !set.Remove(key);
            if (on)
                set.Add(key);
            Save(set);
            version++;
            return on;
        }

        /// <summary>The key of a portal in the current snapshot, from a lookup rebuilt only when the snapshot changes
        /// (the map asks for every icon every frame).</summary>
        private static bool TryKey(ZDOID id, out string key)
        {
            if (keysVersion != PortalRegistry.Version)
            {
                keysVersion = PortalRegistry.Version;
                keys.Clear();
                foreach (PortalInfo info in PortalRegistry.Portals)
                    keys[info.Id] = KeyOf(info.Position);
            }
            return keys.TryGetValue(id, out key);
        }

        private static string KeyOf(Vector3 position)
        {
            return Mathf.RoundToInt(position.x) + "/" + Mathf.RoundToInt(position.y) + "/" + Mathf.RoundToInt(position.z);
        }

        private static HashSet<string> Set()
        {
            if (cachedFor != Player.m_localPlayer)
            {
                cachedFor = Player.m_localPlayer;
                cache = Load();
                version++;
            }
            return cache ?? (cache = new HashSet<string>());
        }

        /// <summary>Entries from before position keys (ZDOID strings, "user:id") are dropped: they no longer name a portal.</summary>
        private static HashSet<string> Load()
        {
            HashSet<string> set = new HashSet<string>();
            if (Player.m_localPlayer == null || !Player.m_localPlayer.m_customData.TryGetValue(Key, out string raw) || string.IsNullOrEmpty(raw))
                return set;
            foreach (string entry in raw.Split(','))
            {
                if (entry.Split('/').Length == 3)
                    set.Add(entry);
            }
            return set;
        }

        private static void Save(HashSet<string> set)
        {
            if (Player.m_localPlayer == null)
                return;
            Player.m_localPlayer.m_customData[Key] = string.Join(",", set);
        }
    }
}
