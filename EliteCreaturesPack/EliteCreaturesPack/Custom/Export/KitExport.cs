using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Export
{
    /// <summary>
    /// `drops:` and `gear:` of an export, as the combat step applies them (Custom/Combat). Drops are the CharacterDrop's
    /// rows with <c>replace: true</c>, so the rows written are the whole table. Gear is the Humanoid's: <c>always</c> its
    /// default items that are not attacks (those go to `attacks:`, <see cref="AttackExport"/>), <c>pick one from</c> its
    /// random weapons, armour and shields, <c>one set from</c> its random sets, with <c>replace: true</c>, so gear and
    /// attacks together give it the base's kit again. Items the game gives each by its own chance have no key: a note.
    /// </summary>
    internal static class KitExport
    {
        private const int MaxAmount = 10000;

        public static void WriteDrops(ExportWriter writer, ExportSource source)
        {
            CharacterDrop? table = source.Prefab.GetComponent<CharacterDrop>();
            List<CharacterDrop.Drop> rows = (table?.m_drops ?? new List<CharacterDrop.Drop>()).Where(row => row != null).ToList();
            writer.Open("drops");
            writer.Note("replace: true throws the base's table away, so the rows below are all it drops");
            writer.Switch("replace", true);
            if (rows.Count == 0)
            {
                writer.Key("items", "[]");
            }
            else
            {
                writer.Open("items");
                rows.ForEach(row => WriteRow(writer, row));
                writer.Close();
            }
            writer.Close();
        }

        public static void WriteGear(ExportWriter writer, ExportSource source)
        {
            Humanoid? humanoid = source.Humanoid;
            if (humanoid == null)
            {
                writer.Note("gear: - it is not a Humanoid, so it carries nothing");
                return;
            }
            writer.Open("gear");
            writer.Note("replace: true empties the base's own gear first; with the attacks below it carries what the base does");
            writer.Switch("replace", true);
            IEnumerable<GameObject> always = Present(humanoid.m_defaultItems).Where(item => !AttackExport.IsAttack(item, source));
            writer.Key("always", ExportValues.Names(always.Select(item => source.OriginOf(item) ?? item.name)));
            GameObject[]?[] sets = (humanoid.m_randomSets ?? new Humanoid.ItemSet[0]).Select(set => set?.m_items).ToArray();
            WriteLists(writer, "pick one from", new[] { humanoid.m_randomWeapon, humanoid.m_randomArmor, humanoid.m_randomShield }, source);
            WriteLists(writer, "one set from", sets, source);
            WriteChances(writer, humanoid.m_randomItems);
            writer.Close();
        }

        /// <summary>The items of an array that are there (a prefab's arrays can hold empty slots).</summary>
        public static IEnumerable<GameObject> Present(GameObject[]? items) =>
            (items ?? Array.Empty<GameObject>()).Where(item => item != null);

        private static void WriteRow(ExportWriter writer, CharacterDrop.Drop row)
        {
            if (row.m_prefab == null)
            {
                writer.Note("a row with no item, which the game skips, is left out");
                return;
            }
            writer.Item();
            writer.Key("item", ExportValues.Name(row.m_prefab.name));
            WriteAmount(writer, row.m_amountMin, row.m_amountMax);
            writer.Number("chance", row.m_chance, 0f, 1f);
            writer.Switch("one per player", row.m_onePerPlayer);
            writer.Switch("more for higher levels", row.m_levelMultiplier);
            if (row.m_dontScale)
            {
                writer.Note("the world's loot setting never scales this row, which a definition cannot say");
            }
            writer.EndItem();
        }

        /// <summary>
        /// The game draws a row's amount with <c>Random.Range(min, max)</c>, which leaves the top out (one number when the
        /// two agree), and a definition's <c>amount: [low, high]</c> includes both ends (the drop step writes high + 1): so
        /// the most that drops, <c>max - 1</c>, is written, never below the least.
        /// </summary>
        private static void WriteAmount(ExportWriter writer, int min, int max)
        {
            int low = min, high = Math.Max(min, max - 1);
            if (low >= 0 && high <= MaxAmount)
            {
                writer.Key("amount", ExportValues.Range(low, high));
                return;
            }
            writer.Note($"amount: {low} to {high} - outside the 0 to 10,000 a definition takes, so it is left as it is");
        }

        private static void WriteLists(ExportWriter writer, string key, GameObject[]?[] lists, ExportSource source) =>
            writer.NameLists(key, lists.Select(list => Present(list).Select(item => source.OriginOf(item) ?? item.name)));

        private static void WriteChances(ExportWriter writer, Humanoid.RandomItem[]? items)
        {
            List<string> chances = (items ?? new Humanoid.RandomItem[0])
                .Where(item => item?.m_prefab != null)
                .Select(item => $"{item.m_prefab.name} ({ExportValues.Number(item.m_chance * 100f)}%)")
                .ToList();
            if (chances.Count > 0)
            {
                writer.Note("it may also carry, each by its own chance, which a definition cannot give: " + string.Join(", ", chances));
            }
        }
    }
}
