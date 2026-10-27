using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a launched ship needs for sailing it as a ship of one's own, added on every peer once it floats
    /// (<see cref="KwShipBody"/>, after its tuning and fittings): its keys for the map pin (<see cref="KwShipKeys"/>), its
    /// cargo (<see cref="KwShipCargo"/>), and with an anchor windlass in its blueprint its anchor
    /// (<see cref="KwShipAnchor"/>, its cable leaving the ship at the first windlass's fairlead, <see cref="KwAnchorPlan"/>)
    /// and, where there is a screen, its working windlasses (<see cref="KwShipWindlass"/>, the first showing the anchor and
    /// its cable). Its tier's perks come with its stats (<see cref="KwShipPerks"/>).
    /// </summary>
    public static class KwShipSailing
    {
        public static void Add(Transform ship, KwBlueprint blueprint, KwBpHull hull)
        {
            Ship game = ship.GetComponent<Ship>();
            KwShipHydro? hydro = ship.GetComponent<KwShipHydro>();
            if (game == null || hydro == null)
                return;
            ship.gameObject.AddComponent<KwShipKeys>();
            ship.gameObject.AddComponent<KwShipCargo>().Set(game, hydro, hull);
            KwBpPiece? first = blueprint.Pieces.Find(p => KwAnchor.Is(p.Prefab));
            if (first == null)
                return;
            KwAnchorLead lead = Lead(blueprint, first);
            Transform holder = ship.Find(KwShipPrefab.Fittings);
            Vector3? hawse = lead.Found ? ship.InverseTransformPoint(holder.TransformPoint(first.Position + first.Rotation * KwMesh.ToUnity(lead.Hawse))) : (Vector3?)null;
            KwShipAnchor anchor = ship.gameObject.AddComponent<KwShipAnchor>();
            anchor.Set(game, hydro, hawse);
            if (KwShipBody.Drawn)
                KwShipBody.Step("Broadside ship windlass", () => Windlasses(holder, blueprint, anchor, lead));
        }

        // The first windlass's lead overboard from the blueprint (the same on every peer); stowed when it cannot be worked out.
        private static KwAnchorLead Lead(KwBlueprint blueprint, KwBpPiece windlass)
        {
            try
            {
                return KwAnchorPlan.For(blueprint.Pieces, windlass);
            }
            catch (System.Exception e)
            {
                Log.Warn($"Broadside anchor: no lead overboard worked out ({e.Message}); its cable goes over the stem head.");
                return KwAnchorLead.Stowed("its lead could not be worked out");
            }
        }

        // Each windlass a working copy on the ship; the first shows the anchor and its cable, led as planned.
        private static void Windlasses(Transform holder, KwBlueprint blueprint, KwShipAnchor anchor, KwAnchorLead lead)
        {
            KwAnchorLead? shown = lead;
            foreach (KwBpPiece piece in blueprint.Pieces)
            {
                if (!KwAnchor.Is(piece.Prefab))
                    continue;
                Transform made = KwShipParts.Copy(holder, piece, "windlass");
                KwShipWindlass.Make(made, anchor, shown);
                shown = null;
            }
        }
    }
}
