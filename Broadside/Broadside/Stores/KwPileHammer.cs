using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The ammunition piles in the hammer's Misc tab after the storage crates (<see cref="KwHammer"/> adds them, so the saved
    /// ships still come last), each costing its recipe (<see cref="KwPileDef.Cost"/>, given back when removed; an item the
    /// game lacks left out, with a warning), at the wood chest's station: a workbench in range. Idempotent.
    /// </summary>
    public static class KwPileHammer
    {
        /// <summary>Each pile's cost set from its recipe; the piles into the hammer's pieces, after the crates.</summary>
        public static void Add(ObjectDB database, List<GameObject> pieces)
        {
            foreach (GameObject prefab in KwPiles.All)
            {
                KwPileDef? def = System.Array.Find(KwPiles.Defs, d => d.Prefab == prefab.name);
                if (def != null)
                    prefab.GetComponent<Piece>().m_resources = Cost(database, def);
                if (!pieces.Contains(prefab))
                    pieces.Add(prefab);
            }
        }

        private static Piece.Requirement[] Cost(ObjectDB database, KwPileDef def)
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
