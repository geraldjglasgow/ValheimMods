using System.Collections.Generic;
using System.Linq;
using OpenKeep.Core;

namespace OpenKeep.Blueprints.Planner
{
    /// <summary>
    /// What a set of a site's pieces costs, for the HUD and the queue panel: the building materials of the pieces added
    /// up by item (pieces the world's "no build cost" key makes free left out, as <see cref="MaterialBill"/> does), shown
    /// as "Wood 40, Stone 10, +2" with the players' item names, or "free" with Build Without Materials.
    /// </summary>
    public static class PlannerBill
    {
        /// <summary>The materials of the pieces (pass the unbuilt ones), biggest first, at most <paramref name="most"/> and "+N"; null when they cost nothing.</summary>
        public static string Describe(Blueprint bp, IEnumerable<int> pieces, int most)
        {
            if (BlueprintSettings.FreeMaterials)
                return Language.Localize(PlannerWords.Free);
            Dictionary<string, int> amounts = Needed(bp, pieces);
            if (amounts.Count == 0)
                return null;
            List<string> parts = amounts.OrderByDescending(a => a.Value).Take(most)
                .Select(a => Language.Localize(a.Key) + " " + a.Value).ToList();
            if (amounts.Count > most)
                parts.Add("+" + (amounts.Count - most));
            return string.Join(", ", parts);
        }

        /// <summary>Amounts by the item's name token ("$item_wood").</summary>
        public static Dictionary<string, int> Needed(Blueprint bp, IEnumerable<int> pieces)
        {
            Dictionary<string, int> amounts = new Dictionary<string, int>();
            foreach (int i in pieces)
            {
                Piece piece = i >= 0 && i < bp.Pieces.Count ? MaterialBill.PieceOf(bp.Pieces[i].Prefab) : null;
                if (piece != null && !FreeInThisWorld(piece))
                    Add(amounts, piece);
            }
            return amounts;
        }

        private static bool FreeInThisWorld(Piece piece) => ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey());

        private static void Add(Dictionary<string, int> amounts, Piece piece)
        {
            foreach (Piece.Requirement r in piece.m_resources)
            {
                if (r.m_resItem == null || r.m_amount <= 0)
                    continue;
                string name = r.m_resItem.m_itemData.m_shared.m_name;
                amounts[name] = (amounts.TryGetValue(name, out int had) ? had : 0) + r.m_amount;
            }
        }
    }
}
