using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// A Doorway or Window tool's plan built (<see cref="KwOpeningPlan"/>): every part it changes built again with its new
    /// saved text through the edit path (<see cref="KwEditWork.Replace"/>: removed the hammer's way and spawned again in its
    /// place with its other ZDO values, an opening's open or shut state among them), planking as planking and a cabin as a
    /// cabin (an old castle becomes one, as a cabin's edit makes it); the parts spawned in one frame are one undo step
    /// (<see cref="KwUndo"/>), so Ctrl + Z puts every part of one action back at once. Free, as all design on the dock is.
    /// </summary>
    public static class KwOpeningWork
    {
        /// <summary>The plan's parts built again; how many a ward kept as they were.</summary>
        public static int Build(Player player, KwOpeningPlan plan)
        {
            List<KwEditPlan> planking = new List<KwEditPlan>(), cabins = new List<KwEditPlan>();
            foreach (KwPartCut cut in plan.Parts)
            {
                KwEditPlan? made = Made(cut);
                if (made != null)
                    (cut.Site.Planking ? planking : cabins).Add(made);
            }
            int kept = 0, more = 0;
            if (planking.Count > 0)
                KwEditWork.Replace(player, "planking", planking, out kept);
            if (cabins.Count > 0)
                KwEditWork.Replace(player, KwHouses.Cabin, cabins, out more);
            return kept + more;
        }

        /// <summary>Whether a ward the player has no access to keeps any part the plan changes.</summary>
        public static bool Warded(KwOpeningPlan plan) =>
            plan.Parts.Exists(cut => cut.Changed && cut.Site.Piece != null && !KwUndoWork.Removable(cut.Site.Piece));

        // A changed part as the edit path builds it again: its new text, its own tier.
        private static KwEditPlan? Made(KwPartCut cut)
        {
            Piece? piece = cut.Site.Piece;
            string? text = cut.Changed && piece != null ? cut.Site.Text(cut.Openings) : null;
            if (text == null || text == cut.Site.Saved)
                return null;
            return new KwEditPlan(new KwEditTarget(piece!, cut.Site.Saved, cut.Site.Tier, new KwPartValues()), text, cut.Site.Tier);
        }
    }
}
