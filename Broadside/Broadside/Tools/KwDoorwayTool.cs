using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Doorway tool (the user's request, 2026-10-25: "make a tool that allows the user to make doorways in the material
    /// ... and also to add a door as well"; and, having tried it, "We need the same for doors: place many doors or openings
    /// on a side, equally spaced. Openings can be any size, even huge, but doors can't be. Doors are limited to a set width,
    /// with a double door that is twice as wide"): with it in hand the current type (<see cref="KwOpeningCurrent"/>: a
    /// single door 1.3 m wide, a double door 2.2 m, an open doorway; the wheel steps through them) shows as the real ghost from
    /// the deck inside up where the player aims on a cabin's wall, its raised side or the hull's planking (through the
    /// planking under a cabin's raised side or the raised side over the planking too, <see cref="KwDoorwayPlan"/>), equally
    /// spaced with the side's doorways or centred in a free stretch (<see cref="KwOpeningHover"/>); a click cuts it; with
    /// Shift held a row of them evenly spread across the free stretch shows (the wheel: how many) and Shift + click cuts
    /// the row. A click on a doorway opens its window: fill in, "Door: none / single / double", the width only with no door
    /// (up to the free stretch it stands in), the height, the frame; Shift + click opens its row's window, with a Number
    /// row (<see cref="KwOpeningRowEdit"/>); the doorway edited becomes the current type. A window clicked is the Window
    /// tool's. Free on a dry dock, wards respected, one undo step each; on a launched ship the same, sent to its owner at
    /// the openings' cost (<see cref="KwSeaOpenings"/>).
    /// </summary>
    public static class KwDoorwayTool
    {
        public const string Tool = "doorway";

        public static void Click(Player player) => Clicked(player, KwOpeningAimed.Of(player));

        /// <summary>A click on what the sight is on: the dock's, or a launched ship's (<see cref="KwSeaOpenings"/>).</summary>
        public static void Clicked(Player player, KwOpeningSight sight)
        {
            if (KwForm.IsOpen)
                return;
            KwOpeningReads.Use(sight.Sea);
            KwOpeningSite? site = sight.Site;
            if (site == null)
                KwOpeningSession.Say(player, sight.Why);
            else if (sight.Opening == null)
                KwOpeningHover.Place(player, Tool, sight, KwTools.Slant());
            else if (Partnered(player, site) is Func<KwOpeningSite, List<KwPartMap>> partners)
            {
                if (!sight.Opening.IsDoorway)
                    KwOpeningSession.Say(player, "This is a window: change it with the Window tool");
                else
                    Change(player, site, sight.Opening, partners, KwTools.Slant());
            }
        }

        // How a doorway's partners are found where the player may change its part (a dry dock's ship, or a launched ship's
        // reads); null, the player told why, where not.
        private static Func<KwOpeningSite, List<KwPartMap>>? Partnered(Player player, KwOpeningSite site)
        {
            if (KwSeaOpeningPart.Of(site) != null)
                return KwSeaOpeningSend.May(player, site) ? KwOpeningReads.Context(Tool).Partners : null;
            List<Piece>? ship = KwOpeningSession.OnDock(player, site);
            return ship != null ? s => KwOpeningHull.Partners(s, ship) : (Func<KwOpeningSite, List<KwPartMap>>?)null;
        }

        // A doorway clicked (with Shift, its row): its window filled from it (its door counted where a partner hangs it).
        private static void Change(Player player, KwOpeningSite clicked, KwOpening was, Func<KwOpeningSite, List<KwPartMap>> partnersOf, bool row)
        {
            KwOpeningSite site = clicked.At(was.U, was.Sill + 1f);
            List<KwPartMap> partners = partnersOf(site);
            KwOpeningAsk ask = KwOpeningAsk.Of(was, was.Sill);
            KwOpening? hung = partners.ConvertAll(m => KwDoorwayPlan.Twin(m, was)).Find(t => t != null && t.HasDoor);
            if (!was.HasDoor && hung != null)
                ask.Covered = hung.Cover;
            float widest = Widest(site, was);
            Func<KwOpeningAsk, List<KwField>> rows = a => Fields(a, site, was, partners, widest);
            if (!row || !KwOpeningRowEdit.Open(player, Tool, site, was, ask, rows))
                One(player, site, was, ask, partners, rows);
        }

        // One doorway's window: its plan through its partners, the rows built again when the door chosen asks others.
        private static void One(Player player, KwOpeningSite site, KwOpening was, KwOpeningAsk ask, List<KwPartMap> partners, Func<KwOpeningAsk, List<KwField>> rows)
        {
            const string title = "Change doorway";
            KwOpeningWorld world = KwOpeningSession.World(site);
            KwOpeningSession.Start(player, title, rows(ask), site, world, form => KwDoorwayPlan.Change(site, was, KwOpeningForm.Read(form, ask), partners, world),
                form => KwWindowTool.Lead(KwOpeningForm.Read(form, ask)), ("Doorway", "doorways"), form => Keep(KwOpeningForm.Read(form, ask)),
                KwOpeningForm.Reshaper(title, ask, (a, _) => rows(a)), KwOpeningSession.Still(new[] { (site, was) }));
        }

        // The window's rows for the answers: the height up to what the wall has room for with or without a door, with the
        // frame chosen (with its own frame, never below its own height).
        private static List<KwField> Fields(KwOpeningAsk ask, KwOpeningSite site, KwOpening was, List<KwPartMap> partners, float widest)
        {
            float room = KwDoorwayPlan.Room(site, was.Sill, ask.Framed, partners, ask.Covered != KwOpening.None);
            float own = ask.Covered == KwOpening.None ? was.Height : Mathf.Min(was.Height, KwOpeningShape.DoorMaxHigh);
            return KwOpeningForm.Doorway(ask, ask.Framed == was.Frame ? Mathf.Max(own, room) : room, widest);
        }

        // An open doorway's widest where it stands: centred on its place, out to the free stretch of wall round it.
        private static float Widest(KwOpeningSite site, KwOpening was)
        {
            float wide = KwOpeningFrames.Width(was.Frame).Wide, top = was.Top + wide;
            List<KwFreeSpan> spans = KwOpeningSpans.Of(site, was.Sill, top, site.Openings, KwOpeningReads.Context(Tool).Ladders(site, was.Sill, top), o => o.Id == was.Id);
            KwFreeSpan? span = KwOpeningSpans.At(spans, was.U);
            return span == null ? was.Width : 2f * Mathf.Min(was.U - span.Value.Lo, span.Value.Hi - was.U) - 2f * wide;
        }

        // The doorway edited becomes the tool's current type (a fill-in does not).
        private static void Keep(KwOpeningAsk ask)
        {
            if (!ask.Filled)
                KwOpeningCurrent.Door.Keep(ask);
        }

        /// <summary>The tool's placeholder (never drawn while in hand: the ghost is): a doorway's frame, two posts and a lintel over a threshold.</summary>
        public static void Mark(MeshBuilder b)
        {
            Solid.Box(b, new Vector3(-0.05f, -0.45f, 0f), new Vector3(0.05f, 0.45f, 0.05f));
            foreach (float y in new[] { -0.4f, 0.4f })
                Solid.Box(b, new Vector3(-0.05f, y - 0.05f, 0.05f), new Vector3(0.05f, y + 0.05f, 1.1f));
            Solid.Box(b, new Vector3(-0.05f, -0.45f, 1.1f), new Vector3(0.05f, 0.45f, 1.2f));
            Solid.Box(b, new Vector3(-0.02f, -0.34f, 0.05f), new Vector3(0.02f, 0.05f, 1.05f));
        }
    }
}
