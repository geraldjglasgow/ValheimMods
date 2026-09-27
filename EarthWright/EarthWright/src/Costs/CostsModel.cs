using System;
using System.Collections.Generic;
using YamlConfig;

namespace EarthWright.Costs
{
    /// <summary>One item of an entry's material list in EarthWright.Costs.yml: the item prefab name and how many.</summary>
    public struct ItemAmount
    {
        public string Prefab;
        public int Amount;
    }

    /// <summary>What EarthWright.Costs.yml sets for one menu entry; a null member leaves that cost to the .cfg settings.</summary>
    public sealed class EntryOverride
    {
        /// <summary>Replaces the entry's own materials; an empty list makes the entry free.</summary>
        public List<ItemAmount> Resources;

        /// <summary>Stamina per use, replacing "Stamina Mode" for this entry.</summary>
        public float? Stamina;

        /// <summary>Multiplies the tool wear of this entry.</summary>
        public float? Durability;

        /// <summary>Prefab name (or $name) of the crafting station the entry needs instead of its own.</summary>
        public string Station;

        /// <summary>Whether the entry needs a station at all, over the per-tool switches.</summary>
        public bool? StationRequired;
    }

    /// <summary>
    /// The parsed EarthWright.Costs*.yml files: under <c>entries:</c>, a menu entry's prefab name mapped to its
    /// <c>resources</c> (item prefab name: amount), <c>stamina</c>, <c>durability</c>, <c>station</c> and
    /// <c>stationRequired</c>. A later file replaces an earlier file's entry of the same name. Item and station names
    /// are resolved when used, so the file parses on a dedicated server before the game's items exist.
    /// </summary>
    public sealed class CostsModel : YamlModel
    {
        public Dictionary<string, EntryOverride> Entries { get; } = new Dictionary<string, EntryOverride>(StringComparer.OrdinalIgnoreCase);

        protected override void Read(YamlNode root)
        {
            YamlNode entries = root.Get("entries");
            if (entries.Kind == YamlNodeKind.Missing || entries.Kind == YamlNodeKind.Null)
                return;
            foreach (KeyValuePair<string, YamlNode> entry in entries.Entries)
                Entries[entry.Key.Trim()] = ReadEntry(entry.Value);
        }

        private static EntryOverride ReadEntry(YamlNode node)
        {
            EntryOverride entry = new EntryOverride();
            if (node.Kind == YamlNodeKind.Null)
            {
                node.Warn("no settings, the entry follows the .cfg");
                return entry;
            }
            entry.Resources = ReadResources(node.Get("resources"));
            entry.Stamina = ReadNonNegative(node.Get("stamina"));
            entry.Durability = ReadNonNegative(node.Get("durability"));
            if (node.Get("station").TryString(out string station) && station.Trim().Length > 0)
                entry.Station = station.Trim();
            if (node.Get("stationRequired").TryBool(out bool required))
                entry.StationRequired = required;
            return entry;
        }

        /// <summary>Null when the key is absent; an empty list for <c>resources: {}</c> or a key without a value.</summary>
        private static List<ItemAmount> ReadResources(YamlNode node)
        {
            if (node.Kind == YamlNodeKind.Missing)
                return null;
            List<ItemAmount> list = new List<ItemAmount>();
            if (node.Kind == YamlNodeKind.Null)
                return list;
            foreach (KeyValuePair<string, YamlNode> item in node.Entries)
            {
                if (!item.Value.TryInt(out int amount))
                    continue;
                if (amount < 0)
                    item.Value.Error("the amount cannot be negative");
                else
                    list.Add(new ItemAmount { Prefab = item.Key.Trim(), Amount = amount });
            }
            return list;
        }

        private static float? ReadNonNegative(YamlNode node)
        {
            if (!node.TryFloat(out float value))
                return null;
            if (value >= 0f)
                return value;
            node.Error("cannot be negative");
            return null;
        }
    }
}
