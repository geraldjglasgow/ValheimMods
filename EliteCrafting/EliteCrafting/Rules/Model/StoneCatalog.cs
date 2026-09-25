using System;
using EliteCrafting.Core;

namespace EliteCrafting.Rules
{
    /// <summary>
    /// The code-side list of stone prefabs (prefabs.md sections 3 and 5): the 43 built-in stone ids (27 stones and
    /// sigils plus the 16 essences, essences.md section 8), whose prefab is <c>ECF_</c> + PascalCase id, the 16
    /// reserved prefabs owner-defined stones bind to, and the 5 shard prefabs of salvage (salvage.md section 5), which
    /// are items but not stones. Prefabs come from code only and exist on every peer whatever the YAML says; the YAML
    /// binds definitions to them.
    /// </summary>
    public static class StoneCatalog
    {
        public const string PrefabPrefix = "ECF_";
        public const string CustomPrefabPrefix = "ECF_Custom";
        public const int CustomPrefabCount = 16;

        /// <summary>The eight essence families of the defaults (ESS-1), in biome order.</summary>
        public static readonly string[] EssenceFamilies = { "storm", "grove", "venom", "frost", "battle", "seidr", "ember", "tide" };

        public static readonly string[] BuiltInIds = Concat(new[]
        {
            "awakening", "ascension", "exaltation", "transcendence", "apotheosis",
            "growth_lesser", "growth_greater", "turmoil_lesser", "turmoil_greater", "upheaval_lesser",
            "upheaval_greater", "perfection_lesser", "perfection_greater", "severing_lesser", "severing_greater",
            "unmaking", "serpent", "binding", "chance", "reflection", "honing", "tempering",
            "sigil_preservation", "sigil_war", "sigil_warding", "sigil_fortune", "sigil_culling",
        }, EssenceIds());

        /// <summary>The five built-in shards (SAL-4): a fixed list, no reserved shard pool.</summary>
        public static readonly string[] ShardIds =
        {
            "shard_awakening", "shard_ascension", "shard_exaltation", "shard_transcendence", "shard_apotheosis",
        };

        public static bool IsBuiltIn(string id) => Array.IndexOf(BuiltInIds, id) >= 0;

        public static bool IsShard(string? id) => id != null && Array.IndexOf(ShardIds, id) >= 0;

        public static bool IsEssence(string id) => id.StartsWith("essence_", StringComparison.Ordinal);

        /// <summary><c>growth_lesser</c> → <c>ECF_GrowthLesser</c>; <c>shard_ascension</c> → <c>ECF_ShardAscension</c>.</summary>
        public static string PrefabFor(string builtInId) => PrefabPrefix + Ids.Pascal(builtInId);

        /// <summary><c>ECF_Custom01</c> ... <c>ECF_Custom16</c>, 1-based.</summary>
        public static string CustomPrefab(int number) => CustomPrefabPrefix + number.ToString("00");

        public static bool IsCustomPrefab(string prefab)
        {
            for (int i = 1; i <= CustomPrefabCount; i++)
            {
                if (prefab == CustomPrefab(i))
                {
                    return true;
                }
            }
            return false;
        }

        // essence_<family>_lesser, essence_<family>_greater for every family, in family order.
        private static string[] EssenceIds()
        {
            string[] ids = new string[EssenceFamilies.Length * 2];
            for (int i = 0; i < EssenceFamilies.Length; i++)
            {
                ids[2 * i] = "essence_" + EssenceFamilies[i] + "_lesser";
                ids[2 * i + 1] = "essence_" + EssenceFamilies[i] + "_greater";
            }
            return ids;
        }

        private static string[] Concat(string[] a, string[] b)
        {
            string[] all = new string[a.Length + b.Length];
            a.CopyTo(all, 0);
            b.CopyTo(all, a.Length);
            return all;
        }
    }
}
