using System.Collections.Generic;
using EliteCrafting.Core;
using EliteCrafting.Rules;
using EliteCrafting.Text;
using UnityEngine;

namespace EliteCrafting.Salvage
{
    /// <summary>The words of grinding and fusing: amounts with shard and stone names, the dialog preview, the sound. Local client.</summary>
    internal static class GrindText
    {
        /// <summary>"2 Shard of Ascension, 1 Shard of Exaltation", or the word for nothing when every chance row failed.</summary>
        public static string Gained(Dictionary<string, int> gained)
        {
            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, int> kind in gained)
            {
                parts.Add(kind.Value + " " + ShardName(kind.Key));
            }
            return parts.Count == 0 ? Words.Localize("$ecf_ui_salvage_nothing") : string.Join(", ", parts);
        }

        /// <summary>What the dialog promises: every row, a chance row with its percent.</summary>
        public static string Preview(GrindJob job)
        {
            List<string> parts = new List<string>();
            foreach (SalvageYield row in job.Rows)
            {
                string chance = row.Chance < 100f ? $" ({Numbers.Format(row.Chance)}%)" : "";
                parts.Add(row.Amount + " " + ShardName(row.Fragment) + chance);
            }
            return string.Join(", ", parts);
        }

        public static string ShardName(string shardId)
        {
            FragmentDef? def = ActiveRules.Current.Economy.Salvage.Fragment(shardId);
            return Words.Localize(def?.Name ?? "$ecf_fragment_" + shardId);
        }

        /// <summary>The game's item-move sound, as a stone use makes (Phase 3 may give grinding its own).</summary>
        public static void PlaySound()
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui != null)
            {
                gui.m_moveItemEffects.Create(gui.transform.position, Quaternion.identity);
            }
        }
    }
}
