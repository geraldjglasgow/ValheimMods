using System.Collections.Generic;
using System.Linq;
using OpenKeep.Blueprints.Sites;

namespace OpenKeep.Blueprints.Planner
{
    /// <summary>
    /// The local player's current selection: pieces of one site (a pick on another site starts a new selection),
    /// whether smart select added to it (it is then queued as a house), and a version that changes with every edit so
    /// the glow and the HUD line set themselves again. It lives on this machine only, until it is queued or cleared,
    /// its site goes, or the local player changes.
    /// </summary>
    public static class PlannerSelection
    {
        private static readonly HashSet<int> pieces = new HashSet<int>();

        /// <summary>The site the selection belongs to, or null.</summary>
        public static SiteMarker Site { get; private set; }

        /// <summary>Smart select added a house to it.</summary>
        public static bool Smart { get; private set; }

        public static int Version { get; private set; }

        public static int Count => Site != null ? pieces.Count : 0;

        public static bool Contains(SiteMarker site, int piece) => site != null && site == Site && pieces.Contains(piece);

        /// <summary>A copy of the selected pieces (handed to the glow, which may keep it).</summary>
        public static ICollection<int> Copy() => new List<int>(pieces);

        /// <summary>The selected pieces not built yet, in blueprint order.</summary>
        public static List<int> Unbuilt() => Site != null ? PlannerPieces.Unbuilt(Site.State, pieces) : new List<int>();

        /// <summary>A click: the piece is selected, or let go when it was.</summary>
        public static void Toggle(SiteMarker site, int piece)
        {
            Begin(site);
            if (!pieces.Remove(piece))
                pieces.Add(piece);
            Changed();
        }

        /// <summary>Shift + click: the house's pieces are added; when every one was selected already, they are let go. The number added (negative: let go).</summary>
        public static int ToggleHouse(SiteMarker site, List<int> house) => ToggleGroup(site, house, smart: true);

        /// <summary>A group of pieces in or out at once (a house, or G's joined pieces of one type); a smart group names the queue entry "House".</summary>
        public static int ToggleGroup(SiteMarker site, List<int> group, bool smart)
        {
            Begin(site);
            int added = group.Count(p => !pieces.Contains(p));
            if (added == 0)
                pieces.ExceptWith(group);
            else
                pieces.UnionWith(group);
            Smart |= smart && added > 0;
            Changed();
            return added > 0 ? added : -group.Count;
        }

        public static void Clear()
        {
            Site = null;
            pieces.Clear();
            Changed();
        }

        /// <summary>The selection's site was unloaded or taken down: the selection goes with it.</summary>
        public static void DropLost()
        {
            if (!ReferenceEquals(Site, null) && Site == null)
                Clear();
        }

        private static void Begin(SiteMarker site)
        {
            if (site == Site)
                return;
            Clear();
            Site = site;
        }

        private static void Changed()
        {
            if (pieces.Count == 0)
                Smart = false;
            Version++;
        }
    }
}
