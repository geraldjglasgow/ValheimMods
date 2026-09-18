using System.Collections.Generic;

namespace Wayfare.Targeting
{
    /// <summary>Per-player favourite portals, persisted client-side in the character save
    /// (<c>Player.m_customData["Wayfare.favourites"]</c>, a comma-separated set of <c>ZDOID</c> strings) so they
    /// travel with the character rather than the installation, and keyed by id rather than tag so a rename does
    /// not un-favourite a portal.</summary>
    public static class PlayerFavourites
    {
        private const string Key = "Wayfare.favourites";

        private static Player cachedFor;
        private static HashSet<string> cache;

        public static bool IsFavourite(ZDOID id) => Set().Contains(id.ToString());

        /// <summary>Adds or removes the id; returns whether it is a favourite afterwards.</summary>
        public static bool Toggle(ZDOID id)
        {
            HashSet<string> set = Set();
            string key = id.ToString();
            bool on = !set.Remove(key);
            if (on)
                set.Add(key);
            Save(set);
            return on;
        }

        public static IEnumerable<ZDOID> All()
        {
            foreach (string raw in Set())
            {
                if (TryParse(raw, out ZDOID id))
                    yield return id;
            }
        }

        private static HashSet<string> Set()
        {
            if (cachedFor != Player.m_localPlayer)
            {
                cachedFor = Player.m_localPlayer;
                cache = Load();
            }
            return cache ?? (cache = new HashSet<string>());
        }

        private static HashSet<string> Load()
        {
            if (Player.m_localPlayer == null || !Player.m_localPlayer.m_customData.TryGetValue(Key, out string raw) || string.IsNullOrEmpty(raw))
                return new HashSet<string>();
            return new HashSet<string>(raw.Split(','));
        }

        private static void Save(HashSet<string> set)
        {
            if (Player.m_localPlayer == null)
                return;
            Player.m_localPlayer.m_customData[Key] = string.Join(",", set);
        }

        private static bool TryParse(string raw, out ZDOID id)
        {
            id = ZDOID.None;
            int sep = raw.IndexOf(':');
            if (sep <= 0)
                return false;
            if (!long.TryParse(raw.Substring(0, sep), out long userId) || !uint.TryParse(raw.Substring(sep + 1), out uint objectId))
                return false;
            id = new ZDOID(userId, objectId);
            return true;
        }
    }
}
