using System;
using System.Collections.Generic;

namespace PackPanel.Tackle
{
    /// <summary>
    /// PackPanel's tackleboxes (the user's table, 2026-09-28): 1, 2, 6 and 8 cells, each recipe taking the box before it,
    /// so a box is upgraded rather than piled up. Their looks are the user's words: a plain wooden box covered in furs,
    /// fine wood in troll leather with bronze fittings, Yggdrasil wood coated in carapace, flametal covered in asksvin hide.
    /// These are the built-in defaults; <c>PackPanel.Tackleboxes.yml</c> overrides any number of them.
    /// </summary>
    public static class TackleboxCatalog
    {
        private static readonly Dictionary<string, TackleboxKind> byName = new Dictionary<string, TackleboxKind>();
        private static readonly Dictionary<string, TackleboxKind> byId = new Dictionary<string, TackleboxKind>(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyList<TackleboxKind> All { get; } = new List<TackleboxKind>
        {
            new TackleboxKind("PackPanel_DriftwoodTacklebox", "driftwood", "Driftwood Tacklebox", "A plain wooden box covered in furs.", 2f,
                new TackleboxStats("piece_workbench", 2, "Wood:10, LeatherScraps:10, DeerHide:5", 1)),
            new TackleboxKind("PackPanel_FinewoodTacklebox", "finewood", "Finewood Tacklebox", "A fine wood box wrapped in troll leather, with bronze corners and clasp.", 3f,
                new TackleboxStats("piece_workbench", 3, "PackPanel_DriftwoodTacklebox:1, FineWood:10, TrollHide:10, Bronze:2", 2)),
            new TackleboxKind("PackPanel_CarapaceTacklebox", "carapace", "Carapace Tacklebox", "A Yggdrasil wood box coated in carapace plates.", 3f,
                new TackleboxStats("blackforge", 1, "PackPanel_FinewoodTacklebox:1, Carapace:10, YggdrasilWood:10", 6)),
            new TackleboxKind("PackPanel_FlametalTacklebox", "flametal", "Flametal Tacklebox", "A flametal box covered in asksvin hide.", 4f,
                new TackleboxStats("blackforge", 3, "PackPanel_CarapaceTacklebox:1, FlametalNew:5, AskHide:10", 8)),
        };

        /// <summary>The kind of an item by its shared name ($packpanel_tacklebox_...), or null for any other item.</summary>
        public static TackleboxKind Of(ItemDrop.ItemData item) => item != null && Tables().TryGetValue(item.m_shared.m_name, out TackleboxKind kind) ? kind : null;

        public static TackleboxKind ById(string id)
        {
            Tables();
            return id != null && byId.TryGetValue(id.Trim(), out TackleboxKind kind) ? kind : null;
        }

        private static Dictionary<string, TackleboxKind> Tables()
        {
            if (byName.Count == 0)
            {
                foreach (TackleboxKind kind in All)
                {
                    byName[kind.Token] = kind;
                    byId[kind.Id] = kind;
                }
            }
            return byName;
        }
    }
}
