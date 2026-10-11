using System.Collections.Generic;
using System.Linq;
using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Definitions;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Combat
{
    /// <summary>
    /// A definition's <c>drops:</c> on the creature's drop table (the game's <c>CharacterDrop</c>, which drops on the
    /// owner as the creature dies, for every peer to see): its rows added after the base's, or in place of them with
    /// <c>replace: true</c>. The table becomes a new list of new rows, so nothing is written into a list or row another
    /// prefab holds; a base with no table gets one.
    /// <list type="bullet">
    /// <item>An item must be a network item prefab (an ItemDrop in ZNetScene): a drop is spawned into the world on the
    /// owner, and every peer makes it from its prefab. Anything else fails the creature at the row's <c>item</c>.</item>
    /// <item><c>amount: [low, high]</c> includes both ends. The game draws the amount with <c>Random.Range(min, max)</c>,
    /// whose top is left out, so <c>m_amountMax</c> is one above the high end. With the world's resource rate it scales
    /// as any drop does (rows are never <c>m_dontScale</c>).</item>
    /// <item><c>chance</c> is <c>m_chance</c>; <c>one per player</c> is <c>m_onePerPlayer</c> (one for each player
    /// connected, instead of the amount); <c>more for higher levels</c> is <c>m_levelMultiplier</c> (chance and amount
    /// doubled for each star).</item>
    /// </list>
    /// </summary>
    internal static class DropTable
    {
        public static void Apply(CreatureBuild build, DropsBlock drops)
        {
            List<CharacterDrop.Drop> rows = new List<CharacterDrop.Drop>();
            foreach (DropRow row in drops.Items)
            {
                CharacterDrop.Drop? drop = Row(build, row);
                if (drop == null)
                {
                    return;
                }
                rows.Add(drop);
            }
            CharacterDrop table = Table(build.Shell);
            List<CharacterDrop.Drop> kept = drops.Replace ? new List<CharacterDrop.Drop>() : (table.m_drops ?? new List<CharacterDrop.Drop>()).Select(Copy).ToList();
            kept.AddRange(rows);
            table.m_drops = kept;
        }

        private static CharacterDrop.Drop? Row(CreatureBuild build, DropRow row)
        {
            GameObject? item = build.Find.Prefab(row.Item);
            if (item == null || item.GetComponent<ItemDrop>() == null)
            {
                build.Report.Fail($"unknown item '{row.Item}' (a drop must be an item of the game or a mod)", row.Field + ".item");
                return null;
            }
            return new CharacterDrop.Drop
            {
                m_prefab = item,
                m_amountMin = row.Amount.Min,
                m_amountMax = row.Amount.Max + 1,
                m_chance = row.Chance,
                m_onePerPlayer = row.OnePerPlayer,
                m_levelMultiplier = row.MoreForHigherLevels,
                m_dontScale = false,
            };
        }

        private static CharacterDrop Table(GameObject shell)
        {
            CharacterDrop table = shell.GetComponent<CharacterDrop>();
            return table != null ? table : shell.AddComponent<CharacterDrop>();
        }

        private static CharacterDrop.Drop Copy(CharacterDrop.Drop drop) => new CharacterDrop.Drop
        {
            m_prefab = drop.m_prefab,
            m_amountMin = drop.m_amountMin,
            m_amountMax = drop.m_amountMax,
            m_chance = drop.m_chance,
            m_onePerPlayer = drop.m_onePerPlayer,
            m_levelMultiplier = drop.m_levelMultiplier,
            m_dontScale = drop.m_dontScale,
        };
    }
}
