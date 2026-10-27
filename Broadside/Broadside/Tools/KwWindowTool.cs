using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Window tool (the user's request, 2026-10-25: "a window creation tool with lots of options for different windows.
    /// None of them should have any glass: just a framed piece that exposes a hole in the ship ... cannon portholes"; and,
    /// having tried it, "it needs to show the last object that you selected for that tool ... it should actually show the
    /// object that you're trying to place, exactly how it would look if you did place it"): with it in hand the current
    /// type (<see cref="KwOpeningCurrent"/>, the wheel steps through them) shows as the real ghost where the player aims on
    /// the hull's planking, a cabin's wall or raised side, snapped level and equally spaced with the side's windows or
    /// centred in a free stretch (<see cref="KwOpeningHover"/>); a click cuts it there; with Shift held a row of them evenly
    /// spread across the free stretch shows (the wheel: how many) and Shift + click cuts the row. A click on a window opens
    /// its window (<see cref="KwOpeningForm"/>: only the rows its shape has) with "Fill in (wall again)" first; Shift + click
    /// opens its row's window, with a Number row (<see cref="KwOpeningRowEdit"/>); the window edited becomes the current
    /// type. A doorway clicked is the Doorway tool's. Free on a dry dock, wards respected, one undo step each; on a launched
    /// ship the same, sent to its owner at the openings' cost (<see cref="KwSeaOpenings"/>).
    /// </summary>
    public static class KwWindowTool
    {
        public const string Tool = "window";

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
            else if (KwOpeningSession.May(player, site))
            {
                if (sight.Opening.IsDoorway)
                    KwOpeningSession.Say(player, "This is a doorway: change it with the Doorway tool");
                else
                    Change(player, site, sight.Opening, KwTools.Slant());
            }
        }

        // A window clicked (with Shift, its row): the window filled from the one clicked, the rows its shape has.
        private static void Change(Player player, KwOpeningSite site, KwOpening was, bool row)
        {
            float? floor = KwOpeningRays.Floor(site, was.U, was.Sill);
            KwOpeningAsk ask = KwOpeningAsk.Of(was, floor);
            Func<KwOpeningAsk, List<KwField>> rows = a => KwOpeningForm.Window(a, floor != null);
            if (row && KwOpeningRowEdit.Open(player, Tool, site, was, ask, rows))
                return;
            const string title = "Change window";
            List<KwOpeningTarget> target = new List<KwOpeningTarget> { new KwOpeningTarget(site, was, floor) };
            KwOpeningWorld world = KwOpeningSession.World(site);
            KwPartValues? first = null;
            KwOpeningSession.Start(player, title, rows(ask), site, world, form =>
            {
                KwPartValues now = KwOpeningRowEdit.Values(KwOpeningForm.Read(form, ask));
                return KwWindowPlan.Change(target, first ??= now, now, world);
            }, form => Lead(KwOpeningForm.Read(form, ask)), ("Window", "windows"), form => Keep(KwOpeningForm.Read(form, ask)),
                KwOpeningForm.Reshaper(title, ask, (a, _) => rows(a)), KwOpeningSession.Still(new[] { (site, was) }));
        }

        // The window edited becomes the tool's current type (a fill-in does not).
        private static void Keep(KwOpeningAsk ask)
        {
            if (!ask.Filled)
                KwOpeningCurrent.Window.Keep(ask);
        }

        /// <summary>What an edit's note says first: a fill-in makes the wall whole again.</summary>
        public static string Lead(KwOpeningAsk ask) => ask.Filled ? "Fill in: the wall is made whole again." : "";

        /// <summary>The tool's placeholder (never drawn while in hand: the ghost is): a square port's frame, its lid hanging open below it.</summary>
        public static void Mark(MeshBuilder b)
        {
            Solid.Box(b, new Vector3(-0.03f, -0.4f, 0.6f), new Vector3(0.03f, 0.4f, 0.7f));
            Solid.Box(b, new Vector3(-0.03f, -0.4f, 1.3f), new Vector3(0.03f, 0.4f, 1.4f));
            foreach (float y in new[] { -0.35f, 0.35f })
                Solid.Box(b, new Vector3(-0.03f, y - 0.05f, 0.7f), new Vector3(0.03f, y + 0.05f, 1.3f));
            Solid.Box(b, new Vector3(0.03f, -0.36f, 0f), new Vector3(0.08f, 0.36f, 0.6f));
        }
    }
}
