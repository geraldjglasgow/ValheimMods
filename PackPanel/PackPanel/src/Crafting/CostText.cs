using System.Collections.Generic;
using PackPanel.Backpacks;
using PackPanel.Tackle;
using UnityEngine;

namespace PackPanel.Crafting
{
    /// <summary>
    /// A crafted item's cost text ("PackPanel_DeerhideSatchel:1, TrollHide:20, Bronze:2") as the game's requirements. An
    /// item of PackPanel's own (a backpack or a tacklebox) is taken from its catalog, so a recipe that takes the one before
    /// it holds even before ObjectDB has them; anything else comes from ObjectDB. A pair that is not prefab:amount of a
    /// known item is logged and skipped.
    /// </summary>
    public static class CostText
    {
        public static Piece.Requirement[] Parse(ObjectDB db, string text, string owner)
        {
            List<Piece.Requirement> cost = new List<Piece.Requirement>();
            foreach (string pair in (text ?? "").Split(','))
            {
                if (pair.Trim().Length == 0)
                    continue;
                string[] parts = pair.Split(':');
                ItemDrop item = parts.Length == 2 ? Item(db, parts[0].Trim()) : null;
                if (item == null || !int.TryParse(parts[1].Trim(), out int amount) || amount <= 0)
                {
                    Plugin.Log.LogWarning($"{owner} cost: \"{pair.Trim()}\" is not prefab:amount of a known item; skipped");
                    continue;
                }
                cost.Add(new Piece.Requirement { m_resItem = item, m_amount = amount, m_amountPerLevel = 0, m_recover = true });
            }
            return cost.ToArray();
        }

        private static ItemDrop Item(ObjectDB db, string name)
        {
            CraftedKind own = (CraftedKind)BackpackCatalog.ById(name) ?? TackleboxCatalog.ById(name);
            GameObject prefab = own != null ? own.Item : db.GetItemPrefab(name);
            return prefab != null ? prefab.GetComponent<ItemDrop>() : null;
        }
    }
}
