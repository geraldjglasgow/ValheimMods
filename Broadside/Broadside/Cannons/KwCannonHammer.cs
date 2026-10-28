using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The ship cannons in the hammer's Misc tab, after Broadside's own pieces (<see cref="KwHammer"/> adds them, so the
    /// saved ships still come last), each costing its own recipe (<see cref="KwArmDef.Cost"/>, refunded on removal; an item
    /// the game lacks left out, with a warning), at the wood floor's station: a workbench in range, as Broadside's sea
    /// edits and saved ships need. Only the cannons of a tier offered now (<see cref="KwTiers.Shown"/>: the bronze and
    /// small iron ship cannons); the others are kept out of it (taken out should they be in it), still registered and still costing their
    /// recipe, so those a world already has load and give it back when removed. Idempotent.
    /// </summary>
    public static class KwCannonHammer
    {
        /// <summary>How many cannons the hammer offers.</summary>
        public static int Offered => KwCannonPrefabs.All.Count(Shown);

        /// <summary>Each cannon's cost set from its recipe; the cannons offered into the hammer's pieces, after Broadside's own.</summary>
        public static void Add(ObjectDB database, List<GameObject> pieces)
        {
            foreach (GameObject prefab in KwCannonPrefabs.All)
            {
                prefab.GetComponent<Piece>().m_resources = Cost(database, prefab.name);
                if (!Shown(prefab))
                    pieces.Remove(prefab);
                else if (!pieces.Contains(prefab))
                    pieces.Add(prefab);
            }
        }

        // Whether the cannon's tier is offered now.
        private static bool Shown(GameObject prefab) => KwArmory.OfPrefab(prefab.name) is KwArmDef arm && KwTiers.Shown(arm.Tier);

        // A cannon's own recipe (an item the game lacks left out, with a warning).
        private static Piece.Requirement[] Cost(ObjectDB database, string name)
        {
            List<Piece.Requirement> cost = new List<Piece.Requirement>();
            foreach ((string item, int amount) in KwArmory.OfPrefab(name)?.Cost ?? new List<(string, int)>())
            {
                ItemDrop? drop = database.GetItemPrefab(item)?.GetComponent<ItemDrop>();
                if (drop != null)
                    cost.Add(new Piece.Requirement { m_resItem = drop, m_amount = amount, m_recover = true });
                else
                    Log.Warn($"Broadside cannons: {name} costs {item}, which the game does not have; left out.");
            }
            return cost.ToArray();
        }
    }
}
