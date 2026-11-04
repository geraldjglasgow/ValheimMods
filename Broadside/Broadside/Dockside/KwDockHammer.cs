using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The dockside decorations in the hammer's Misc tab after the ammunition piles (<see cref="KwHammer"/> adds them, so
    /// the saved ships still come last), and in its build menu's Broadside entry (<see cref="KwBuildMenu"/>), each costing
    /// its recipe (<see cref="KwDockDef.Cost"/>, given back when removed; an item the game lacks left out, with a warning),
    /// at a workbench in range (<see cref="KwDockside"/>). Idempotent.
    /// </summary>
    public static class KwDockHammer
    {
        /// <summary>Each decoration's cost set from its recipe; the decorations into the hammer's pieces, after the piles.</summary>
        public static void Add(ObjectDB database, List<GameObject> pieces)
        {
            foreach (GameObject prefab in KwDockside.All)
            {
                KwDockDef? def = System.Array.Find(KwDockside.Defs, d => d.Prefab == prefab.name);
                if (def != null)
                    prefab.GetComponent<Piece>().m_resources = Cost(database, def);
                if (!pieces.Contains(prefab))
                    pieces.Add(prefab);
            }
        }

        private static Piece.Requirement[] Cost(ObjectDB database, KwDockDef def)
        {
            List<Piece.Requirement> cost = new List<Piece.Requirement>();
            foreach ((string item, int amount) in def.Cost)
            {
                ItemDrop? drop = database.GetItemPrefab(item)?.GetComponent<ItemDrop>();
                if (drop != null)
                    cost.Add(new Piece.Requirement { m_resItem = drop, m_amount = amount, m_recover = true });
                else
                    Log.Warn($"Broadside: {def.Prefab} costs {item}, which the game does not have; left out.");
            }
            return cost.ToArray();
        }
    }
}
