using System.Collections.Generic;
using System.Text;
using EliteCrafting.Affixes;
using EliteCrafting.Display;
using EliteCrafting.Items;
using EliteCrafting.Rules;
using EliteCrafting.Text;

namespace EliteCrafting.Tables.Window
{
    /// <summary>The Inscribe tab's words: the chosen gear's description and the rune and essence tooltips.</summary>
    internal static class InscribeText
    {
        private const string Gold = "<color=#FFB75C>";

        public static string Describe(InscribeChoice choice)
        {
            ItemDrop.ItemData item = choice.Item!;
            ItemState state = ItemState.Read(item);
            ClassInfo info = ItemClasses.Classify(item);
            int level = ItemTier.Of(item);
            var sb = new StringBuilder();
            sb.Append(Words.Localize("$ecf_table_gear_line", Words.Localize("$ecf_class_" + info.ClassId), level.ToString(), TableWords.Biome(level)));
            sb.Append("\n\n");
            string block = state.IsMagic ? DisplayCache.Block(state, item) : "";
            sb.Append(block.Length > 0 ? block.Trim('\n') : Words.Localize("$ecf_table_plain"));
            sb.Append("\n\n").Append(RuneLine(choice.Def));
            sb.Append('\n').Append(SteerLine(choice, info));
            return sb.ToString();
        }

        public static string RuneTip(StoneDef? def, int stored, int carried)
        {
            string text = def != null ? Words.Localize(def.Description) : "";
            return text + "\n\n" + Words.Localize("$ecf_table_rune_counts", stored.ToString(), carried.ToString());
        }

        private static string RuneLine(StoneDef? def)
        {
            if (def == null)
            {
                return "";
            }
            return Gold + Words.Localize(def.Name) + "</color>: " + Words.Localize(def.Description);
        }

        private static string SteerLine(InscribeChoice choice, ClassInfo info)
        {
            if (!TableIcons.Steers(choice.Def))
            {
                return Words.Localize("$ecf_table_no_steer");
            }
            Essence? essence = choice.Essence;
            if (essence == null)
            {
                return Words.Localize("$ecf_table_pick_essence", choice.EssenceCost.ToString());
            }
            (int favoured, int total) = Share(essence, info.ClassId);
            return Words.Localize("$ecf_table_steer", TableWords.EssenceName(essence), favoured.ToString(), total.ToString(),
                choice.EssenceCost.ToString());
        }

        // How many of the inscriptions this item's class can roll the essence steers toward.
        private static (int Favoured, int Total) Share(Essence essence, string? classId)
        {
            int favoured = 0;
            int total = 0;
            IReadOnlyList<PoolEntry> pool = ActiveRules.Current.Affixes.Pool(classId);
            foreach (PoolEntry entry in pool)
            {
                if (!entry.Def.Enabled || entry.Def.Weight <= 0f)
                {
                    continue;
                }
                total++;
                if (essence.Inscriptions.Contains(entry.Def.Id))
                {
                    favoured++;
                }
            }
            return (favoured, total);
        }
    }
}
