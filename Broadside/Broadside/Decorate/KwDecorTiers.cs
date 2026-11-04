using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The layout's last step (DECORATE.md, "Wood tiers"): every decoration that comes in the docks' wood tiers
    /// (<see cref="KwDockTiers"/>: the mooring post, stone anchor, net drying rack, lobster pots, harbour beacon, fenders and
    /// signal bell) takes the tier that goes with where it stands. On or beside a dock section (its deck within 1.5 m), that
    /// dock's material's (<see cref="KwPierDeck.Material"/>: plain wood plain, core wood core, fine wood fine); on a game
    /// floor (the floor read under it), core wood when the floor is clearly core wood (hard wood, or a darkwood or log piece's
    /// name), else plain; on the shore (in the water, or within 4 m of it) within 12 m of a dock section, the nearest one's;
    /// anywhere else plain. The same footprint, so nothing else of the layout changes. Plain arithmetic but the docks read.
    /// </summary>
    public static class KwDecorTiers
    {
        private const float OnDock = 1.5f, NearDock = 12f, Shore = 4f;

        /// <summary>Each tiered decoration of the layout swapped for its copy in the tier where it stands.</summary>
        public static void Apply(KwDecorLayout layout)
        {
            List<KwDecorPlan> plans = layout.Plans;
            for (int i = 0; i < plans.Count; i++)
            {
                KwDockDef def = plans[i].Kind.Def;
                KwDockTier? tier = def.Tier == null && KwDockTiers.Has(def.Id) ? Of(layout.Area.Grid, plans[i]) : null;
                KwDecorKind? kind = tier != null ? KwDecorKind.Find(def.Id + "_" + tier.Id) : null;
                if (kind != null)
                    plans[i] = plans[i].As(kind);
            }
        }

        /// <summary>The tier going with where a decoration stands; null for plain wood.</summary>
        public static KwDockTier? Of(KwDecorGrid grid, KwDecorPlan plan)
        {
            KwPierDeck? deck = Nearest(plan.Slot.At, OnDock) ?? Nearest(plan.Position, OnDock);
            if (deck != null)
                return KwDockTiers.OfPier(deck.Material);
            Piece? floor = Floor(grid, plan.Position) ?? Floor(grid, plan.Slot.At);
            if (floor != null)
                return Wood(floor);
            return Shored(grid, plan.Position) ? KwDockTiers.OfPier(Nearest(plan.Position, NearDock)?.Material) : null;
        }

        // The built dock section whose deck is nearest a point, within a level distance.
        private static KwPierDeck? Nearest(Vector3 at, float within)
        {
            KwPierDeck? best = null;
            foreach (KwPierDeck deck in KwPierDeck.Near(at, within))
            {
                if (best == null || deck.Distance(at) < best.Distance(at))
                    best = deck;
            }
            return best;
        }

        // The floor piece read under a point (a deck cell's).
        private static Piece? Floor(KwDecorGrid grid, Vector3 at) =>
            grid.CellOf(at, out int k) && grid.Cells[k].Kind == KwCellKind.Deck ? grid.Cells[k].Floor : null;

        // A floor's wood: a dock section's material; core wood for hard wood, a darkwood or a log piece; else plain.
        private static KwDockTier? Wood(Piece floor)
        {
            KwPier? pier = floor.GetComponent<KwPier>();
            if (pier != null)
                return KwDockTiers.OfPier(pier.Kind);
            string name = Utils.GetPrefabName(floor.gameObject);
            WearNTear? wear = floor.GetComponent<WearNTear>();
            bool core = (wear != null && wear.m_materialType == WearNTear.MaterialType.HardWood)
                || name.IndexOf("darkwood", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("_log", StringComparison.OrdinalIgnoreCase) >= 0;
            return core ? KwDockTiers.Core : null;
        }

        // In the water, or on the land within a few metres of it.
        private static bool Shored(KwDecorGrid grid, Vector3 at) =>
            grid.CellOf(at, out int k) && (grid.Cells[k].Kind == KwCellKind.Water || grid.Cells[k].Water <= Shore);
    }
}
