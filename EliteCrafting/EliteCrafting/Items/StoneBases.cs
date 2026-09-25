using System;
using System.Collections.Generic;
using EliteCrafting.Core;
using UnityEngine;

namespace EliteCrafting.Items
{
    /// <summary>The look family a stone prefab is cloned from (prefabs.md section 4a).</summary>
    internal enum StoneGroup
    {
        Ascension,
        Manipulation,
        Risk,
        Sigil,
        Honing,
        Tempering,
        Essence,
    }

    /// <summary>
    /// Which vanilla item each stone prefab is cloned from (prefabs.md section 4a, DECISIONS.md PRF-1). The base names
    /// live in the game's asset bundles, not in code, so none is verified here: each group tries its proposed base,
    /// then its alternates, then any candidate of any group (with a warning), and a missing base never throws.
    /// Runs on every peer, identically.
    /// </summary>
    internal static class StoneBases
    {
        private static readonly string[] Ascension = { "Ruby", "Amber", "AmberPearl" };
        private static readonly string[] Manipulation = { "Crystal", "DragonTear", "Thunderstone" };
        private static readonly string[] Risk = { "SurtlingCore", "BlackCore" };
        private static readonly string[] Sigil = { "Flint", "BlackMarble" };
        private static readonly string[] Honing = { "Stone", "IronNails", "CopperScrap" };
        private static readonly string[] Tempering = { "IronScrap", "IronNails", "CopperScrap" };

        // ESS-13: believed vanilla, verify with `ecraft dump items` in game like PRF-1.
        private static readonly string[] Essence = { "Thunderstone", "DragonTear", "AmberPearl" };

        private static readonly string[][] AllGroups = { Ascension, Manipulation, Risk, Sigil, Honing, Tempering, Essence };

        private static readonly string[] AscensionIds = { "awakening", "ascension", "exaltation", "transcendence", "apotheosis" };
        private static readonly string[] RiskIds = { "serpent", "binding", "chance", "reflection" };

        /// <summary>The group of a built-in stone id; a shard takes its ascension stone's look (SAL-15).</summary>
        public static StoneGroup GroupOf(string builtInId)
        {
            if (builtInId.StartsWith("essence_", StringComparison.Ordinal)) return StoneGroup.Essence;
            if (builtInId.StartsWith("shard_", StringComparison.Ordinal)) return StoneGroup.Ascension;
            if (Array.IndexOf(AscensionIds, builtInId) >= 0) return StoneGroup.Ascension;
            if (Array.IndexOf(RiskIds, builtInId) >= 0) return StoneGroup.Risk;
            if (builtInId == "honing") return StoneGroup.Honing;
            if (builtInId == "tempering") return StoneGroup.Tempering;
            if (builtInId.StartsWith("sigil_", StringComparison.Ordinal)) return StoneGroup.Sigil;
            return StoneGroup.Manipulation;
        }

        /// <summary>The reserved pool: 01-04 ascension, 05-08 manipulation, 09-12 risk, 13-16 sigil (prefabs.md section 5).</summary>
        public static StoneGroup GroupOfCustom(int number)
        {
            switch ((number - 1) / 4)
            {
                case 0: return StoneGroup.Ascension;
                case 1: return StoneGroup.Manipulation;
                case 2: return StoneGroup.Risk;
                default: return StoneGroup.Sigil;
            }
        }

        /// <summary>Whether any candidate base of any group is in the given lists (false on the main menu's empty first Awake).</summary>
        public static bool AnyPresent(IList<GameObject>? first, IList<GameObject>? second)
        {
            foreach (string[] group in AllGroups)
            {
                foreach (string name in group)
                {
                    if (Find(first, second, name) != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>The base prefab for a group: its own candidates in order, else any candidate at all. Null only when none exists.</summary>
        public static GameObject? Resolve(StoneGroup group, IList<GameObject>? first, IList<GameObject>? second)
        {
            string[] own = AllGroups[(int)group];
            GameObject? found = FirstOf(own, first, second);
            if (found != null)
            {
                if (found.name != own[0])
                {
                    Log.Warn($"stone base {own[0]} not found for the {group} stones; using {found.name}");
                }
                return found;
            }
            foreach (string[] other in AllGroups)
            {
                found = FirstOf(other, first, second);
                if (found != null)
                {
                    Log.Warn($"no base of the {group} group ({string.Join(", ", own)}) exists; using {found.name}");
                    return found;
                }
            }
            return null;
        }

        private static GameObject? FirstOf(string[] names, IList<GameObject>? first, IList<GameObject>? second)
        {
            foreach (string name in names)
            {
                GameObject? found = Find(first, second, name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static GameObject? Find(IList<GameObject>? first, IList<GameObject>? second, string name) =>
            FindIn(first, name) ?? FindIn(second, name);

        // Only real items qualify: the object must carry an ItemDrop to clone the item data from.
        private static GameObject? FindIn(IList<GameObject>? list, string name)
        {
            if (list == null)
            {
                return null;
            }
            for (int i = 0; i < list.Count; i++)
            {
                GameObject go = list[i];
                if (go != null && go.name == name && go.GetComponent<ItemDrop>() != null)
                {
                    return go;
                }
            }
            return null;
        }
    }
}
