using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The hammock in the hammer's Misc tab after the ammunition piles (<see cref="KwHammer"/> adds it, so the saved ships
    /// still come last) and in its build menu's Broadside entry (<see cref="KwBuildMenu"/>), costing its recipe
    /// (<see cref="KwHammocks.Cost"/>, given back when removed; an item the game lacks left out, with a warning), at the
    /// bed's station: a workbench in range. Idempotent.
    /// </summary>
    public static class KwHammockHammer
    {
        /// <summary>The hammock's cost set from its recipe; the hammock into the hammer's pieces, after the piles.</summary>
        public static void Add(ObjectDB database, List<GameObject> pieces)
        {
            foreach (GameObject prefab in KwHammocks.All)
            {
                prefab.GetComponent<Piece>().m_resources = Cost(database, prefab.name);
                if (!pieces.Contains(prefab))
                    pieces.Add(prefab);
            }
        }

        private static Piece.Requirement[] Cost(ObjectDB database, string prefab)
        {
            List<Piece.Requirement> cost = new List<Piece.Requirement>();
            foreach ((string item, int amount) in KwHammocks.Cost)
            {
                ItemDrop? drop = database.GetItemPrefab(item)?.GetComponent<ItemDrop>();
                if (drop != null)
                    cost.Add(new Piece.Requirement { m_resItem = drop, m_amount = amount, m_recover = true });
                else
                    Log.Warn($"Broadside: {prefab} costs {item}, which the game does not have; left out.");
            }
            return cost.ToArray();
        }
    }
}
