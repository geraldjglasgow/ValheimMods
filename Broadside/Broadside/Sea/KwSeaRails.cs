using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Railings on a launched ship (the user, 2026-10-26: "I want to remove some of my railing so there's a gap for the
    /// cannon on the deck"): the railing tool's click on a railing opens the dock's railing window on it
    /// (<see cref="KwRailEdit"/>: post spacing, sides, material; a roof railing's edges and gaps; a Remove button); on a
    /// planking part's top edge with no railing over it, or a cabin's roof with none, the dock's window for a new one
    /// (<see cref="KwSeaRailAdd"/>); the hover shows the new railing see-through. The hammer's remove aimed at a railing
    /// takes the side aimed at (a two-sided part keeps its other side) or, on a roof railing, the edge aimed at; the last
    /// side or edge removes the part, leaving the gap a cannon stands in. Each change is a sea edit (<see cref="KwSeaShip"/>):
    /// its cost or refund the railing's, the shields renumbered on the owner (<see cref="KwSeaShields"/>), the railing's
    /// look, boxes and the shield hooks redrawn on every peer (<see cref="KwSeaRailDraw"/>).
    /// </summary>
    public sealed class KwSeaRails : KwSeaKind
    {
        public const float Reach = 1.2f;
        private static readonly KwRailEdit Edits = new KwRailEdit();
        private static readonly string Prefab = KwToolPieces.PartPrefix + KwBiomeCost.Rail;

        public override string Name => "rail";

        public override bool Owns(KwBpPiece piece) => piece.Prefab == Prefab;

        public override bool Serves(string tool) => tool == KwBiomeCost.Rail;

        public override bool Click(Player player, string tool, KwSeaAim aim)
        {
            if (!aim.Hit)
                return false;
            KwSeaShip ship = aim.Ship;
            int railing = Railing(aim);
            KwSeaRoofSite? roof = railing < 0 ? KwSeaRoofSite.At(ship.Blueprint, aim.Point) : null;
            KwSeaSheer? sheer = railing < 0 && roof == null ? KwSeaRailSite.Sheer(ship.Blueprint, ship.Keels, aim.Point, Reach) : null;
            if (railing < 0)
                railing = roof != null ? roof.Railed(ship.Blueprint) : sheer != null ? KwSeaRailSite.Over(ship.Blueprint, sheer.Value.Index) : -1;
            if (railing >= 0)
                Edit(player, ship, railing, aim.Point);
            else if (roof != null)
                KwSeaRailAdd.Roof(player, ship, roof, aim.Point);
            else if (sheer != null)
                KwSeaRailAdd.Hull(player, ship, sheer.Value);
            return railing >= 0 || roof != null || sheer != null;
        }

        // The railing whose stretch's box the aim met; -1 for none.
        private static int Railing(KwSeaAim aim) =>
            aim.Tag != null && aim.Tag.Kind == KwShipHitRouter.Rail && aim.Tag.Piece >= 0 && aim.Tag.Piece < aim.Ship.Blueprint.Pieces.Count ? aim.Tag.Piece : -1;

        // The railing's window, as on the dock; its Remove button takes the whole part.
        private static void Edit(Player player, KwSeaShip ship, int index, Vector3 point)
        {
            KwBpPiece piece = ship.Blueprint.Pieces[index];
            KwPartValues? values = Values(piece);
            string? why = values == null ? "This railing's saved text does not read" : KwSeaRules.Why(player, ship, ship.World(point));
            if (why != null)
            {
                player.Message(MessageHud.MessageType.Center, why);
                return;
            }
            Renderer? look = KwShipBody.Drawn ? ship.Visual.GetComponent<KwShipRailLooks>()?.Of(piece) : null;
            KwSeaEdit Whole() => new KwSeaEdit("rail", ship.Revision, "Railing removed", new List<KwSeaPart> { KwSeaPart.Remove(index) });
            KwSeaWindow.Open(player, ship, index, Edits, values!, KwRoofRail.TryParse(KwSeaPiece.Text(piece), out _) ? "Roof railing" : "Railing",
                look != null ? new[] { look } : new Renderer[0], () => KwSeaAsk.Send(player, ship, Whole(), point), "rail", "Railing changed", point);
        }

        // A railing's window values: the dock's, a hull railing's sides as port and starboard by the way it runs (the bow is +z).
        private static KwPartValues? Values(KwBpPiece piece)
        {
            string saved = KwSeaPiece.Text(piece);
            KwPartValues? values = Edits.Read(saved, KwBiomeTiers.Of(piece.Data), KwSeaPiece.Tiered(piece));
            if (values == null || KwRoofRail.TryParse(saved, out _) || !KwSpan.TryParse(saved, out KwSpan span))
                return values;
            float runs = KwSeaRailSite.Runs(piece);
            values.Set(KwRailField.Runs, runs);
            values[KwRailField.HullSides] = KwRailBow.Name(KwRailText.Sides(span.Rail), runs);
            return values;
        }

        public override bool Hover(Player player, string tool, KwSeaAim aim) => aim.Hit && KwSeaRailAdd.Hover(aim, Railing(aim) >= 0);

        public override void Unhover() => KwSeaGhost.Clear();

        public override KwSeaPick? Pick(KwSeaAim aim)
        {
            if (Railing(aim) >= 0)
                return new KwSeaPick(this, aim.Tag!.Piece, aim.Tag.Part, 0f, "railing", aim.Point);
            KwShipRails? rails = aim.Hit ? aim.Ship.GetComponent<KwShipRails>() : null;
            if (rails == null)
                return null;
            KwSeaPick? best = null;
            foreach (KwRailStretch s in rails.Stretches)
            {
                float d = s.Distance(aim.Point);
                if (d <= KwSeaRemove.Near && (best == null || d < best.Distance))
                    best = new KwSeaPick(this, s.Piece, s.Part, d, "railing", aim.Point);
            }
            return best;
        }

        public override KwSeaEdit? Removal(KwSeaShip ship, KwSeaPick pick) => KwSeaRailCut.Removal(ship, pick);

        public override string? Check(KwSeaShip ship, KwSeaEdit edit)
        {
            foreach (KwSeaPart part in edit.Parts)
            {
                string text = part.Piece != null && Owns(part.Piece) ? KwSeaPiece.Text(part.Piece) : "";
                if (part.Piece != null && Owns(part.Piece) && !KwRoofRail.TryParse(text, out _) && !KwSpan.TryParse(text, out _))
                    return "That railing does not read";
            }
            return null;
        }

        public override void Owner(KwSeaShip ship, KwSeaChanges mine, ZDO zdo) => KwSeaShields.Owner(ship, mine, zdo);

        public override void Redraw(KwSeaShip ship, KwSeaChanges mine) => KwSeaRailDraw.Redraw(ship, mine);
    }
}
