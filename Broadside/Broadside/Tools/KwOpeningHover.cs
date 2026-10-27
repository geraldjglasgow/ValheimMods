using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Doorway and Window tools every frame while one is in hand (from <see cref="KwPreview"/> through
    /// <see cref="KwHouseTools"/>): the tool's own placeholder hidden (only its renderers, so the game still counts the
    /// wheel for it); the wheel's notches (<see cref="KwToolNotches"/>: the game's placement rotation for a piece that turns,
    /// also in the build camera, which leaves the wheel to the game while no window is open) stepping through the tool's
    /// types (<see cref="KwOpeningCurrent"/>), or with Shift held setting how many a row has; with the aim on a bare wall of
    /// a part on a dry dock, the current type placed there and snapped (<see cref="KwOpeningPlace"/>), shown as the real
    /// ghost (<see cref="KwOpeningGhost"/>; a part a ward keeps refuses it); with Shift held, the row evenly spread across the
    /// free stretch the aim is in shown instead (<see cref="KwOpeningRowShown"/>, <see cref="KwOpeningRow"/>); and the
    /// placement message said (<see cref="KwOpeningWords"/>). The placing is worked out again only when the aim moves a
    /// centimetre or the type changes. With a tool's window open, the window's plan is the ghost. Local to this player.
    /// </summary>
    public static class KwOpeningHover
    {
        private static KwOpeningPlacing? placing;
        private static (string Tool, object Type, object? Part, string Wall, int U, int Z) at;
        private static GameObject? concealed;

        /// <summary>One frame with the Doorway or Window tool in hand, aimed on the dock.</summary>
        public static void Frame(Player player, string tool) => Frame(player, tool, () => KwOpeningAimed.Of(player));

        /// <summary>One frame with the Doorway or Window tool in hand, the sight given (a launched ship's, <see cref="KwSeaOpenings"/>).</summary>
        public static void Frame(Player player, string tool, System.Func<KwOpeningSight> aim)
        {
            Placeholder(player);
            bool row = KwTools.Slant();
            Wheel(tool, row);
            if (KwForm.IsOpen)
            {
                KwOpeningSession.Preview();
                return;
            }
            KwOpeningSight sight = aim();
            KwOpeningReads.Use(sight.Sea);
            KwOpeningPlacing? now = Shown(tool, sight);
            KwOpeningRowPlan? rowed = row && now != null ? KwOpeningRowShown.Of(tool, now, sight.Site!.U) : null;
            KwOpeningGhost.Show(row ? rowed?.Plan : now != null && now.Fits ? now.Plan : null, KwOpeningGhostLooks.Placing);
            KwOpeningWords.Hover(KwOpeningWords.Of(tool, sight, now, row, rowed));
        }

        /// <summary>Every frame: with another tool in hand (or none) the ghost gone and what was worked out forgotten.</summary>
        public static void Unless(string? tool)
        {
            if (tool == KwDoorwayTool.Tool || tool == KwWindowTool.Tool || (!KwOpeningGhost.Shown && placing == null))
                return;
            KwOpeningGhost.Hide();
            Forget();
        }

        private static void Forget()
        {
            placing = null;
            at = default;
            KwOpeningRowShown.Forget();
        }

        /// <summary>A click on a bare wall: the placing at the aim, worked out now, built (with Shift, its row); or why not.</summary>
        public static void Place(Player player, string tool, KwOpeningSight sight, bool row)
        {
            at = default;
            KwOpeningPlacing? now = Shown(tool, sight);
            if (now == null)
                KwOpeningSession.Say(player, sight.Why.Length > 0 ? sight.Why : "Only parts on a dry dock or a launched ship can be cut");
            else if (row)
                PlaceRow(player, tool, now, sight.Site!.U);
            else if (!now.Fits)
                KwOpeningSession.Say(player, "Cannot: " + now.Plan.Why);
            else
                Build(player, tool, now.Plan, now.Cut != null ? new List<(KwOpeningSite, KwOpening)> { now.Cut.Value } : new List<(KwOpeningSite, KwOpening)>());
        }

        // Shift + click: the row at the aim worked out now and built, the places that fit; or why not.
        private static void PlaceRow(Player player, string tool, KwOpeningPlacing now, float aimed)
        {
            KwOpeningRowPlan? row = KwOpeningRow.At(now, aimed, KwOpeningCurrent.Of(tool).Row, KwOpeningReads.Context(tool));
            if (row == null)
                KwOpeningSession.Say(player, now.Sited ? "No free stretch of wall here for a row" : "Cannot: " + now.Plan.Why);
            else if (row.Plan.Made == 0)
                KwOpeningSession.Say(player, "None of the row fits: " + row.Plan.SkipWhy);
            else
                Build(player, tool, row.Plan, row.Pair);
        }

        // The placing at a bare wall on a dock, the type current; null elsewhere. Kept while the aim stays within a centimetre.
        private static KwOpeningPlacing? Shown(string tool, KwOpeningSight sight)
        {
            KwOpeningSite? site = sight.Site;
            if (site == null || sight.Opening != null || !sight.Cuttable)
                return null;
            KwOpeningType type = KwOpeningCurrent.Of(tool).Type;
            var now = (tool, (object)type, site.Of, site.Wall, Mathf.RoundToInt(site.U * 100f), Mathf.RoundToInt(site.Z * 100f));
            if (placing != null && now.Equals(at))
                return placing;
            at = now;
            placing = KwOpeningPlace.At(site, type, KwOpeningReads.Context(tool));
            if (placing.Fits && KwOpeningWork.Warded(placing.Plan))
                placing.Plan = KwOpeningPlace.Refusal("A ward you have no access to keeps this part as it is");
            return placing;
        }

        /// <summary>A plan the tool's click made built (one undo step); the spacing it set kept, the player told.</summary>
        public static void Build(Player player, string tool, KwOpeningPlan plan, List<(KwOpeningSite Site, KwOpening Opening)> placed)
        {
            if (KwOpeningWork.Warded(plan))
            {
                KwOpeningSession.Say(player, "A ward you have no access to keeps a part this would change");
                return;
            }
            KwOpeningContext context = KwOpeningReads.Context(tool);
            KwOpeningSpacing.Placed(tool, placed.ConvertAll(p => context.Middle(p.Site, p.Opening)));
            (string one, string many) = KwOpeningWords.Kind(tool);
            if (KwSeaOpeningSend.Sent(player, plan, one, many))
            {
                Forget();
                return;
            }
            int kept = KwOpeningWork.Build(player, plan);
            KwOpeningGhost.Hide();
            KwOpeningReads.Forget();
            Forget();
            KwOpeningSession.Say(player, KwOpeningNotes.Done(plan, one, many, kept));
        }

        // The tool's own placeholder never drawn: its renderers off once for each placeholder the game makes.
        private static void Placeholder(Player player)
        {
            GameObject? ghost = player.m_placementGhost;
            if (ghost == null || ghost == concealed)
                return;
            concealed = ghost;
            foreach (Renderer renderer in ghost.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
        }

        // The wheel's notches since the last frame (KwToolNotches takes them off the game's placement rotation): types
        // stepped, or with Shift held the row's count.
        private static void Wheel(string tool, bool row)
        {
            int notches = KwToolNotches.Take(tool);
            if (notches == 0 || KwForm.IsOpen)
                return;
            if (row)
            {
                KwOpeningRowShown.Step(tool, notches);
                return;
            }
            KwOpeningCurrent.Of(tool).Step(notches);
            at = default;
        }
    }
}
