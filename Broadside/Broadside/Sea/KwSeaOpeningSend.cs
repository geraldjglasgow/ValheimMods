using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A Doorway or Window tool's plan on a launched ship sent to its owner (the dock builds it at once,
    /// <see cref="KwOpeningWork"/>): every part it changes, a planking part or a cabin, as its blueprint piece with its new
    /// saved text (its other saved values and its tier kept, <see cref="KwSeaPiece.With"/>), in one sea edit
    /// (<see cref="KwSeaEdit"/>) planned on the ship's revision the tools read; the game's building rules at the openings'
    /// middle, the cost and the owner's answer are the core's (<see cref="KwSeaAsk"/>: a frame, cover or door paid for, one
    /// filled in given back, <see cref="KwOpeningCost"/>). The player is told what was done once the owner has done it.
    /// Runs on the editing player's game.
    /// </summary>
    public static class KwSeaOpeningSend
    {
        /// <summary>The launched ship a plan's changed parts are on (the one the tools read); null for the dock's.</summary>
        public static KwSeaOpeningShip? Of(KwOpeningPlan plan)
        {
            KwSeaOpeningShip? sea = KwOpeningReads.Sea;
            return sea != null && plan.Parts.Exists(c => c.Changed && KwSeaOpeningPart.Of(c.Site) != null) ? sea : null;
        }

        /// <summary>Whether the player may change a launched ship's part at a site (the game's building rules there); told why not.</summary>
        public static bool May(Player player, KwOpeningSite site)
        {
            KwSeaShip? ship = KwOpeningReads.Sea?.Ship;
            KwSeaOpeningPart? part = KwSeaOpeningPart.Of(site);
            if (ship == null || part == null)
                return false;
            string? why = KwSeaRules.Why(player, ship, ship.World(part.Ship(site.Surface.Point(site.U, site.Z, 0f))));
            if (why != null)
                KwOpeningSession.Say(player, why);
            return why == null;
        }

        /// <summary>A plan on a launched ship sent to its owner (true: it was one, sent or refused with the player told); false for the dock's.</summary>
        public static bool Sent(Player player, KwOpeningPlan plan, string one, string many)
        {
            KwSeaOpeningShip? sea = Of(plan);
            if (sea == null || sea.Ship == null)
                return false;
            KwSeaEdit? edit = Edit(sea, plan, KwOpeningNotes.Done(plan, one, many, 0));
            if (edit == null)
                KwOpeningSession.Say(player, "Nothing changed");
            else
                KwSeaAsk.Send(player, sea.Ship, edit, Middle(plan));
            return true;
        }

        /// <summary>The edit a plan makes on a launched ship: each changed part's piece with its new text; null when none changes. Pure.</summary>
        public static KwSeaEdit? Edit(KwSeaOpeningShip sea, KwOpeningPlan plan, string done)
        {
            List<KwSeaPart> parts = new List<KwSeaPart>();
            int revision = sea.Revision;
            foreach (KwPartCut cut in plan.Parts)
            {
                KwSeaOpeningPart? part = KwSeaOpeningPart.Of(cut.Site);
                string? text = cut.Changed && part != null ? cut.Site.Text(cut.Openings) : null;
                if (part == null || text == null || text == part.Saved)
                    continue;
                parts.Add(KwSeaPart.Change(part.Index, KwSeaPiece.With(part.Piece, text, null)));
                revision = part.Revision;
            }
            return parts.Count == 0 ? null : new KwSeaEdit(KwSeaOpenings.Kind, revision, done, parts);
        }

        // Where the rules are asked: the first opening the plan cuts, changes or fills in (its middle on its wall).
        private static Vector3 Middle(KwOpeningPlan plan)
        {
            foreach (KwPartCut cut in plan.Parts)
            {
                if (!(KwSeaOpeningPart.Of(cut.Site) is KwSeaOpeningPart part) || !cut.Changed)
                    continue;
                if (cut.Shown.Count > 0)
                    return On(part, cut.Shown[0].Site, cut.Shown[0].Opening);
                KwOpening? gone = cut.Site.Openings.Find(o => !cut.Openings.Exists(n => n.Id == o.Id));
                KwOpeningSite? wall = gone != null ? part.Wall(gone.Wall) : null;
                return gone != null && wall != null ? On(part, wall, gone) : part.Piece.Position;
            }
            return Vector3.zero;
        }

        private static Vector3 On(KwSeaOpeningPart part, KwOpeningSite site, KwOpening o) => part.Ship(site.Surface.Point(o.U, o.Sill + o.Height / 2f, 0f));
    }
}
