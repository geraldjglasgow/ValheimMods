using System.Collections.Generic;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Scaling;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// The Thieving pouch: up to <see cref="HardCap"/> stolen items, packed into one byte array on the creature's ZDO
    /// under <see cref="TraitKeys.Pouch"/> - the same "serialise an ItemData into a ZDO field" idiom the game's own
    /// ItemDrop already uses for a single item (ItemDrop.SaveToZDO), repeated here for a small list. A ZDO is
    /// replicated, so every machine can read the pouch; only the owner ever writes it, from Runtime.ThievingRpc.
    /// </summary>
    public static class PouchStore
    {
        /// <summary>One stolen item: its live data (ItemData.Save/Load already round-trips its own prefab identity),
        /// and who took it and when - the pair a re-delivered steal packet is recognised and banked only once by.</summary>
        public sealed class Entry
        {
            public ItemDrop.ItemData Item = null!;
            public long StealerId;
            public int StealCounter;
        }

        /// <summary>ZDO data is replicated to everyone near the creature; a pouch is not a chest. Spec-fixed, never configurable.</summary>
        public const int HardCap = 8;

        // The Version.Item value ItemData.Save/Load already agree on (ChunksNCheats) - written once for the whole
        // package, not per item, matching how a single item is saved into a ZDO field elsewhere in the game.
        private const byte ItemVersion = 109;

        private static bool _warnedCap;

        public static int Count(ZDO zdo) => Load(zdo).Count;

        public static List<Entry> Load(ZDO zdo)
        {
            List<Entry> entries = new List<Entry>();
            if (zdo == null || !zdo.GetByteArray(TraitKeys.Pouch, out byte[] bytes) || bytes == null || bytes.Length == 0)
            {
                return entries;
            }
            ZPackage pkg = new ZPackage(bytes);
            Version.Item version = (Version.Item)pkg.ReadByte();
            int count = pkg.ReadInt();
            for (int i = 0; i < count; i++)
            {
                Entry? entry = ReadEntry(pkg, version);
                if (entry != null)
                {
                    entries.Add(entry);
                }
            }
            return entries;
        }

        private static Entry? ReadEntry(ZPackage pkg, Version.Item version)
        {
            long stealerId = pkg.ReadLong();
            int counter = pkg.ReadInt();
            var (prefabHash, item) = ItemDrop.ItemData.Load(pkg, version);
            if (prefabHash == 0 || !TryResolvePrefab(prefabHash, item))
            {
                return null; // the item's prefab no longer exists (removed content) - drop it rather than crash
            }
            return new Entry { Item = item, StealerId = stealerId, StealCounter = counter };
        }

        /// <summary>Wires up what ItemData.Load leaves unset: the shared item data and the drop prefab, from the
        /// resolved prefab hash. Without this, a reloaded item's GetIcon() and ItemDrop.DropItem both fail. Public
        /// because ThievingRpc needs the identical fix-up for an item just received fresh off the steal RPC.</summary>
        public static bool TryResolvePrefab(int prefabHash, ItemDrop.ItemData item)
        {
            GameObject? prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefabHash) : null;
            ItemDrop? drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (drop == null)
            {
                return false;
            }
            item.m_shared = drop.m_itemData.m_shared;
            item.m_dropPrefab = prefab;
            return true;
        }

        public static void Save(ZDO zdo, List<Entry> entries)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(ItemVersion);
            pkg.Write(entries.Count);
            foreach (Entry entry in entries)
            {
                pkg.Write(entry.StealerId);
                pkg.Write(entry.StealCounter);
                entry.Item.Save(pkg);
            }
            zdo.Set(TraitKeys.Pouch, pkg.GetArray());
        }

        /// <summary>True when this exact steal (stealer + its per-attempt counter) is already in the pouch - the durable,
        /// ZDO-backed guard that makes a re-delivered steal packet bank once, surviving an ownership handover for free.</summary>
        public static bool AlreadyBanked(List<Entry> entries, long stealerId, int counter)
        {
            foreach (Entry entry in entries)
            {
                if (entry.StealerId == stealerId && entry.StealCounter == counter)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Owner-only: bank a steal if there is room and it is not already banked. False means "no room" - the
        /// caller drops the item at the creature instead of discarding it. True covers both "banked" and "already had it".</summary>
        public static bool TryAdd(ZDO zdo, Entry entry, int maxItems)
        {
            List<Entry> entries = Load(zdo);
            if (AlreadyBanked(entries, entry.StealerId, entry.StealCounter))
            {
                return true;
            }
            if (entries.Count >= maxItems)
            {
                return false;
            }
            entries.Add(entry);
            Save(zdo, entries);
            return true;
        }

        /// <summary>The resolved `max items` for this creature: the configured value, large-star enhanced like any other
        /// stat bonus (spec: "the bonus above the baseline... rounded to the nearest whole item, never below 1"), then
        /// hard-clamped to <see cref="HardCap"/> with a once-only warning naming the setting.</summary>
        public static int ResolvedMaxItems(BiomeRules rules, CreatureTraits traits)
        {
            float raw = Enhance.Stat(rules, traits, Mutation.Thieving, Fields.MaxItems);
            int rounded = Mathf.Max(1, Mathf.RoundToInt(raw));
            if (rounded <= HardCap)
            {
                return rounded;
            }
            if (!_warnedCap)
            {
                _warnedCap = true;
                Log.Warn($"Thieving 'max items' resolves to {rounded}, above the hard cap of {HardCap} - clamped. "
                    + "The pouch rides the creature's ZDO, which is replicated to everyone nearby.");
            }
            return HardCap;
        }
    }
}
