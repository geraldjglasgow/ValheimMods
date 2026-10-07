using EliteCrafting.Affixes;
using EliteCrafting.Display;
using EliteCrafting.Rules;
using EliteCrafting.Stones;
using EliteCrafting.Text;

namespace EliteCrafting.Tables.Window
{
    /// <summary>The Inscribe tab's right side: the chosen gear, the rune and essence rows, the cost and the button.</summary>
    internal static class InscribePane
    {
        public static void Fill(TableView view, InscribeChoice choice)
        {
            PanelParts parts = view.Parts;
            PaneText.Header(parts, choice.Item?.GetIcon(), choice.Item != null ? NameOf(choice.Item) : Words.Localize("$ecf_table_no_gear"));
            PaneText.Body(parts, choice.Item != null ? InscribeText.Describe(choice) : Words.Localize("$ecf_table_no_gear_desc"));
            PaneText.Labels(parts, "$ecf_table_label_rune", "$ecf_table_label_essence");
            FillRunes(view, choice);
            FillEssences(view, choice);
            bool ready = FillCost(view, choice);
            PaneText.Button(parts, "$ecf_table_inscribe", ready && view.Store.Writable);
            PaneText.Extra(parts, "$ecf_table_store_all", RuneStash.Carried(view.Inventory) > 0 && view.Store.Writable);
        }

        /// <summary>Pure essence the player can spend here: the table's pool and what they carry.</summary>
        public static int Have(TableView view) => view.Store.Essence + RuneBag.CountPrefab(view.Inventory, EssenceItem.PrefabName);

        /// <summary>The item's localized name in its rarity's colour (plain for Normal).</summary>
        public static string NameOf(ItemDrop.ItemData item)
        {
            string? topic = DisplayCache.Topic(ItemState.Read(item), item);
            return Words.Localize(topic ?? item.m_shared.m_name);
        }

        private static void FillRunes(TableView view, InscribeChoice choice)
        {
            SlotRow? row = view.Parts.Runes;
            if (row == null)
            {
                return;
            }
            row.Show(true);
            for (int i = 0; i < row.Count && i < StoneCatalog.BuiltInIds.Length; i++)
            {
                string id = StoneCatalog.BuiltInIds[i];
                int stored = view.Store.Runes(id);
                int carried = RuneBag.Count(view.Inventory, id);
                StoneDef? def = TableIcons.Def(id);
                bool usable = StoneVerbs.IsUsable(def);
                row[i].Show(TableIcons.Rune(id), null, (stored + carried).ToString(), true, dim: !usable || stored + carried == 0);
                row[i].Choose(id == choice.Rune, Items.StoneVisuals.Tint(id));
            }
        }

        private static void FillEssences(TableView view, InscribeChoice choice)
        {
            SlotRow? row = view.Parts.Essences;
            if (row == null)
            {
                return;
            }
            row.Show(true);
            int have = Have(view);
            row[0].Show(TableIcons.EssenceItem(), null, have.ToString(), true, dim: have == 0);
            row[0].Choose(false);
            bool steers = TableIcons.Steers(choice.Def);
            bool short_ = choice.Item != null && have < choice.EssenceCost;
            for (int i = 1; i < row.Count && i <= Essences.All.Length; i++)
            {
                Essence essence = Essences.All[i - 1];
                row[i].Show(TableIcons.Essence(essence), null, "", true, dim: !steers || short_);
                row[i].Choose(essence == choice.Essence && steers, essence.Color);
            }
        }

        // The rune, then the essence when one steers; true when the press can be paid.
        private static bool FillCost(TableView view, InscribeChoice choice)
        {
            CostRow? cost = view.Parts.Cost;
            cost?.Clear();
            if (choice.Item == null || cost == null)
            {
                return false;
            }
            int runes = view.Store.Runes(choice.Rune) + RuneBag.Count(view.Inventory, choice.Rune);
            bool enough = runes >= choice.RuneCost && StoneVerbs.IsUsable(choice.Def);
            cost.Add(TableIcons.Rune(choice.Rune), Words.Localize(choice.Def?.Name ?? ""), choice.RuneCost, runes >= choice.RuneCost);
            Essence? essence = choice.Steering;
            if (essence != null)
            {
                bool paid = Have(view) >= choice.EssenceCost;
                cost.Add(TableIcons.EssenceItem(), Words.Localize("$ecf_essence_item"), choice.EssenceCost, paid);
                enough &= paid;
            }
            return enough;
        }
    }
}
