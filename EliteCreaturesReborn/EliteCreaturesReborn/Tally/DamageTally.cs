using System.Collections.Generic;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Tally
{
    /// <summary>
    /// A boss's damage tally: how much health each player has taken off it. It lives in the boss's ZDO, written by the
    /// boss's owner at every hit, so an owner hand-over mid-fight keeps the count. One entry per player, keyed by player
    /// ID, carrying the name as it was at the player's last hit.
    /// </summary>
    internal static class DamageTally
    {
        public sealed class Entry
        {
            public long Id;
            public string Name = "";
            public float Damage;
        }

        public static List<Entry> Load(ZDO zdo)
        {
            List<Entry> entries = new List<Entry>();
            byte[]? bytes = zdo.GetByteArray(TraitKeys.BossDamage);
            if (bytes != null && bytes.Length > 0)
            {
                Read(new ZPackage(bytes), entries);
            }
            return entries;
        }

        /// <summary>Owner side: credits one hit's health loss to the player who dealt it.</summary>
        public static void Add(ZDO zdo, Player player, float amount)
        {
            List<Entry> entries = Load(zdo);
            Merge(entries, player.GetPlayerID(), player.GetPlayerName(), amount);
            ZPackage pkg = new ZPackage();
            Write(pkg, entries);
            zdo.Set(TraitKeys.BossDamage, pkg.GetArray());
        }

        public static void Merge(List<Entry> entries, long id, string name, float amount)
        {
            Entry? entry = entries.Find(e => e.Id == id);
            if (entry == null)
            {
                entries.Add(new Entry { Id = id, Name = name, Damage = amount });
                return;
            }
            entry.Name = name;
            entry.Damage += amount;
        }

        public static void Write(ZPackage pkg, List<Entry> entries)
        {
            pkg.Write(entries.Count);
            foreach (Entry entry in entries)
            {
                pkg.Write(entry.Id);
                pkg.Write(entry.Name);
                pkg.Write(entry.Damage);
            }
        }

        public static void Read(ZPackage pkg, List<Entry> into)
        {
            int count = pkg.ReadInt();
            for (int i = 0; i < count; i++)
            {
                into.Add(new Entry { Id = pkg.ReadLong(), Name = pkg.ReadString(), Damage = pkg.ReadSingle() });
            }
        }
    }
}
