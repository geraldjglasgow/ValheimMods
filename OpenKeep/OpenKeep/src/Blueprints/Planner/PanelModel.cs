using System.Collections.Generic;
using OpenKeep.Blueprints.Sites;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints.Planner
{
    /// <summary>
    /// One row of the queue panel: an entry's name, its sizes, the materials its unbuilt pieces and the supports it
    /// brings still need, and those pieces (they glow while the row is hovered).
    /// </summary>
    public sealed class PanelRow
    {
        public string Name;
        public string Sizes;
        public string Materials;
        public List<int> Unbuilt;
    }

    /// <summary>
    /// What the queue panel shows for its site, made from the site's ZDO and kept until the ZDO changes (made again at
    /// most twice a second, so a site building piece by piece does not remake it every piece; at once after an edit
    /// from here): the site's name and progress and a row per queue entry. Made only in OnGUI's layout pass, so every
    /// pass of a frame draws the same rows.
    /// </summary>
    public static class PanelModel
    {
        private const float MinGap = 0.5f;
        private const int MostMaterials = 4;

        public static readonly List<PanelRow> Rows = new List<PanelRow>();

        public static SiteMarker Site { get; private set; }

        public static string Title { get; private set; } = "";

        public static string Progress { get; private set; } = "";

        /// <summary>Changes whenever the rows are made again (the hovered row's glow sets itself again).</summary>
        public static int Stamp { get; private set; }

        private static uint revision = uint.MaxValue;
        private static float madeAt = -100f;

        public static void Refresh(SiteMarker site)
        {
            if (site != null && site == Site && UpToDate(site))
                return;
            Make(site);
        }

        /// <summary>This machine changed the queue: the rows are made again on the next layout pass.</summary>
        public static void Invalidate()
        {
            revision = uint.MaxValue;
            madeAt = -100f;
        }

        private static bool UpToDate(SiteMarker site) => site.State.Zdo.DataRevision == revision || Time.unscaledTime < madeAt + MinGap;

        private static void Make(SiteMarker site)
        {
            Site = site;
            Rows.Clear();
            Stamp++;
            madeAt = Time.unscaledTime;
            revision = uint.MaxValue;
            SiteState state = site != null ? site.State : null;
            Title = state != null ? state.Name : "";
            Progress = "";
            if (state?.Blueprint == null)
                return;
            revision = state.Zdo.DataRevision;
            Progress = BlueprintWords.Format(PlannerWords.Progress, state.BuiltCount, state.Blueprint.Pieces.Count);
            List<List<int>> builds = SiteOrder.ByEntry(state);
            for (int k = 0; k < state.Queue.Count; k++)
                Rows.Add(Row(state, state.Queue[k], k < builds.Count ? builds[k] : new List<int>()));
        }

        /// <summary>A queue entry's row; <paramref name="builds"/> is what the entry builds, supports included.</summary>
        private static PanelRow Row(SiteState state, SiteSelection entry, List<int> builds)
        {
            if (builds.Count == 0)
                return new PanelRow { Name = entry.Name, Sizes = Language.Localize(PlannerWords.RowDone), Materials = "", Unbuilt = builds };
            int own = PlannerPieces.Unbuilt(state, entry.Pieces).Count;
            string sizes = BlueprintWords.Format(PlannerWords.RowSizes, entry.Pieces.Count, own) + PlannerWords.SupportsNote(builds.Count - own);
            string materials = PlannerBill.Describe(state.Blueprint, builds, MostMaterials) ?? Language.Localize(PlannerWords.Free);
            return new PanelRow { Name = entry.Name, Sizes = sizes, Materials = materials, Unbuilt = builds };
        }
    }
}
