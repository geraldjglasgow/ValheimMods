using System.Collections.Generic;

namespace Hearthhold
{
    /// <summary>
    /// What friendship needs to know about one trader, worked out once per trader object: its prefab name (Haldor,
    /// Hildir, the Bog Witch or any modded trader), its custom data keys and its display name; plus the last hover text,
    /// so the hover line is not rebuilt every frame (<see cref="TraderHover"/>).
    /// </summary>
    public sealed class TraderInfo
    {
        private const int PruneAt = 32;

        private static readonly Dictionary<Trader, TraderInfo> cache = new Dictionary<Trader, TraderInfo>();
        private static readonly List<Trader> gone = new List<Trader>();

        public readonly string Prefab;
        public readonly string PointsKey;
        public readonly string GiftDayKey;
        public readonly string DisplayName;

        internal string HoverBase;
        internal string HoverText;
        internal int HoverHearts = -1;

        private TraderInfo(Trader trader)
        {
            Prefab = Utils.GetPrefabName(trader.gameObject);
            PointsKey = FriendshipKeys.PointsPrefix + Prefab;
            GiftDayKey = FriendshipKeys.GiftDayPrefix + Prefab;
            DisplayName = Localization.instance != null ? Localization.instance.Localize(trader.m_name) : trader.m_name;
        }

        /// <summary>The info of a live trader, made on first use.</summary>
        public static TraderInfo Of(Trader trader)
        {
            if (cache.TryGetValue(trader, out TraderInfo info))
                return info;
            if (cache.Count >= PruneAt)
                Prune();
            info = new TraderInfo(trader);
            cache[trader] = info;
            return info;
        }

        /// <summary>Forgets traders that were destroyed (their zone unloaded), so the cache never grows.</summary>
        private static void Prune()
        {
            gone.Clear();
            foreach (Trader trader in cache.Keys)
            {
                if (trader == null)
                    gone.Add(trader);
            }
            foreach (Trader trader in gone)
                cache.Remove(trader);
            gone.Clear();
        }
    }
}
