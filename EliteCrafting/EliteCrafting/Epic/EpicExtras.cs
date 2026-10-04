using System;
using System.Collections.Generic;
using System.Reflection;
using EliteCrafting.Core;
using Newtonsoft.Json;

namespace EliteCrafting.Epic
{
    /// <summary>
    /// The two things the runes need that Epic Loot's API does not publish, read from its public classes by reflection:
    /// how many effects a rarity holds (<c>LootRoller.GetEffectCountsPerRarity</c>, the server owner's
    /// <c>MagicEffectsCount</c>) and the name Epic Loot gives a magic item (<c>MagicItemNames.GetNameForItem</c>, which
    /// builds a Rare item's name from its effects). Either missing: Epic Loot's built-in counts, and the item keeps its name.
    /// </summary>
    internal static class EpicExtras
    {
        private static bool looked;
        private static Type? rarityType;
        private static Type? magicItemType;
        private static MethodInfo? counts;
        private static MethodInfo? names;

        /// <summary>The fewest and most effects Epic Loot rolls at a rarity (counts with a weight above 0).</summary>
        public static (int Min, int Max) Counts(int rarity)
        {
            Bind();
            List<KeyValuePair<int, float>>? table = Invoke(counts, Enum(rarity), false) as List<KeyValuePair<int, float>>;
            int min = int.MaxValue, max = -1;
            foreach (KeyValuePair<int, float> row in table ?? new List<KeyValuePair<int, float>>())
            {
                if (row.Value > 0f)
                {
                    min = Math.Min(min, row.Key);
                    max = Math.Max(max, row.Key);
                }
            }
            // Epic Loot's own defaults when the table is unreadable: Magic 1-3, Rare 2-4, Epic 3-5 and so on.
            return max < 0 ? (rarity + 1, rarity + 3) : (min, max);
        }

        /// <summary>Gives the item the name Epic Loot would give it now; leaves the name alone when that cannot be asked.</summary>
        public static void Rename(ItemDrop.ItemData target, EpicItem item)
        {
            Bind();
            if (names == null || magicItemType == null)
            {
                return;
            }
            try
            {
                object? magic = JsonConvert.DeserializeObject(item.Json, magicItemType);
                item.SetDisplayName(names.Invoke(null, new[] { target, magic }) as string);
            }
            catch (Exception e)
            {
                names = null;
                Log.Warn($"Epic Loot's item names are not reachable, rune-changed items keep their names: {e.InnerException?.Message ?? e.Message}");
            }
        }

        private static object? Enum(int rarity) => rarityType == null ? null : System.Enum.ToObject(rarityType, rarity);

        private static object? Invoke(MethodInfo? method, params object?[] args)
        {
            if (method == null || args[0] == null)
            {
                return null;
            }
            try
            {
                return method.Invoke(null, args);
            }
            catch (Exception e)
            {
                counts = null;
                Log.Warn($"Epic Loot's effect counts are not reachable, using its built-in counts: {e.InnerException?.Message ?? e.Message}");
                return null;
            }
        }

        private static void Bind()
        {
            Assembly? assembly = EpicApi.Ready ? EpicApi.Assembly : null;
            if (looked || assembly == null)
            {
                return;
            }
            looked = true;
            rarityType = assembly.GetType("EpicLoot.ItemRarity");
            magicItemType = assembly.GetType("EpicLoot.MagicItem");
            if (rarityType != null)
            {
                counts = assembly.GetType("EpicLoot.LootRoller")?.GetMethod("GetEffectCountsPerRarity", new[] { rarityType, typeof(bool) });
            }
            if (magicItemType != null)
            {
                names = assembly.GetType("EpicLoot.MagicItemNames")?.GetMethod("GetNameForItem", new[] { typeof(ItemDrop.ItemData), magicItemType });
            }
        }
    }
}
