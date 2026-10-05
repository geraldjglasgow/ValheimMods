using System.Collections.Generic;
using OpenKeep.Blueprints.Sites;
using OpenKeep.Core;

namespace OpenKeep.Blueprints.Planner
{
    /// <summary>
    /// The Site planner's block of HUD lines ("planner", <see cref="BlueprintHud"/>): the site's name and how much of it
    /// stands, the ghost piece under the crosshair (with Shift, the size of its house) or a grey hint when there is
    /// none, the selection with its size and the materials its unbuilt pieces need, and the keys (left out while the
    /// queue panel is shown, which lists its own). The selection line is made again only when the selection or the
    /// site's ZDO changed.
    /// </summary>
    public static class PlannerHud
    {
        public const string Block = "planner";
        private const int MostMaterials = 4;

        private static string selectionLine;
        private static int selectionVersion = -1;
        private static uint selectionRevision;

        /// <summary>The lines for this frame; <paramref name="site"/> is the one named in the first line (or null).</summary>
        public static void Show(SiteMarker site, bool panelShowing)
        {
            List<string> lines = new List<string>();
            SiteState state = site != null ? site.State : null;
            if (state?.Blueprint != null)
                lines.Add("<b>" + BlueprintWords.Format(PlannerWords.SiteTitle, state.Name, state.BuiltCount, state.Blueprint.Pieces.Count) + "</b>");
            lines.Add(PlannerAim.Any ? AimLine() : BlueprintHud.Coloured(Language.Localize(PlannerWords.AimHint), BlueprintHud.Grey));
            if (PlannerSelection.Count > 0)
                lines.Add(SelectionLine());
            if (!panelShowing)
                lines.Add(Language.Localize(PlannerWords.Keys));
            BlueprintHud.Set(Block, lines);
        }

        public static void Clear() => BlueprintHud.Clear(Block);

        private static string AimLine()
        {
            SiteMarker site = PlannerAim.Site;
            int piece = PlannerAim.Piece;
            string name = PlannerPieces.Name(site.State.Blueprint, piece);
            string line = BlueprintWords.Format(PlannerSelection.Contains(site, piece) ? PlannerWords.AimingSelected : PlannerWords.Aiming, name);
            if (PlannerKeys.SameType)
                line += "  " + BlueprintWords.Format(PlannerWords.SameTypePreview, PlannerGroup.Of(site, piece).Count);
            else if (PlannerKeys.Shift)
                line += "  " + BlueprintWords.Format(PlannerWords.HousePreview, PlannerHouse.Of(site, piece).Count);
            return line;
        }

        private static string SelectionLine()
        {
            SiteMarker site = PlannerSelection.Site;
            uint revision = site.State.Zdo.DataRevision;
            if (selectionLine != null && selectionVersion == PlannerSelection.Version && selectionRevision == revision)
                return selectionLine;
            List<int> left = PlannerSelection.Unbuilt();
            List<int> builds = left.Count == 0 ? left : SiteOrder.WithSupports(site.State, left);
            string materials = left.Count == 0 ? Language.Localize(PlannerWords.SelectionBuilt)
                : PlannerBill.Describe(site.State.Blueprint, builds, MostMaterials) ?? Language.Localize(PlannerWords.Free);
            selectionLine = BlueprintWords.Format(PlannerWords.SelectionLine, PlannerSelection.Count, materials,
                PlannerWords.SupportsNote(builds.Count - left.Count));
            selectionVersion = PlannerSelection.Version;
            selectionRevision = revision;
            return selectionLine;
        }
    }
}
