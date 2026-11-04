using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The net winch in the hammer's Misc tab after the ammunition piles (<see cref="KwHammer"/> adds it, so the saved ships
    /// still come last), costing its recipe (<see cref="KwTrawl.Cost"/>, given back when it is removed; an item the game
    /// lacks left out, with a warning), at the wood floor's station: a workbench in range. Idempotent.
    /// </summary>
    public static class KwTrawlHammer
    {
        public static void Add(ObjectDB database, List<GameObject> pieces)
        {
            foreach (GameObject prefab in KwTrawl.All)
            {
                prefab.GetComponent<Piece>().m_resources = Cost(database);
                if (!pieces.Contains(prefab))
                    pieces.Add(prefab);
            }
        }

        private static Piece.Requirement[] Cost(ObjectDB database)
        {
            List<Piece.Requirement> cost = new List<Piece.Requirement>();
            foreach ((string item, int amount) in KwTrawl.Cost)
            {
                ItemDrop? drop = database.GetItemPrefab(item)?.GetComponent<ItemDrop>();
                if (drop != null)
                    cost.Add(new Piece.Requirement { m_resItem = drop, m_amount = amount, m_recover = true });
                else
                    Log.Warn($"Broadside: {KwTrawl.Prefab} costs {item}, which the game does not have; left out.");
            }
            return cost.ToArray();
        }
    }
}
